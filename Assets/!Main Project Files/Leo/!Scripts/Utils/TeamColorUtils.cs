using _Main_Project_Files.Leo._Scripts.Agents;
using UnityEngine;

namespace _Main_Project_Files._Scripts.Utils
{
    public static class TeamColorUtils
    {
        public static Color GetColor(TeamColor team)
        {
            return team switch
            {
                TeamColor.Red => new Color(0.9f, 0.3f, 0.3f),
                TeamColor.Blue => new Color(0.3f, 0.3f, 0.9f),
                TeamColor.Green => new Color(0.3f, 0.9f, 0.3f),
                TeamColor.Yellow => new Color(0.9f, 0.9f, 0.3f),
                _ => Color.gray
            };
        }
    }
}
