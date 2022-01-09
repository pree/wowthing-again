﻿using System.Collections.Generic;
using Wowthing.Lib.Models.Player;

namespace Wowthing.Web.Models
{
    public class UserAchievementData
    {
        public Dictionary<int, int> Achievements { get; set; }
        public Dictionary<int, List<int[]>> Criteria { get; set; }
        public Dictionary<int, Dictionary<int, PlayerCharacterAddonAchievementsAchievement>> AddonAchievements { get; set; }
    }
}
