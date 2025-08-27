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
		var dataSourceName = msh.SendingApplication?.NamespaceID?.Value ?? msh.SendingApplication?.ToString();
		var deviceId = msh.SendingFacility?.NamespaceID?.Value ?? msh.SendingFacility?.ToString();

		string? patientId = null;
		string? placerOrderNumber = null;
		string? patientSeqNum = null;
		string? patientName = null;
		string? patientLast = null;
		string? patientFirst = null;
		string? patientMiddle = null;
		string? patientSex = null;
		string? patientAddress = null;
		string? patientPhone = null;
		string? patientRace = null;

		var observations = new List<ObservationRecord>();

		if (generic is ORU_R01 oru)
		{
			for (int pr = 0; pr < oru.PATIENT_RESULTRepetitionsUsed; pr++)
			{
				var pat = oru.GetPATIENT_RESULT(pr);
				var pid = pat.PATIENT.PID;
				patientId ??= pid.GetPatientIdentifierList().Length > 0
					? pid.GetPatientIdentifierList()[0].IDNumber.Value
					: null;
				patientSeqNum ??= pid.PatientAccountNumber?.IDNumber?.Value;
				if (pid.GetPatientName().Length > 0)
				{
					var xpn = pid.GetPatientName(0);
					patientLast ??= xpn.FamilyName?.Surname?.Value;
					patientFirst ??= xpn.GivenName?.Value;
					patientMiddle ??= xpn.SecondAndFurtherGivenNamesOrInitialsThereof?.Value;
					patientName ??= string.Join('^', new[] { patientLast, patientFirst, patientMiddle }.Where(s => !string.IsNullOrEmpty(s)));
				}
				patientSex ??= pid.AdministrativeSex?.Value;
				if (pid.GetPatientAddress().Length > 0)
				{
					var xad = pid.GetPatientAddress(0);
					var street = xad.StreetAddress?.StreetOrMailingAddress?.Value;
					var city = xad.City?.Value;
					var state = xad.StateOrProvince?.Value;
					var postal = xad.ZipOrPostalCode?.Value;
					patientAddress ??= string.Join(' ', new[] { street, city, state, postal }.Where(s => !string.IsNullOrWhiteSpace(s)));
				}
				if (pid.GetPhoneNumberHome().Length > 0)
				{
					var xtn = pid.GetPhoneNumberHome(0);
					patientPhone ??= xtn.TelephoneNumber?.Value ?? xtn.AnyText?.Value;
				}
				if (pid.RaceRepetitionsUsed > 0)
				{
					patientRace ??= pid.GetRace(0)?.Identifier?.Value;
				}
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
						var id1 = obx.ObservationIdentifier.Text.Value;
						var id2 = obx.ObservationIdentifier.NameOfCodingSystem.Value;
						var id3 = obx.ObservationIdentifier.AlternateIdentifier.Value;
						var id4 = obx.ObservationIdentifier.AlternateText.Value;
						var value = obx.GetObservationValue().Length > 0 ? obx.GetObservationValue()[0].Data.ToString() : null;
						var units = obx.Units?.Identifier?.Value;
						var refRange = obx.ReferencesRange?.Value;
						var flags = obx.AbnormalFlagsRepetitionsUsed > 0 ? obx.GetAbnormalFlags(0).Value : null;
						var setId = obx.SetIDOBX?.Value;
						var status = obx.ObservationResultStatus?.Value;
						string? operatorId = null;
						if (obx.ResponsibleObserverRepetitionsUsed > 0)
						{
							var xcn = obx.GetResponsibleObserver(0);
							operatorId = xcn?.IDNumber?.Value ?? xcn?.FamilyName?.Surname?.Value;
						}
						DateTime? obsDt = null;
						if (obx.DateTimeOfTheObservation?.Time?.Value is string ts && !string.IsNullOrWhiteSpace(ts))
						{
							obsDt = ParseHl7TsToUtc(ts);
						}
						observations.Add(new ObservationRecord
						{
							ObservationId = id,
							ObservationText = text,
							ObservationId1 = id1,
							ObservationId2 = id2,
							ObservationId3 = id3,
							ObservationId4 = id4,
							Value = value,
							Units = units,
							ReferenceRange = refRange,
							AbnormalFlags = flags,
							ObservationDateTimeUtc = obsDt,
							ResultSequenceNumber = setId,
							ResultStatus = status,
							ResultOperatorId = operatorId
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
			DataSourceName = dataSourceName,
			DeviceId = deviceId,
			PatientSequenceNumber = patientSeqNum,
			PatientName = patientName,
			PatientNameLast = patientLast,
			PatientNameMiddle = patientMiddle,
			PatientNameFirst = patientFirst,
			PatientSex = patientSex,
			PatientAddress = patientAddress,
			PatientPhone = patientPhone,
			PatientRace = patientRace,
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
