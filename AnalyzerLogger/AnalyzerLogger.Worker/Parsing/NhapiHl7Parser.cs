using AnalyzerLogger.Worker.Parsing.Models;
using NHapi.Base.Parser;
using NHapi.Base.Model;
using NHapi.Model.V251.Message;
using NHapi.Model.V251.Segment;

namespace AnalyzerLogger.Worker.Parsing;

public sealed class NhapiHl7Parser : IHl7Parser
{
	private readonly PipeParser _parser = new();

	public ParsedObservationMessage Parse(string rawMessage)
	{
		var generic = _parser.Parse(rawMessage);
		var msh = (MSH)generic.GetStructure("MSH");
		var messageDateTime = msh.DateTimeOfMessage.Time.Value;
		var messageDate = ParseHl7TsToUtc(messageDateTime);
		var messageControlId = msh.MessageControlID.Value ?? string.Empty;

		string? patientId = null;
		string? placerOrderNumber = null;

		var observations = new List<ObservationRecord>();

		if (generic is ORU_R01 oru)
		{
			for (int pr = 0; pr < oru.PATIENT_RESULTRepetitionsUsed; pr++)
			{
				var pat = oru.GetPATIENT_RESULT(pr);
				patientId ??= pat.PATIENT.PID.GetPatientIdentifierList().Length > 0
					? pat.PATIENT.PID.GetPatientIdentifierList()[0].IDNumber.Value
					: null;
				for (int orIdx = 0; orIdx < pat.ORDER_OBSERVATIONRepetitionsUsed; orIdx++)
				{
					var orderObs = pat.GetORDER_OBSERVATION(orIdx);
					placerOrderNumber ??= orderObs.ORC.PlacerOrderNumber.EntityIdentifier.Value;
					for (int obIdx = 0; obIdx < orderObs.OBSERVATIONRepetitionsUsed; obIdx++)
					{
						var obs = orderObs.GetOBSERVATION(obIdx);
						var obx = obs.OBX;
						var id = obx.ObservationIdentifier.Identifier.Value ?? string.Empty;
						var text = obx.ObservationIdentifier.Text.Value;
						var value = obx.GetObservationValue().Length > 0 ? obx.GetObservationValue()[0].Data.ToString() : null;
						var units = obx.Units?.Identifier?.Value;
						var refRange = obx.ReferencesRange?.Value;
						var flags = obx.AbnormalFlagsRepetitionsUsed > 0 ? obx.GetAbnormalFlags(0).Value : null;
						DateTime? obsDt = null;
						if (obx.DateTimeOfTheObservation?.Time?.Value is string ts && !string.IsNullOrWhiteSpace(ts))
						{
							obsDt = ParseHl7TsToUtc(ts);
						}
						observations.Add(new ObservationRecord
						{
							ObservationId = id,
							ObservationText = text,
							Value = value,
							Units = units,
							ReferenceRange = refRange,
							AbnormalFlags = flags,
							ObservationDateTimeUtc = obsDt
						});
					}
				}
			}
		}

		return new ParsedObservationMessage
		{
			MessageControlId = messageControlId,
			PatientId = patientId,
			OrderNumber = placerOrderNumber,
			MessageDateTimeUtc = messageDate,
			Observations = observations
		};
	}

	private static DateTime ParseHl7TsToUtc(string? ts)
	{
		if (string.IsNullOrWhiteSpace(ts)) return DateTime.UtcNow;
		// HL7 TS can be yyyyMMddHHmmss[.S][+/-ZZZZ]
		// Use basic parse fallbacks
		if (DateTime.TryParseExact(ts.Substring(0, Math.Min(ts.Length, 14)), "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
		{
			return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
		}
		if (DateTime.TryParse(ts, out var generic))
		{
			return generic.ToUniversalTime();
		}
		return DateTime.UtcNow;
	}
}
