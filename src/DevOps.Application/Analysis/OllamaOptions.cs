namespace DevOps.Application.Analysis;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string Model { get; set; } = "llama3.2";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(45);
}
