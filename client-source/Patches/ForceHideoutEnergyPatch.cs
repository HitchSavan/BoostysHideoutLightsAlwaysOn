using System.Collections;
using System.Reflection;
using Comfort.Common;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace BoostysHideoutLightsAlwaysOn.Client.Patches;

internal class ForceHideoutEnergyPatch : ModulePatch
{
    private static float _nextIlluminationReadAt;
    private static int _cachedIlluminationLevel = -1;
    private static int _lastLoggedIlluminationLevel = int.MinValue;
    private static int _lastLoggedRequiredLevel = int.MinValue;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(AmbianceController), nameof(AmbianceController.EnergySupplyChanged));
    }

    [PatchPrefix]
    private static void Prefix(ref bool isOn)
    {
        ApplyConfiguredOverride(ref isOn);
    }

    internal static void ApplyConfiguredOverride(ref bool isOn)
    {
        if (!Plugin.EnableLightsAlwaysOn)
        {
            return;
        }

        var requiredLevel = Plugin.MinimumIlluminationLevel;
        if (requiredLevel <= 0)
        {
            isOn = true;
            return;
        }

        var currentLevel = GetCurrentIlluminationLevelCached();
        MaybeLogGateState(requiredLevel, currentLevel);
        if (currentLevel >= requiredLevel)
        {
            isOn = true;
        }
    }

    internal static void ForceRefreshAmbianceFromCurrentState(string reason)
    {
        try
        {
            var hideout = Singleton<HideoutRepresentation>.Instance;
            if (hideout?.EnergyController == null)
            {
                Plugin.Log.LogInfo($"Ambiance refresh skipped ({reason}): Hideout/EnergyController unavailable.");
                return;
            }

            var energyOn = hideout.EnergyController.IsEnergyGenerationOn;
            var controllers = UnityEngine.Object.FindObjectsOfType<AmbianceController>(true);
            foreach (var controller in controllers.Where(c => c != null))
            {
                controller.EnergySupplyChanged(energyOn);
            }

            Plugin.Log.LogInfo($"Ambiance refresh applied ({reason}). controllers={controllers.Length} energyOn={energyOn}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Ambiance refresh failed ({reason}): {ex.Message}");
        }
    }

    private static int GetCurrentIlluminationLevelCached()
    {
        var now = Time.unscaledTime;
        if (now < _nextIlluminationReadAt && _cachedIlluminationLevel >= 0)
        {
            return _cachedIlluminationLevel;
        }

        _nextIlluminationReadAt = now + 1f;
        if (TryGetCurrentIlluminationLevel(out var level))
        {
            _cachedIlluminationLevel = level;
        }

        return _cachedIlluminationLevel;
    }

    private static void MaybeLogGateState(int requiredLevel, int currentLevel)
    {
        if (currentLevel == _lastLoggedIlluminationLevel && requiredLevel == _lastLoggedRequiredLevel)
        {
            return;
        }

        _lastLoggedIlluminationLevel = currentLevel;
        _lastLoggedRequiredLevel = requiredLevel;
        Plugin.Log.LogInfo($"Illumination gate check: current={currentLevel}, required={requiredLevel}");
    }

    private static bool TryGetCurrentIlluminationLevel(out int level)
    {
        level = -1;

        try
        {
            var hideout = Singleton<HideoutRepresentation>.Instance;
            if (hideout == null)
            {
                return false;
            }

            // Most builds expose all area data through one of these members.
            var areasObj = GetPropertyOrFieldValue(hideout, "Areas")
                ?? GetPropertyOrFieldValue(hideout, "AreaDatas")
                ?? GetPropertyOrFieldValue(hideout, "areas_0");

            if (areasObj is IEnumerable areas)
            {
                foreach (var area in areas)
                {
                    if (area == null || !IsIlluminationArea(area))
                    {
                        continue;
                    }

                    if (TryReadAreaLevel(area, out level))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to read Illumination level: {ex.Message}");
        }

        return false;
    }

    private static bool IsIlluminationArea(object area)
    {
        return IsAreaType(area, Plugin.IlluminationAreaType);
    }

    private static bool IsAreaType(object area, int targetType)
    {
        var typeObj = GetPropertyOrFieldValue(area, "Type");
        if (TryConvertToInt(typeObj, out var typeCode))
        {
            return typeCode == targetType;
        }

        var templateObj = GetPropertyOrFieldValue(area, "Template");
        if (templateObj == null)
        {
            return false;
        }

        var templateTypeObj = GetPropertyOrFieldValue(templateObj, "Type");
        return TryConvertToInt(templateTypeObj, out typeCode) && typeCode == targetType;
    }

    private static bool TryReadAreaLevel(object area, out int level)
    {
        level = 0;

        var candidates = new[] { "Level", "CurrentLevel", "level", "currentLevel" };
        foreach (var name in candidates)
        {
            if (TryConvertToInt(GetPropertyOrFieldValue(area, name), out level))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryConvertToInt(object? value, out int result)
    {
        result = 0;
        if (value == null)
        {
            return false;
        }

        try
        {
            result = Convert.ToInt32(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object? GetPropertyOrFieldValue(object? instance, string name)
    {
        if (instance == null)
        {
            return null;
        }

        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null)
            {
                return prop.GetValue(instance);
            }
        }

        return null;
    }
}

internal class ForceHideoutShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(AmbianceController), nameof(AmbianceController.Show));
    }

    [PatchPrefix]
    private static void Prefix(ref bool isEnergyOn)
    {
        ForceHideoutEnergyPatch.ApplyConfiguredOverride(ref isEnergyOn);
    }
}
