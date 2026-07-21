using System.Collections;
using System.IO;
using WorldGenerationEngineFinal;
using WorldListEntry = XUiC_WorldList.WorldListEntry;

public class XUiC_EditingToolsCaveEditor : XUiC_EditingToolsDialogBase
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<XUiC_EditingToolsCaveEditor>();

    public const string ID = "caveEditor";

    private WorldListEntry selectedWorldEntry = null;

    XUiC_WorldList worldListController;

    private string WorldName => selectedWorldEntry?.Location.Name;

    public override void OnOpen()
    {
        base.OnOpen();

        var caveNetworks = GetChildById("caveNetworks") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveEntrances = GetChildById("caveEntrances") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveWater = GetChildById("caveWater") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveSeed = GetChildById("caveSeed") as XUiC_TextInput;

        caveNetworks.Value = WorldBuilder.GenerationSelections.Default;
        caveEntrances.Value = WorldBuilder.GenerationSelections.Default;
        caveWater.Value = WorldBuilder.GenerationSelections.Default;
        caveSeed.Text = "1337";
    }

    public override void Init()
    {
        base.Init();

        worldListController = GetChildByType<XUiC_WorldList>();
        worldListController.SelectionChanged += WorldListController_SelectionChanged;

        GetChildById("btnGenerate").OnPress += ButtonGenerate_OnPress;
        GetChildById("btnDelete").OnPress += ButtonDelete_OnPress;
    }

    private void WorldListController_SelectionChanged(XUiC_List<WorldListEntry> _list, WorldListEntry _previousEntry, WorldListEntry _newEntry)
    {
        selectedWorldEntry = _newEntry;
    }

    private void ButtonGenerate_OnPress(XUiController _sender, int _mouseButton)
    {
        if (selectedWorldEntry == null)
            return;

        xui.StartCoroutine(Coroutine());
    }

    private IEnumerator Coroutine()
    {
        XUiC_ProgressWindow.Open(LocalPlayerUI.primaryUI, "Generating Caves...", null, _modal: false, _notEscClosable: true, _useShadow: true);

        var caveSettings = ReadCaveSettings();
        var caveBuilder = new CaveBuilder(caveSettings);

        yield return caveBuilder.GenerateCaveFromWorld(selectedWorldEntry.Location);

        XUiC_ProgressWindow.Close(LocalPlayerUI.primaryUI);

        worldListController.RefreshView();
    }

    private CaveBuilder.Settings ReadCaveSettings()
    {
        var caveNetworks = GetChildById("caveNetworks") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveEntrances = GetChildById("caveEntrances") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveWater = GetChildById("caveWater") as XUiC_ComboBoxEnum<WorldBuilder.GenerationSelections>;
        var caveSeed = GetChildById("caveSeed") as XUiC_TextInput;

        var settings = new CaveBuilder.Settings()
        {
            seed = caveSeed.Text.GetHashCode(),
            caveEntrances = caveEntrances.Value,
            caveNetworks = caveNetworks.Value,
            caveWater = caveWater.Value,
        };

        return settings;
    }

    private void ButtonDelete_OnPress(XUiController _sender, int _mouseButton)
    {
        if (selectedWorldEntry == null)
            return;

        XUiC_MessageBoxWindowGroup.ShowCustom(
            xui,
            Localization.Get("xuiDeleteSaveGame"),
            string.Format(Localization.Get("xuiSavegameDeleteConfirmation"), WorldName),
            "_icon",
            DeleteConfirmationHandler,
            _openMainMenuOnClose: false,
            _modal: false
        );
    }

    private void DeleteConfirmationHandler(XUiC_MessageBoxWindowGroup _box)
    {
        _box.Buttons[0].DefaultConfirm("btnConfirm", DeleteSelectedCaves, _enabled: true, 0f, 1.5f);
        _box.Buttons[2].DefaultCancel("xuiCancel", () =>
        {
        });
    }

    private void DeleteSelectedCaves()
    {
        var cavemapPath = System.IO.Path.Combine(selectedWorldEntry?.Location.FullPath, "cavemap");

        if (Directory.Exists(cavemapPath))
        {
            Directory.Delete(cavemapPath, true);
        }

        worldListController.RefreshView();
    }
}
