using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AnalyzerLogger.Worker.Options;
using AnalyzerLogger.Worker.Storage;
using AnalyzerLogger.Worker.Parsing;
using Microsoft.Extensions.Options;

namespace AnalyzerLogger.Worker.Services;

public sealed class MllpTcpServerService : BackgroundService
{
	private const byte SB = 0x0B; // <VT>
	private const byte EB = 0x1C; // <FS>
	private const byte CR = 0x0D; // <CR>

	private readonly ILogger<MllpTcpServerService> _logger;
	private readonly ListenerOptions _listenerOptions;
	private readonly TcpListener _listener;
	private readonly IRawMessageStore _rawStore;
	private readonly IObservationRepository _observationRepository;
	private readonly IHl7Parser _hl7Parser;

	public MllpTcpServerService(
		ILogger<MllpTcpServerService> logger,
		IOptions<ListenerOptions> listenerOptions,
		IRawMessageStore rawStore,
		IObservationRepository observationRepository,
		IHl7Parser hl7Parser)
	{
		_logger = logger;
		_listenerOptions = listenerOptions.Value;
		_listener = new TcpListener(IPAddress.Parse(_listenerOptions.IpAddress), _listenerOptions.Port);
		_rawStore = rawStore;
		_observationRepository = observationRepository;
		_hl7Parser = hl7Parser;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_listener.Start(_listenerOptions.Backlog);
		_logger.LogInformation("MLLP listener started on {ip}:{port}", _listenerOptions.IpAddress, _listenerOptions.Port);

		while (!stoppingToken.IsCancellationRequested)
		{
			TcpClient? client = null;
			try
			{
				client = await _listener.AcceptTcpClientAsync(stoppingToken);
				_ = HandleClientAsync(client, stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error accepting client");
				client?.Close();
			}
		}
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		_listener.Stop();
		_logger.LogInformation("MLLP listener stopped");
		return base.StopAsync(cancellationToken);
	}

	private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
	{
		using var c = client;
		c.ReceiveTimeout = _listenerOptions.ReceiveTimeoutSeconds * 1000;
		var stream = c.GetStream();

		var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
		var builder = new ArrayBufferWriter<byte>();
		try
		{
			while (!ct.IsCancellationRequested)
			{
				var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct);
				if (read == 0)
				{
					break;
				}
				for (var i = 0; i < read; i++)
				{
					var b = buffer[i];
					if (b == SB)
					{
						builder = new ArrayBufferWriter<byte>();
						continue;
					}
					if (b == EB)
					{
						// Expect CR next, but tolerate if absent
						var messageBytes = builder.WrittenSpan.ToArray();
						var raw = Encoding.ASCII.GetString(messageBytes);
						await _rawStore.StoreAsync(raw, ct);
						await ProcessMessageAsync(raw, stream, ct);
						builder = new ArrayBufferWriter<byte>();
						continue;
					}
					if (b != CR)
					{
						builder.Write(new[] { b });
					}
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Client handler error");
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
			c.Close();
		}
	}

	private async Task ProcessMessageAsync(string rawMessage, NetworkStream stream, CancellationToken ct)
	{
		try
		{
			var parsed = _hl7Parser.Parse(rawMessage);
			await _observationRepository.SaveAsync(parsed, ct);
			await SendAckAsync(stream, true, null, ct);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process message");
			await SendAckAsync(stream, false, ex.Message, ct);
		}
	}

	private static Task SendAckAsync(NetworkStream stream, bool success, string? error, CancellationToken ct)
	{
		// Minimal MSA/MDM ACK in HL7 v2 (generic). For analyzers, ACK usually is enough
		var ack = success ? "MSH|^~\\&|LOGGER|HOST|DEVICE|FACILITY|{0}||ACK^A01|1|P|2.5.1\rMSA|AA|1\r" :
			$"MSH|^~\\&|LOGGER|HOST|DEVICE|FACILITY|{{0}}||ACK^A01|1|P|2.5.1\rMSA|AE|1|{error}\r";
		ack = string.Format(ack, DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
		var bytes = Encoding.ASCII.GetBytes(ack);
		var framed = new byte[bytes.Length + 3];
		framed[0] = SB; Array.Copy(bytes, 0, framed, 1, bytes.Length); framed[^2] = EB; framed[^1] = CR;
		return stream.WriteAsync(framed, 0, framed.Length, ct);
	}
}
