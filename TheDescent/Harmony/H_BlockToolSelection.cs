using HarmonyLib;


[HarmonyPatch(typeof(BlockToolSelection), "CheckKeys")]
public class BlockToolSelection_CheckKeys
{
    public static bool Prefix(ItemInventoryData _data, WorldRayHitInfo _hitInfo, PlayerActionsLocal playerActions, BlockToolSelection __instance)
    {
        if (playerActions.SelectionDelete.IsPressed)
        {
            BlockSelectionUtils.ClearSelection();
        }

        return true;
    }
}
