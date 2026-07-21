using System.Collections.Generic;

public class DebugItemConsoleCmd : ConsoleCmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<DebugItemConsoleCmd>();

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var itemValue = player.inventory.holdingItemItemValue;
        var itemData = player.inventory.holdingItemData;
        var itemClass = player.inventory.holdingItem;

        foreach (var partName in player.parts.Keys)
        {
            logger.Info($"[DebugItem] part: {partName}");
        }

        foreach (var mod in itemValue.Modifications)
        {
            logger.Info($"[DebugItem] mod: {mod.ItemClass.Name}");
        }

        if (itemValue == null)
        {
            logger.Warning($"[DebugItem] player is not holding an item");
            return;
        }

        logger.Info($"[DebugItem] name: {itemValue.ItemClass.Name}");
        logger.Info($"[DebugItem] Quality: {itemValue.Quality}");
        logger.Info($"[DebugItem] useTimes: {itemValue.UseTimes}");
        logger.Info($"[DebugItem] maxUseTimes: {itemValue.MaxUseTimes}");
    }

    public override string[] getCommands()
    {
        return new string[] { "debugitem", "di" };
    }

    public override string getDescription()
    {
        return "DebugItemConsoleCmd";
    }
}
