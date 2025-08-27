using AnalyzerLogger.Worker.Parsing.Models;

namespace AnalyzerLogger.Worker.Parsing;

public interface IHl7Parser
{
	ParsedObservationMessage Parse(string rawMessage);
}
