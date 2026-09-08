using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace BoostysHideoutLightsAlwaysOn;

[Injectable(InjectionType.Singleton)]
public class ModEntry(ISptLogger<ModEntry> logger, HideoutTable hideoutTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var illuminationArea = hideoutTable?.Areas?.Find(area => area.Type == HideoutAreas.Illumination);

        if (illuminationArea == null)
        {
            logger.Warning("[BoostysHideoutLightsAlwaysOn] Illumination area not found in hideout database.");
            return Task.CompletedTask;
        }

        illuminationArea.NeedsFuel = false;
        logger.Success("[BoostysHideoutLightsAlwaysOn] Illumination no longer requires generator fuel.", (Exception?)null);
        return Task.CompletedTask;
    }
}
