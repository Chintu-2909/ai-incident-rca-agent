using System.ComponentModel.DataAnnotations;

namespace IncidentRca.Application.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    [Required]
    [Url]
    public string BaseUrl { get; set; } =
        "http://localhost:11434";

    [Required]
    public string Model { get; set; } =
        "phi4-mini:latest";

    [Range(10, 600)]
    public int TimeoutSeconds { get; set; } = 120;

    [Range(1, 10)]
    public int MaximumAgentSteps { get; set; } = 5;
}
