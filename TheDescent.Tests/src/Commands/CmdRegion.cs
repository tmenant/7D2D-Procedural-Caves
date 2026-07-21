using System.Collections.Generic;

public class CmdRegion : CmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<CmdRegion>();

    public override string[] GetCommands()
    {
        return new string[] { "region" };
    }

    public override void Execute(List<string> args)
    {
        var dirname = @"ignore/cavemap";
        var totalBlocks = 0;

        for (int i = 0; i < 256; i++)
        {
            string filename = $"{dirname}/region_{i}.bin";

            var timer = ProfilingUtils.StartTimer();
            var region = new CaveRegion(filename);
            var blocksCount = region.BlockCount;

            totalBlocks += blocksCount;

            logger.Info($"{i}: ChunkCount={region.ChunkCount}, blocks: {blocksCount:N0} timer={timer.ElapsedMilliseconds}ms");
        }

        logger.Info($"Total blocks: {totalBlocks:N0}");
    }

}