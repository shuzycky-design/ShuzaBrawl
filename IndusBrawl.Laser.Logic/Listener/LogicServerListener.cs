﻿using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Ranked;

namespace IndusBrawl.Laser.Logic.Listener
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Battle.Structures; // ← ДОБАВИТЬ

    public interface LogicServerListener
    {
        public static LogicServerListener Instance;

        ClientAvatar GetAvatar(long id);
        LogicGameListener GetGameListener(long id);
        HomeMode GetHomeMode(long id);
        bool IsPlayerOnline(long id);
        
        // ИСПРАВЛЕНО: thestealdevRankedMatchPlayer → mp_thestealdev
        void StartMatchBattle(List<mp_thestealdev> entries, EventData edata, int Location = -1, int id = 0, int round = 0);
        
        // ИСПРАВЛЕНО: thestealdevRankedMatch → RankedMatch
        RankedMatch GetRankedMatch(long id);
        
        void SaveAccount(long id);
    }
}