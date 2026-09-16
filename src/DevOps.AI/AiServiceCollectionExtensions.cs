using DevOps.Application.Abstractions;
using DevOps.Application.Analysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevOps.AI;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddIncidentAnalysis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        var enabled = configuration.GetValue<bool>($"{OllamaOptions.SectionName}:Enabled");
        if (!enabled)
        {
            return services;
        }

        var timeout = configuration.GetValue<TimeSpan?>($"{OllamaOptions.SectionName}:Timeout")
            ?? TimeSpan.FromMinutes(10);
        var baseUrl = configuration.GetValue<string>($"{OllamaOptions.SectionName}:BaseUrl")
            ?? "http://127.0.0.1:11434";
        services.AddHttpClient<IIncidentAnalyzer, OllamaIncidentAnalyzer>(client =>
        {
            client.BaseAddress = new Uri(baseUrl!);
            client.Timeout = timeout;
        });
        return services;
    }
}
