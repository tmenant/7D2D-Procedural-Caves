using HarmonyLib;


/// <summary>
/// add a new cave edition menu into the EditingTools window
/// </summary>
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
    }
}
