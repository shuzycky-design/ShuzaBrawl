namespace IndusBrawl.Laser.Logic.Battle.Objects
{
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.CompilerServices;
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Logic.Data;

    public class GameObjectManager//+32 type 64 encode 48IsAlive
    {
        public Queue<GameObject> AddObjects;
        private Queue<GameObject> RemoveObjects;

        private BattleMode Battle;
        public List<GameObject> GameObjects;
        public List<Petrol> Petrols;

        private int ObjectCounter;
        public int Ended = -1;

        public GameObjectManager(BattleMode battle)
        {
            Battle = battle;
            GameObjects = new List<GameObject>();
            Petrols = new List<Petrol>();

            AddObjects = new Queue<GameObject>();
            RemoveObjects = new Queue<GameObject>();
        }

        public GameObject[] GetGameObjects()
        {
            return GameObjects.ToArray();
        }
        public Projectile[] GetProjectiles()
        {
            List<Projectile> objects = new List<Projectile>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 1)
                {
                    objects.Add((Projectile)obj);
                }
            }
            return objects.ToArray();
        }

        public Character[] GetCharacters()
        {
            List<Character> objects = new List<Character>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 0)
                {
                    objects.Add((Character)obj);
                }
            }
            foreach (GameObject obj in AddObjects)
            {
                if (obj.GetObjectType() == 0)
                {
                    objects.Add((Character)obj);
                }
            }
            return objects.ToArray();
        }

        public Item[] GetItems()
        {
            List<Item> objects = new List<Item>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 3)
                {
                    objects.Add((Item)obj);
                }
            }
            foreach (GameObject obj in AddObjects)
            {
                if (obj.GetObjectType() == 3)
                {
                    objects.Add((Item)obj);
                }
            }
            return objects.ToArray();
        }

        public AreaEffect[] GetAreaEffects()
        {
            List<AreaEffect> objects = new List<AreaEffect>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 2)
                {
                    objects.Add((AreaEffect)obj);
                }
            }
            foreach (GameObject obj in AddObjects)
            {
                if (obj.GetObjectType() == 2)
                {
                    objects.Add((AreaEffect)obj);
                }
            }
            return objects.ToArray();
        }
        public Projectile[] GetSpecificProjectiles(GameObject gameObject)
        {
            List<Projectile> objects = new List<Projectile>();

            foreach (GameObject obj in GameObjects)
            {
                if (obj.GetObjectType() == 1 && obj.GetIndex() / 16 != gameObject.GetIndex() / 16 && obj.GetPosition().GetDistance(gameObject.GetPosition()) <= 600)
                {
                    objects.Add((Projectile)obj);
                }
            }
            return objects.ToArray();
        }

        public BattleMode GetBattle()
        {
            return Battle;
        }

        public void PreTick()
        {
            for (int i = 0; i < GameObjects.Count; i++)
            {

                GameObject gameObject = GameObjects[i];
                if (gameObject == null)
                {
                    GameObjects.Remove(gameObject);
                    i--;
                    continue;
                }
                if (gameObject.ShouldDestruct())
                {
                    gameObject.OnDestruct();
                    RemoveGameObject(gameObject);
                }
                else
                {
                    gameObject.ResetEventsOnTick();
                }
            }

            while (AddObjects.Count > 0)
            {
                GameObjects.Add(AddObjects.Dequeue());
            }
            for (int i = 0; i < GameObjects.Count; i++)
            {
                GameObject gameObject = GameObjects[i];
                if (gameObject == null)
                {
                    GameObjects.Remove(gameObject);
                    i--;
                    continue;
                }
            }
            while (RemoveObjects.Count > 0)
            {
                GameObjects.Remove(RemoveObjects.Dequeue());
            }
        }

        public void Tick()
        {
            //try
            {
                for (int i = 0; i < GameObjects.Count; i++)
                {
                    GameObject gameObject = GameObjects[i];
                    if (gameObject == null)
                    {
                        GameObjects.Remove(gameObject);
                        i--;
                        continue;
                    }
                    gameObject.Tick();
                }
            }


            //catch (Exception)
            //{
            //    Battle.GameOver();
            //    Battle.IsGameOver = true;
            //}
            //if (GetBattle().GetTicksGone() == 180)
            //{
            //    foreach (GameObject gameObject in GameObjects)
            //    {
            //        gameObject.OnDestruct();
            //        RemoveGameObject(gameObject);

            //    }
            //}
        }

        public void AddGameObject(GameObject gameObject)
        {
            gameObject.AttachGameObjectManager(this, GlobalId.CreateGlobalId(gameObject.GetObjectType(), ObjectCounter++));
            AddObjects.Enqueue(gameObject);
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            RemoveObjects.Enqueue(gameObject);
        }

        public GameObject GetGameObjectByID(int globalId)
        {
            GameObject gameObject = GameObjects.Find(obj => obj.GetGlobalID() == globalId);
            if (gameObject == null) gameObject = AddObjects.ToList().Find(obj => obj.GetGlobalID() == globalId);
            return gameObject;
        }

        public List<GameObject> GetGameObjectByCloneOfObjectGlobalId(int globalId)
        {
            return GameObjects.FindAll(obj => obj.GetCloneOfObjectGlobalId() == globalId);
        }

        public List<GameObject> GetVisibleGameObjects(int teamIndex, int ownObjectGlobalId)
        {
            List<GameObject> objects = new List<GameObject>();
            try
            {
                bool IsInRealm = false;
                GameObject character = GetGameObjectByID(ownObjectGlobalId);
                if (character != null)
                {
                    IsInRealm = character.IsInRealm;
                }
                foreach (GameObject obj in GameObjects)
                {
                    if (obj.IsInRealm ^ IsInRealm) continue;
                    if (obj.GetFadeCounter() > 0 || obj.GetIndex() / 16 == teamIndex && GetBattle().StoryMode == null)
                    {
                        objects.Add(obj);
                    }
                }
                foreach (GameObject obj in AddObjects)
                {
                    try
                    {
                        if (obj == null)
                        {
                            Parallel.ForEach(GetBattle().m_players, player =>
                            {
                                if (player.GameListener != null)
                                {
                                    ServerErrorMessage serverErrorMessage = new ServerErrorMessage();
                                    try
                                    {
                                        player.GameListener.SendMessage(serverErrorMessage);
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("Battle stopped");
                                    }
                                }
                            });

                            GetBattle().m_updateTimer.Dispose();
                            GetBattle().IsGameOver = true;
                        }
                        if (obj == null) continue;
                        if (obj.IsInRealm ^ IsInRealm) continue;
                    }
                    catch (Exception ex)
                    {
                        Parallel.ForEach(GetBattle().m_players, player =>
                        {
                            if (player.GameListener != null)
                            {
                                ServerErrorMessage serverErrorMessage = new ServerErrorMessage();
                                try
                                {
                                    player.GameListener.SendMessage(serverErrorMessage);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Battle stopped with exception! Message: " + ex.Message + " Trace: " + ex.StackTrace);
                                }
                            }
                        });

                        GetBattle().m_updateTimer.Dispose();
                        GetBattle().IsGameOver = true;

                    }
                    if (obj.GetFadeCounter() > 0 || obj.GetIndex() / 16 == teamIndex)
                    {
                        objects.Add(obj);
                    }
                }
            }
            catch (Exception)
            {
                Battle.GameOver();
                Battle.IsGameOver = true;
                Battle.m_updateTimer.Dispose();
            }


            return objects;
        }
   private int[] CalculateRounds(int own)
   {
       int[] p = { GetBattle().RoundWins[0], GetBattle().RoundWins[1], GetBattle().RoundWins[2] };
       if (own == 1)
       {
           if (GetBattle().RoundWins[0] == 0) p[0] = 1;
           if (GetBattle().RoundWins[1] == 0) p[0] = 1;
           if (GetBattle().RoundWins[2] == 0) p[0] = 1;
       }
       return p;
   }
        public void Encode(BitStream bitStream, TileMap tileMap, int ownObjectGlobalId, int playerIndex, int teamIndex)
        {
            BattlePlayer[] players = Battle.GetPlayers();
            List<GameObject> visibleGameObjects = GetVisibleGameObjects(teamIndex, ownObjectGlobalId);

            int GameModeVariation = Battle.GetGameModeVariation();
            bitStream.WritePositiveInt(ownObjectGlobalId, 21);
            bitStream.WritePositiveInt(0, 21);
            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);

            //for (int i = 0; i < players.Length; i++)
            //{
            //    {
            //        bitStream.WritePositiveIntMax3(players[i].HeroIndex);  //got trashed in v53 lol
            //        bitStream.WriteBoolean(false);
            //    }
            //}
 switch (GameModeVariation)
 {
     case 0:
     case 33:
         bitStream.WritePositiveVIntMax65535(Battle.GetGemGrabCountdown());
         break;
     case 17:
         bitStream.WritePositiveIntMax7(1);
         bitStream.WritePositiveIntMax127(GetBattle().GetZone1Score());
         bitStream.WritePositiveIntMax127(GetBattle().GetZone2Score());
         break;
     case 19:
         bitStream.WritePositiveIntMax7(GetBattle().GetTeam1King());
         bitStream.WritePositiveIntMax7(GetBattle().GetTeam2King());
         break;
     case 20:
     case 24:
     case 35: // it from v54 btw
         bitStream.WritePositiveIntMax4095(!GetBattle().nokbool ? GetBattle().RoundTicks : 4095); // round ticks
         bitStream.WriteBoolean(GetBattle().nokbool);
         bitStream.WriteBoolean(GetBattle().nokbool);

         bitStream.WriteBoolean(GetBattle().nokbool);
         bitStream.WriteBoolean(GetBattle().nokbool);
         bitStream.WriteBoolean(GetBattle().nokbool);

         bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[0]);
         bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[1]);
         bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[2]);
         break;

 }

            bitStream.WriteBoolean(false); // idk
            bitStream.WriteIntMax15(Ended);

            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);//全都没用 (переводится как безполезно, больше сюда не лезу)

            if (tileMap.Width < 22)
            {
                bitStream.WritePositiveInt(0, 5); // 0xa820a8
                bitStream.WritePositiveInt(0, 6); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 5); // 0xa820c0
            }
            else
            {
                bitStream.WritePositiveInt(0, 6); // 0xa820a8
                bitStream.WritePositiveInt(0, 6); // 0xa820b4
                bitStream.WritePositiveInt(tileMap.Width - 1, 6); // 0xa820c0
            }
            bitStream.WritePositiveInt(tileMap.Height - 1, 6); // 0xa820d0

            for (int i = 0; i < tileMap.Width; i++)
            {
                for (int j = 0; j < tileMap.Height; j++)
                {
                    var tile = tileMap.GetTile(i, j, true, true);
                    if (tile.Data.RespawnSeconds > 0 || tile.Data.Name.StartsWith("Indestructible") || tile.Code == 'J' || tile.Data.IsDestructible || tile.Data.RestoreAfterDynamicOverlap)
                    {
                        //if (j == 13)
                        //{
                        //    bitStream.WriteBoolean(false);
                        //    continue;
                        //}
                        bitStream.WriteBoolean(tile.IsDestructed() && tileMap.GetTile(i, j, true).IsDestructed());
                    }
                }
            }


            bitStream.WritePositiveVIntMax65535OftenZero(tileMap.NewTiles.Count);
            {
                for (int i = 0; i < tileMap.NewTiles.Count; i++)
                {
                    bitStream.WritePositiveIntMax4095(tileMap.NewTiles[i].TileX + tileMap.Width * tileMap.NewTiles[i].TileY);
                    bitStream.WritePositiveIntMax15(tileMap.NewTiles[i].Data.DynamicCode);
                }
            }
            bitStream.WritePositiveVIntMax65535OftenZero(0);

            bitStream.WritePositiveVIntMax65535OftenZero(Petrols.Count);
            if (Petrols.Count >= 1)
            {
                for (int i = 0; i < Petrols.Count; i++)
                {
                    bitStream.WritePositiveIntMax63(Petrols[i].X);
                    bitStream.WritePositiveIntMax63(Petrols[i].Y);
                    bitStream.WriteIntMax15(Petrols[i].TeamIndex);
                    bitStream.WriteIntMax15(Petrols[i].PlayerIndex);
                }
            }
            for (int i = 0; i < players.Length; i++)
            {
                if (i == playerIndex)
                {
                    //bitStream.WritePositiveInt((((Character)GetGameObjectByID(players[i].OwnObjectId))?.DisplayUltiTmp ?? false) ? 4000 : players[i].GetUltiCharge(), 12);
                    bitStream.WritePositiveIntMax4095(players[i].GetUltiCharge());
                    bitStream.WritePositiveIntMax4095(players[i].GetOverCharge());//OverCharge
                    bitStream.WritePositiveInt(0, 1);
                    bitStream.WritePositiveInt(0, 1);
                    bitStream.WritePositiveVIntMax255OftenZero(0);
                    bitStream.WritePositiveInt(0, 1);
                }
                bitStream.WritePositiveIntMax3(players[i].HeroIndex);//v53
                bitStream.WriteBoolean(false);
                //if (players[i].Accessory != null)
                //{
                //    players[i].Accessory.Encode(bitStream, i == playerIndex);
                //}
                bitStream.WriteBoolean(false);//v53 (idk)
                bitStream.WriteBoolean(players[i].HasUlti());
                if (bitStream.WriteBoolean(false)) //(DO NOT TRUE PLEASE!!!)
                {
                    ;
                }
                
                bitStream.WriteBoolean(false); //(DO NOT TRUE PLEASE!!!)
                if (GameModeVariation == 6) bitStream.WritePositiveInt(0, 4);
                if (GameModeVariation == 3) bitStream.WriteBoolean(false);
                //if (GameModeVariation == 15) bitStream.WriteBoolean(false);
                // Предположительно счётчик "пальцев вверх" игрока; отправляем его только на экране результатов
                bitStream.WritePositiveVIntMax255OftenZero(GetBattle().IsGameOver ? players[i].Likes : 0);
                bitStream.WriteBoolean(false); // is allow to use pin (visual)
                bitStream.WriteBoolean(false);// maybie is allow to use spray (old comment: spray)
                bitStream.WriteBoolean(players[i].IsUsingPin(GetBattle().GetTicksGone()));
                if (players[i].IsUsingPin(GetBattle().GetTicksGone()))
                {
                    bitStream.WriteIntMax7(players[i].PinIndex);
                    bitStream.WritePositiveIntMax16383(players[i].StartUsingPinTicks);
                }
                //bitStream.WriteBoolean(true);
                //{
                //    bitStream.WriteIntMax7(0);
                //    bitStream.WritePositiveIntMax16383(0);
                //}

            }
            bitStream.WritePositiveVIntMax65535OftenZero(0);
            bitStream.WritePositiveVIntMax255OftenZero(0);
            if (((2129332653 >> GameModeVariation) & 1) != 0) bitStream.WritePositiveVIntMax65535OftenZero(0);


            switch (GameModeVariation)
            {
                case 5:
                    bitStream.WriteBoolean(!(GetBattle().GetGoalTeam() != -1));
                    bitStream.WriteIntMax1(GetBattle().GetGoalTeam() != -1 ? GetBattle().GetGoalTeam() : 0);
                    break;
                case 24:
                   bitStream.WritePositiveIntMax15(3);
                   bitStream.WritePositiveIntMax4095(2000); // Dым?
                   bitStream.WritePositiveIntMax1(1);

                   bitStream.WritePositiveIntMax1(1);
                   bitStream.WritePositiveIntMax7(0); // давайка будет 0

                   bitStream.WritePositiveIntMax1(0); // давайка будет 0
                   break;
                case 6:
                    bitStream.WritePositiveIntMax15(Battle.GetPlayersAliveCountForBattleRoyale());
                    break;
                //case 15:
                //    bitStream.WritePositiveIntMax15(1);
                //    break;
                case 16:
                    bitStream.WritePositiveIntMax8191(0);
                    bitStream.WritePositiveIntMax8191(0);
                    break;
                // В методе Encode найдите case 20:
// В методе Encode найдите case 20:

                case 27:
                    bitStream.WritePositiveIntMax7(0);
                    break;
                case 19:
                    int hp1 = 0;
                    int hp2 = 0;
                    foreach (Character character in GetCharacters())
                    {
                        if (!character.CharacterData.IsHero()) continue;
                        if (!character.IsAlive()) continue;
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetBattle().GetTeam1King()) hp1 = character.GetHitpointPercentage();
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetBattle().GetTeam2King()) hp2 = character.GetHitpointPercentage();
                    }
                    bitStream.WritePositiveIntMax127(hp1);
                    bitStream.WritePositiveIntMax127(hp2);
                    break;

                case 9:
                    bitStream.WritePositiveIntMax7(1);
                    break;
            }
            if (GameModeUtil.HasTwoBases(GameModeVariation))
            {
                int hp1 = 0;
                int hp2 = 0;
                foreach (Character character in GetCharacters())
                {
                    if (!character.CharacterData.IsBase()) continue;
                    if (!character.IsAlive()) continue;
                    if (character.GetIndex() / 16 == 0) hp1 = character.GetHitpointPercentage();
                    else hp2 = character.GetHitpointPercentage();
                }
                bitStream.WritePositiveIntMax127(hp1);
                bitStream.WritePositiveIntMax127(hp2);
            }
            if (GameModeVariation == 13) bitStream.WritePositiveIntMax131071(0); // Damage Per Second
            if (GameModeVariation == 7) bitStream.WritePositiveIntMax127(0);
            if (GameModeVariation == 8) bitStream.WritePositiveIntMax127(0);
            if (GameModeVariation == 29) bitStream.WritePositiveIntMax127(100);
            if (GameModeVariation == 10) bitStream.WritePositiveIntMax127(0);
            //if (GameModeVariation == 15) bitStream.WritePositiveIntMax31(1);
            for (int i = 0; i < players.Length; i++)
            {
                //bitStream.WriteBoolean(false); 
                //bitStream.WriteBoolean(false);
                if (GameModeVariation != 6 && GameModeVariation != 15)
                {
                    bitStream.WriteBoolean(true);
                    bitStream.WritePositiveVIntMax255(players[i].GetScore());
                }
                else
                {
                    bitStream.WriteBoolean(false);
                }
                if (bitStream.WriteBoolean(players[i].KillList.Count > 0))
                {
                    if (GameModeVariation == 6)
                    {
                        bitStream.WritePositiveIntMax15(0);
                    }
                    else
                    {
                        bitStream.WritePositiveIntMax15(players[i].KillList.Count);
                        for (int j = 0; j < players[i].KillList.Count; j++)
                        {
                            bitStream.WritePositiveIntMax15(players[i].KillList[j].PlayerIndex);
                            bitStream.WriteIntMax7(players[i].KillList[j].BountyStarsEarned);
                        }
                    }
                }
            }

            if (false)
            {
                visibleGameObjects = visibleGameObjects.Take(1).ToList();
                bitStream.WritePositiveVIntMax65535(1);
            }
            else bitStream.WritePositiveVIntMax65535(visibleGameObjects.Count);

            foreach (GameObject gameObject in visibleGameObjects)
            {
                ByteStreamHelper.WriteDataReference(bitStream, gameObject.GetDataId());
                //Debugger.Print(gameObject.GetDataId() + "");
            }

            foreach (GameObject gameObject in visibleGameObjects)
            {
                bitStream.WritePositiveVIntMax65535(GlobalId.GetInstanceId(gameObject.GetGlobalID())); // 0x2381b4
            }
            bitStream.WriteBoolean(false);
            foreach (GameObject gameObject in visibleGameObjects)
            {
                gameObject.Encode(bitStream, gameObject.GetGlobalID() == ownObjectGlobalId, playerIndex + teamIndex * 16, teamIndex);
                //bitStream.WritePositiveVIntMax65535OftenZero(0);

                //Debugger.Print("e:"+gameObject.GetDataId());
            }
            if (GameModeVariation == 7)
            {
                if (bitStream.WriteBoolean(true))
                {
                    bitStream.WritePositiveIntMax32767(10000);
                    bitStream.WritePositiveIntMax65535(10000);
                }
            }
            if (GameModeVariation == 8)
            {
                if (bitStream.WriteBoolean(true))
                {
                    bitStream.WritePositiveIntMax32767(10000);
                    bitStream.WritePositiveIntMax65535(10000);
                }
            }
            //if (GameModeVariation == 29)
            //{
            //    if (bitStream.WriteBoolean(true))
            //    {
            //        bitStream.WritePositiveIntMax32767(10000);
            //        bitStream.WritePositiveIntMax65535(10000);
            //    }
            //}
            if (GameModeVariation == 10)
            {
                if (bitStream.WriteBoolean(true))
                {
                    bitStream.WritePositiveIntMax32767(10000);
                    bitStream.WritePositiveIntMax65535(10000);
                }
            }
        }
    }
}