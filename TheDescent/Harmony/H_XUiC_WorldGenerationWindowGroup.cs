using HarmonyLib;
using WorldGenerationEngineFinal;

public class H_XUiC_WorldGenerationWindow
{
    public static XUiC_ComboBoxInt terrainOffset;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveNetworks;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveEntrances;

    public static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveWater;

    public static int TerrainOffset => (int)terrainOffset.Value;

    public static WorldBuilder.GenerationSelections CaveNetworks => caveNetworks.Value;

    public static WorldBuilder.GenerationSelections CaveEntrances => caveEntrances.Value;

    public static WorldBuilder.GenerationSelections CaveWater => caveWater.Value;
}


[HarmonyPatch(typeof(XUiC_WorldGenerationWindow), "OnOpen")]
public class XUiC_WorldGenerationWindow_OnOpen
{
    public static void Postfix(XUiC_WorldGenerationWindow __instance)
    {
        if ((H_XUiC_WorldGenerationWindow.terrainOffset = __instance.GetChildById("terrainOffset") as XUiC_ComboBoxInt) != null)
        {
            H_XUiC_WorldGenerationWindow.terrainOffset.Value = (int)CaveConfig.terrainOffset;
        }

        if ((H_XUiC_WorldGenerationWindow.caveNetworks = __instance.GetChildById("caveNetworks") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindow.caveNetworks.Value = WorldBuilder.GenerationSelections.Default;
        }

        if ((H_XUiC_WorldGenerationWindow.caveEntrances = __instance.GetChildById("caveEntrances") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindow.caveEntrances.Value = WorldBuilder.GenerationSelections.Default;
        }

        if ((H_XUiC_WorldGenerationWindow.caveWater = __instance.GetChildById("caveWater") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>) != null)
        {
            H_XUiC_WorldGenerationWindow.caveWater.Value = WorldBuilder.GenerationSelections.Default;
        }
    }
}


[HarmonyPatch(typeof(XUiC_WorldGenerationWindow), "GenerateButton_OnPressed")]
public class XUiC_WorldGenerationWindow_GenerateButton_OnPressed
{
    public static bool Prefix(XUiController _sender, int _mouseButton)
    {
        CaveConfig.terrainOffset = H_XUiC_WorldGenerationWindow.TerrainOffset;
        CaveConfig.caveNetworks = H_XUiC_WorldGenerationWindow.CaveNetworks;
        CaveConfig.caveEntrances = H_XUiC_WorldGenerationWindow.CaveEntrances;
        CaveConfig.caveWater = H_XUiC_WorldGenerationWindow.CaveWater;

        CaveConfig.generateWater = CaveConfig.caveWater != WorldBuilder.GenerationSelections.None;
        CaveConfig.generateCaves = CaveConfig.caveNetworks != WorldBuilder.GenerationSelections.None;

        Logging.Info($"generateWater: {CaveConfig.generateWater}");
        Logging.Info($"terrainOffset: {CaveConfig.terrainOffset}");

        return true;
    }
}
