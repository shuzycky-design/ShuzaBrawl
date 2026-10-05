using IndusBrawl.Laser.Logic.Home.Items;
using IndusBrawl.Laser.Logic.Ranked;
using IndusBrawl.Laser.Server.Logic.Game;
using IndusBrawl.Laser.Server.Networking;
using System;
using System.Threading.Tasks;
using System.Collections.Generic; // ← ДОБАВИТЬ
using IndusBrawl.Laser.Logic.Battle.Structures; // ← ДОБАВИТЬ

namespace IndusBrawl.Laser.Server.Logic
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Networking.Session;

    public class ServerListener : LogicServerListener
    {
        public ClientAvatar GetAvatar(long id)
        {
            return Accounts.Load(id).Avatar;
        }

        public LogicGameListener GetGameListener(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).GameListener;
            }
            return null;
        }

        public HomeMode GetHomeMode(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).Home;
            }
            return null;
        }

        public bool IsPlayerOnline(long id)
        {
            return Sessions.IsSessionActive(id);
        }
        
        // ИСПРАВЛЕНО: thestealdevRankedMatchPlayer → mp_thestealdev
        public void StartMatchBattle(List<mp_thestealdev> entries, EventData edata, int Location = -1, int id = 0, int round = 0)
        {
            List<MatchmakingEntry> list = new();

            foreach(mp_thestealdev ranked in entries) // ИСПРАВЛЕНО: thestealdevRankedMatchPlayer → mp_thestealdev
            {
                Connection conn = null;
                if (Sessions.IsSessionActive(ranked.i_thestealdev)) // ИСПРАВЛЕНО: thestealdevAccountId → i_thestealdev
                    conn = Sessions.GetSession(ranked.i_thestealdev).Connection;
                if (conn == null)
                {
                    Console.WriteLine($"[ServerListener.StartMatchBattle] Пропуск игрока {ranked.i_thestealdev}: нет сессии");
                    continue;
                }
                MatchmakingEntry e = new(conn);
                e.PrefferedTeam = ranked.titi_thestealdev; // ИСПРАВЛЕНО: thestealdevTeamIndex → titi_thestealdev
                e.CharacterId = ranked.ca_thestealdev; // ИСПРАВЛЕНО: thestealdevCharacter → ca_thestealdev
                e.PlayerTeamId = ranked.titi_thestealdev; // ДОБАВЛЕНО
                Console.WriteLine($"[ServerListener.StartMatchBattle] Игрок {ranked.i_thestealdev}: character={ranked.ca_thestealdev}, team={ranked.titi_thestealdev}");
                list.Add(e);
            }
            RankedMatchRegulator.StartMatchBattle(list, edata, Location, id, round);
        }
        
        // ИСПРАВЛЕНО: thestealdevRankedMatch → RankedMatch
        public RankedMatch GetRankedMatch(long id) => RankedMatchRegulator.Get(id);
        
        public void SaveAccount(long id)
        {
            Task.Run(() =>
            {
                try
                {
                    var account = Accounts.Load(id);
                    if (account == null)
                    {
                        Console.WriteLine($"[ServerListener] SaveAccount({id}) error: account not found");
                        return;
                    }
                    
                    var session = Sessions.GetSession(id);
                    if (session != null && session.Home != null && session.Home.Avatar != null)
                    {
                        account.Avatar = session.Home.Avatar;
                    }
                    
                    Accounts.Save(account);
                    Console.WriteLine($"[ServerListener] SaveAccount({id}) completed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServerListener] SaveAccount({id}) error: {ex.Message}");
                }
            });
        }
    }
}