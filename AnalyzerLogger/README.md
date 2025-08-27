## AnalyzerLogger (.NET 8) — TCP/IP HL7 Data Logger for IoT Gateways

### Features
- Listens for TCP connections using MLLP framing (VT/FS/CR)
- Logs raw incoming HL7 to files for traceability
- Parses HL7 v2.5.1 ORU^R01 messages via NHapi
- Persists structured observations to Azure SQL Database
- Runs as Windows Service (Windows IoT Enterprise) or console app

### Project Structure
- `AnalyzerLogger.Worker`: Worker service with listener, parser, and repositories

### Requirements
- .NET 8 SDK
- Azure SQL Database (or SQL Server)

### Configuration (`appsettings.json`)
- Listener: IP, Port, Backlog
- Serilog: file sink under `logs/`
- Sql: `ConnectionString`, `Schema`

### Build and Run (Development)
```bash
dotnet build
dotnet run --project AnalyzerLogger.Worker
```

The listener binds to `Listener:IpAddress` and `Listener:Port` (default 0.0.0.0:2575).

### Deploy as Windows Service (Windows IoT Enterprise)
1) Publish:
```bash
dotnet publish AnalyzerLogger.Worker -c Release -r win-x64 --self-contained false -o ./publish
```
2) Install service (run as Administrator PowerShell):
```powershell
New-Service -Name "AnalyzerLogger" -BinaryPathName "C:\path\to\publish\AnalyzerLogger.Worker.exe" -DisplayName "Analyzer Logger" -StartupType Automatic
Start-Service AnalyzerLogger
```
Logs go to `publish\logs`.

### Azure SQL
- Ensure outbound connectivity from IoT device to Azure SQL (Firewall, TLS)
- Set `Sql:ConnectionString` accordingly. Example uses SQL auth; consider Managed Identity if applicable.

### HL7 Over MLLP Test
Send an HL7 message framed by <VT> ... <FS><CR> to the configured port. A simple netcat test on Linux:
```bash
printf '\x0BMSH|^~\\&|SRC|FAC|DST|FAC|20250101010101||ORU^R01|1|P|2.5.1\rPID|||12345^^^HOSP||DOE^JOHN\rOBR|1||ORDER123\rOBX|1|ST|GLU||5.4|mmol/L|4-8|N|||20250101010000\r\x1C\x0D' | nc 127.0.0.1 2575
```

### Troubleshooting
- Check `logs/` for runtime errors
- Ensure firewall allows inbound TCP on the configured port
- Verify SQL connectivity and permissions

