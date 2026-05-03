using UnityEngine;
using System.Collections.Generic;

public static class GameModeData
{
    public class ModeInfo
    {
        public int[] PlayerRolesInTeam;
    }

    public static Dictionary<E_Mode, ModeInfo> Modes = new Dictionary<E_Mode, ModeInfo>()
    {
        {
            E_Mode.Competition1v1, new ModeInfo { PlayerRolesInTeam = new int[] { 0 } }
        }
    };

    public static ModeInfo GetModeInfo(E_Mode mode)
    {
        if (Modes.TryGetValue(mode, out ModeInfo info))
            return info;

        Debug.LogError("Error tring to get mode data");
        return null;
    }
}
