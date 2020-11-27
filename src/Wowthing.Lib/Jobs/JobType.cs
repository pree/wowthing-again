﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Wowthing.Lib.Jobs
{
    public enum JobType
    {
        UserCharacters = 0,

        Character = 100,
        CharacterMounts,
      
        DataPlayableClass = 200,
        DataReputationTiers,
        DataTitle,

        // Scheduled jobs
        CacheStatic = 1000,
        DataPlayableRaceIndex,
        DataPlayableClassIndex,
        DataRealmIndex,
        DataReputationTiersIndex,
        DataTitleIndex,
    }
}
