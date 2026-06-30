using System.Collections.Generic;


public class CmdLogging : CmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<CmdLogging>();

    public override string[] GetCommands()
    {
        return new string[] { "logging" };
    }

    public override void Execute(List<string> args)
    {
        Log.Out("Out");
        Log.Warning("Warning");
        Log.Error("Error");

        logger.Debug("Debug ?");
        logger.Info("Info :)");
        logger.Warning("Warning !");
        logger.Error("Error :(");

        // logger.Info("Debug ?");
        // logger.Info("Info :)");
        // logger.Warning("Warning !");
        // logger.Error("Error :()");
    }

}