using HarmonyLib;

[HarmonyPatch(typeof(XUiC_EditingTools), "onWindowSelected")]
public class H_XUiC_EditingTools_onWindowSelected
{
    public static void Postfix(XUiC_WindowSelector _sender, string _windowId, XUiC_EditingTools __instance)
    {
        GUIWindowManager windowManager = __instance.xui.playerUI.windowManager;

        if (_windowId == "caveEditor")
        {
            windowManager.Open(XUiC_EditingToolsCaveEditor.ID, _bModal: true);
        }

        // case "rwgPreviewer":
        //     if (!XUiC_WorldGenerationWindow.IsWindowOpen(xui))
        //     {
        //         XUiC_WorldGenerationWindow.Open(xui, XUiC_MainMenu.ID);
        //     }
        //     break;
        // case "poiEditor":
        //     windowManager.Open(XUiC_EditingToolsPoiEditor.ID, _bModal: true);
        //     break;
        // case "worldEditor":
        //     windowManager.Open(XUiC_WorldEditor.ID, _bModal: true);
        //     break;
    }
}
