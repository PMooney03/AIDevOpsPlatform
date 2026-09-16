using DevOps.Application.Contracts;

namespace DevOps.Application.Abstractions;

public interface IIncidentAnalyzer
{
    Task<IncidentAnalysisModelResult> AnalyzeAsync(IncidentAnalysisContext context, CancellationToken cancellationToken);
}
