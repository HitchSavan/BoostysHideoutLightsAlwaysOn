using SPTarkov.Server.Core.Models.Spt.Mod;

namespace BoostysHideoutLightsAlwaysOn;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.boosty.hideoutlightsalwayson";

    public string Name { get; init; } = "BoostysHideoutLightsAlwaysOn";

    public string Author { get; init; } = "Boosty";

    public List<string>? Contributors { get; init; }

    public SemanticVersioning.Version Version { get; init; } = new("1.0.1", false);

    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

    public string? Url { get; init; } = "https://github.com/BKBoosty/BoostysHideoutLightsAlwaysOn";

    public bool? IsBundleMod { get; init; } = false;

    public string License { get; init; } = "CC BY-NC-SA 4.0";
    public bool HasPrepatcher { get; init; } = false;
}
