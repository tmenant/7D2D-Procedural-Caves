using HarmonyLib;
using WorldGenerationEngineFinal;


[HarmonyPatch]
public class H_XUiC_WorldGenerationWindow
{
    public static readonly CaveBuilder.Settings caveSettings = new CaveBuilder.Settings();

    private static XUiC_ComboBoxInt terrainOffset;

    private static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveNetworks;

    private static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveEntrances;

    private static XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections> caveWater;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(XUiC_WorldGenerationWindow), "OnOpen")]
    public static void OnOpen_Postfix(XUiC_WorldGenerationWindow __instance)
    {
        if (!(__instance.ViewComponent is XUiV_Window))
            return;

        terrainOffset = __instance.GetChildById("terrainOffset") as XUiC_ComboBoxInt;
        caveNetworks = __instance.GetChildById("caveNetworks") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        caveEntrances = __instance.GetChildById("caveEntrances") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        caveWater = __instance.GetChildById("caveWater") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;

        terrainOffset.Value = (int)caveSettings.terrainOffset;
        caveNetworks.Value = caveSettings.caveNetworks;
        caveEntrances.Value = caveSettings.caveEntrances;
        caveWater.Value = caveSettings.caveWater;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(XUiC_WorldGenerationWindow), "GenerateButton_OnPressed")]
    public static bool GenerateButton_OnPressed_Prefix(XUiController _sender, int _mouseButton, XUiC_WorldGenerationWindow __instance)
    {
        caveSettings.terrainOffset = terrainOffset.Value;
        caveSettings.caveNetworks = caveNetworks.Value;
        caveSettings.caveEntrances = caveEntrances.Value;
        caveSettings.caveWater = caveWater.Value;

        return true;
    }
}