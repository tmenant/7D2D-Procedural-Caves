using System.Collections.Generic;


public class WorldGenConsoleCmd : ConsoleCmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<WorldGenConsoleCmd>();

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
        string worldName = _params.Count > 1 ? _params[1] : "";

        if (string.IsNullOrEmpty(worldName))
        {
            logger.Warning("No world name was provided");
            return;
        }

        var caveBuilder = new CaveBuilder();
        var worldDatas = new WorldDatas(worldName);

        worldDatas.Debug();

        ThreadManager.StartCoroutine(caveBuilder.GenerateCaveFromWorld(worldDatas));
    }
}

