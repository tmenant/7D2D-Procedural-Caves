
using System.Collections;
using System.Collections.Generic;


public class WorldGenConsoleCmd : ConsoleCmdAbstract
{
    public override bool AllowedInMainMenu => true;

    public override string[] getCommands()
    {
        return new string[] { "worldgen", "cavegen" };
    }

    public override string getDescription()
    {
        return "Generates caves over an existing World";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        Log.Out("[WorldGenConsoleCmd] cave generation started.");

        var worldName = string.Join(" ", _params);

        if (worldName == "")
        {
            worldName = "Pregen06k01";
        }

        var caveBuilder = new CaveBuilder();
        var worldDatas = new WorldDatas(worldName);
        var coroutine = caveBuilder.GenerateCaveFromWorld(worldDatas);

        worldDatas.Debug();

        GameManager.Instance.StartCoroutine(coroutine);
    }
}

