using SPTarkov.Server.Core.Models.Spt.Mod;

namespace ShAK12Sys;

/// <summary>
///     SPT 4.1.x mod metadata.
/// </summary>
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.avehulian.shak12sys";
    public string Name { get; init; } = "ShAK-12-Sys";
    public string Author { get; init; } = "Avehulian";
    public List<string>? Contributors { get; init; } = ["Avehulian"];
    public SemanticVersioning.Version Version { get; init; } = new("1.5.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.6");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Avehulian/ShAK-12-Sys";
    public string License { get; init; } = "MIT";
}
