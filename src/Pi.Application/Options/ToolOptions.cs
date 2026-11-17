using System.ComponentModel.DataAnnotations;

namespace Pi.Application.Options;

public sealed class ToolOptions
{
    public const string SectionName = "Tools";

    /// <summary>
    /// Pastas que a ferramenta de busca pode varrer. Vazio por padrão (seguro).
    /// </summary>
    public string[] AllowedFolders { get; set; } = [];

    [Range(1, 1000)]
    public int MaxResults { get; set; } = 20;

    [Range(1, 300)]
    public int SearchTimeoutSeconds { get; set; } = 10;
}
