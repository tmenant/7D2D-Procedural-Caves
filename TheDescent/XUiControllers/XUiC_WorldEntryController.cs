using System.IO;

public class XUiC_WorldEntryController : XUiC_WorldList.EntryController
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<XUiC_WorldEntryController>();

    private bool hasCave = false;

    public override bool GetBindingValueInternal(ref string _value, string _bindingName)
    {
        switch (_bindingName)
        {
            case "hascave":
                _value = this.hasCave.ToString();
                return true;

            default:
                break;
        }

        return base.GetBindingValueInternal(ref _value, _bindingName);
    }

    public override void SetEntry(XUiC_WorldList.WorldListEntry _data)
    {
        base.SetEntry(_data);

        hasCave = false;

        if (entryData != null)
        {
            hasCave = entryData.HasCave();
        }
    }
}