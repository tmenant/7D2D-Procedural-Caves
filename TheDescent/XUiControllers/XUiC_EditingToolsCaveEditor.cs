using WorldListEntry = XUiC_WorldList.WorldListEntry;

public class XUiC_EditingToolsCaveEditor : XUiC_EditingToolsDialogBase
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<XUiC_EditingToolsCaveEditor>();

    public const string ID = "caveEditor";

    private WorldListEntry selectedWorldEntry = null;

    public override void Init()
    {
        base.Init();

        GetChildByType<XUiC_WorldList>().SelectionChanged += WorldListController_SelectionChanged;

        GetChildById("btnGenerate").OnPress += ButtonGenerate_OnPress;
        GetChildById("btnDelete").OnPress += ButtonDelete_OnPress;
    }

    private void WorldListController_SelectionChanged(XUiC_List<WorldListEntry> _list, WorldListEntry _previousEntry, WorldListEntry _newEntry)
    {
        selectedWorldEntry = _newEntry;
    }

    private void ButtonGenerate_OnPress(XUiController _sender, int _mouseButton)
    {
        logger.Info($"ButtonGenerate_OnPress: '{selectedWorldEntry?.Location.Name}'");
    }

    private void ButtonDelete_OnPress(XUiController _sender, int _mouseButton)
    {
        logger.Info($"ButtonDelete_OnPress: '{selectedWorldEntry?.Location.Name}'");
    }
}