namespace IndusBrawl.Laser.Server.Handler
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Battle.Level;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Data.Helper;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Command.Home;
    using IndusBrawl.Laser.Logic.Command.Avatar;
    using IndusBrawl.Laser.Logic.Listener;
    using IndusBrawl.Laser.Logic.Message.Account;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Logic.Util;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Server.Database.Cache;
    using IndusBrawl.Laser.Server.Database.Models;
    using IndusBrawl.Laser.Server.Networking.Session;
    using System.Reflection;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using System.Numerics; // Добавьте эту строку

    public static class CmdHandler
    {
        public static void Start()
        {
            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();
                    if (cmd == null) continue;
                    if (!cmd.StartsWith("/")) continue;
                    HandleCmd(cmd);
                }
                catch (Exception) { }
            }
        }
        
        public static void HandleCmd(string cmd, long OwnAccountId = -1)
        {
            cmd = cmd.Substring(1);
            string[] args = cmd.Split(" ");
            if (args.Length < 1) return;
            switch (args[0])
            {
                case "givepassplus":
                    ExecuteGivePremiumPassPlus(args);
                    break;
                case "vip":
                    ExecuteGivePremiumToAccount(args);
                    break;
                    case "changehomevalue":
    ExecuteChangeHomeValue(args);
    break;
    case "resetbphome":
    ExecuteResetBPHome(args);
    break;
    case "checkhome":
    ExecuteCheckHome(args);
    break;
    case "checkaccount":
    ExecuteCheckAccount(args);
    break;
    case "resetbpnow":
    ExecuteResetBPNow(args);
    break;
    case "resetbpfinal":
    ExecuteResetBPFinal(args);
    break;
    case "resetallbp":
    ExecuteResetAllBP();
    break;
                case "coinnotif":
                    CoinNotif(args);
                    break;
                case "gem":
                    gem(args);
                    break;
                case "debugbp":
                    ExecuteDebugBP(args);
                    break;
                case "clubnotif":
                    ClubNotif(args);
                    break;
                    case "resetbp4":
    ExecuteResetBPAuto(args);
    break;
                case "ban":
                    ExecuteBanAccount(args);
                    break;
                case "cheater":
                    ExecuteBanCheater(args);
                    break;
                case "tempban":
                    ExecuteTempBanAccount(args);
                    break;
                case "ToID":
                    Console.WriteLine(LogicLongCodeGenerator.ToId(args[1]));
                    break;
                case "unban":
                    ExecuteUnbanAccount(args);
                    break;
case "premiumultra":
    ExecuteGivePremiumUltra(args);
    break;
case "givenamenew":
        ExecuteGiveNameNew(args);
        break;    
case "red":
        ExecuteRedName(args);
        break; 
case "devname":
    ExecuteDevName(args);
    break;
            case "changename":
                    ExecuteChangeNameForAccount(args);
                    break;
                case "getvalue":
                    ExecuteGetFieldValue(args);
                    break;
                case "findbp":
                    ExecuteFindBattlePassFields(args);
                    break;
                case "listhome":
                    ExecuteListHomeFields(args);
                    break;
                    case "resetbp":
    ExecuteResetBattlePass(args);
    break;
                case "checkvip":
                    ExecuteCheckVIPStatus(args);
                    break;
                case "changevalue":
                    ExecuteChangeValueForAccount(args);
                    break;
                case "unlockall":
                    ExecuteUnlockAllForAccount(args);
                    break;
                case "removeall":
                    ExecuteRemoveAllForAccount(args);
                    break;
                case "maintenance":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "m":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "md":
                    List<LogicData> logicDatas = DataTables.Get(DataType.Map).GetDatas();
                    LogicData logicData = logicDatas[0];
                    string s = logicData.GetCSVRow().GetValueAt(1);
                    int o = logicData.GetCSVRow().GetArraySizeAt(1);
                    string[] m = MapLoader.InitWithMapFromDataTable(null, DataTables.Get(19), "Tutorial");
                    break;
                case "login":
                    if (OwnAccountId == -1) break;
                    long id = LogicLongCodeGenerator.ToId(args[1]);
                    Account account = Accounts.Load(id);
                    if (account == null)
                    {
                        Console.WriteLine("Fail: account not found!");
                        return;
                    }
                    if (LogicServerListener.Instance.IsPlayerOnline(OwnAccountId))
                    {
                        LogicServerListener.Instance.GetGameListener(OwnAccountId).SendTCPMessage(new UnlockAccountOkMessage()
                        {
                            AccountId = account.AccountId,
                            PassToken = account.PassToken
                        });
                    }
                    break;
                    // Добавьте эти команды в switch блок:
case "giveskin":
    ExecuteGiveSkin(args);
    break;
    // Добавьте в switch блок HandleCmd:
    case "giveskinsalltest":
    ExecuteGiveAlllSkins(args);
    break;
case "givecredits":
    ExecuteGiveCredits(args);
    break;
case "givetitle":
    ExecuteGiveTitle(args);
    break;
case "giveicon":
    ExecuteGiveIcon(args);
    break;
case "givepin":
    ExecuteGivePin(args);
    break;
case "giveemote":
    ExecuteGiveEmote(args);
    break;
    case "giveblings":
    ExecuteGiveBlings(args);
    break;
case "unlockallcosmetics":
    ExecuteUnlockAllCosmetics(args);
    break;
case "maxaccount":
    ExecuteMaxAccount(args);
    break;
  
case "resettokens":
    ExecuteResetTokens(args);
    break;
                case "changetheme":
                    if (OwnAccountId == -1) break;
                    Account account1 = Accounts.Load(OwnAccountId);
                    int i = -1;
                    try
                    {
                        i = int.Parse(args[1]);
                    }
                    catch(Exception) { }
                    account1.Home.PreferredThemeId = i;
                    break;
                case "findmasterycheaters":
                    FindMasteryCheaters();
                    break;
            }
        }

        // Снаряжение, доступное только отдельным бойцам (как в LogicPuschaseGearCommand)
        private static readonly Dictionary<int, List<int>> MaxAccountGearBrawlers = new Dictionary<int, List<int>>
        {
            { 5, new List<int> { 46, 56, 53, 14, 3, 1, 27, 50, 40, 4 } },
            { 6, new List<int> { 51, 41, 59, 2, 36, 58, 43, 37, 10, 34 } },
            { 7, new List<int> { 22 } }, { 8, new List<int> { 21 } }, { 9, new List<int> { 12 } },
            { 10, new List<int> { 5 } }, { 11, new List<int> { 23 } }, { 12, new List<int> { 28 } },
            { 13, new List<int> { 40 } }, { 14, new List<int> { 7, 8, 31, 17, 19 } },
            { 15, new List<int> { 56 } }, { 16, new List<int> { 16 } }, { 18, new List<int> { 11 } }
        };

        // /maxaccount [TAG] - все бойцы 11 уровня со всеми пассивками, гаджетами, гиперзарядами
        // и снаряжением, все скины, аватарки, пины и титулы
        private static void ExecuteMaxAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /maxaccount [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            ClientAvatar avatar = account.Avatar;
            avatar.Refresh(); // все бойцы

            if (avatar.SPGS == null) avatar.SPGS = new List<int>();
            if (avatar.SelectedSPGS == null) avatar.SelectedSPGS = new List<int>();

            var cards = DataTables.Get(DataType.Card).GetDatas().Cast<CardData>().ToList();
            int spg = 0, gears = 0;

            foreach (Hero hero in avatar.Heroes.ToArray())
            {
                hero.PowerLevel = 11;

                string heroName = hero.CharacterData?.Name;
                if (heroName != null)
                {
                    foreach (CardData card in cards)
                    {
                        // 4 - пассивки, 5 - гаджеты, 6 - гиперзаряды
                        if (card.Target != heroName || card.MetaType < 4 || card.MetaType > 6) continue;
                        if (card.LockedForChronos || card.DirectPurchasePrice == 0) continue;
                        if (!avatar.SPGS.Contains(card.GetGlobalId()))
                        {
                            avatar.SPGS.Add(card.GetGlobalId());
                            spg++;
                        }
                    }
                }

                int brawlerId = GlobalId.GetInstanceId(hero.CharacterId);
                if (brawlerId == 33 || brawlerId == 55 || brawlerId > 77) continue;
                if (hero.UnlockedGearInstanceIds == null) hero.UnlockedGearInstanceIds = new List<int>();
                for (int gear = 0; gear <= 18; gear++)
                {
                    if (MaxAccountGearBrawlers.TryGetValue(gear, out List<int> allowed) && !allowed.Contains(brawlerId)) continue;
                    if (!hero.UnlockedGearInstanceIds.Contains(gear))
                    {
                        hero.UnlockedGearInstanceIds.Add(gear);
                        gears++;
                    }
                }
            }
            avatar.MarkFreeGearAsUsed();

            ClientHome home = account.Home;
            if (home.UnlockedSkins == null) home.UnlockedSkins = new List<int>();
            if (home.UnlockedThumbnails == null) home.UnlockedThumbnails = new List<int>();
            if (home.UnlockedEmotes == null) home.UnlockedEmotes = new List<int>();

            int skins = 0, icons = 0, pins = 0;
            foreach (SkinData skin in DataTables.Get(DataType.Skin).GetDatas().Cast<SkinData>())
            {
                if (skin.Disabled || home.UnlockedSkins.Contains(skin.GetGlobalId())) continue;
                home.UnlockedSkins.Add(skin.GetGlobalId());
                skins++;
            }
            foreach (LogicData icon in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            {
                if (home.UnlockedThumbnails.Contains(icon.GetGlobalId())) continue;
                home.UnlockedThumbnails.Add(icon.GetGlobalId());
                icons++;
            }
            foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas().Cast<EmoteData>())
            {
                if (emote.Disabled || home.UnlockedEmotes.Contains(emote.GetGlobalId())) continue;
                home.UnlockedEmotes.Add(emote.GetGlobalId());
                pins++;
            }

            if (home.UnlockedTituls == null) home.UnlockedTituls = new List<int>();
            int titles = 0;
            foreach (LogicData title in DataTables.Get(DataType.Titul).GetDatas())
            {
                if (home.UnlockedTituls.Contains(title.GetGlobalId())) continue;
                home.UnlockedTituls.Add(title.GetGlobalId());
                titles++;
            }
            Console.WriteLine($"[maxaccount] {args[1]}: titles +{titles}");

            Accounts.Save(account);
            Console.WriteLine($"[maxaccount] {args[1]}: бойцов {avatar.Heroes.Count} (11 лвл), пассивок/гаджетов/гиперзарядов +{spg}, снаряжения +{gears}, скинов +{skins}, аватарок +{icons}, пинов +{pins}");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteTempBanAccount(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: /tempban [TAG] [Days] [Reason...]");
                Console.WriteLine("Example: /tempban #9JG2P2P 7 Оскорбления в чате");
                Console.WriteLine("Example: /tempban #9JG2P2P 1 Нарушение правил");
                Console.WriteLine("Бан на указанное количество дней без сброса трофеев");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            if (!int.TryParse(args[2], out int days) || days <= 0)
            {
                Console.WriteLine("Fail: Invalid number of days! Use positive number (1, 7, 30, etc).");
                return;
            }

            string reason = string.Join(" ", args, 3, args.Length - 3);
            if (string.IsNullOrEmpty(reason))
                reason = "Нарушение правил";

            account.Avatar.BanID = 3;
            account.Avatar.BanEndTime = DateTime.UtcNow.AddDays(days);
            account.Avatar.TextReason = $"Ваш аккаунт заблокирован на {days} дней за {reason}.";

            Accounts.Save(account);

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = account.Avatar.TextReason
                });
                Sessions.Remove(id);
            }

            Console.WriteLine($"Success: Account {args[1]} ({id}) has been banned for {days} days. Reason: {reason}");
            Console.WriteLine("Трофеи НЕ сброшены!");
        }

        private static void FindMasteryCheaters()
        {
            Console.WriteLine("Searching for mastery cheaters...");
            
            int cheaterCount = 0;
            var cheaters = new List<string>();
            
            foreach (var accountId in AccountCache.GetAllCachedAccountIds())
            {
                try
                {
                    Account account = AccountCache.GetAccount(accountId);
                    if (account?.Avatar?.Heroes == null) continue;
                    
                    bool isCheater = false;
                    
                    foreach (var hero in account.Avatar.Heroes)
                    {
                        if (hero == null) continue;
                        
                        if (hero.ClaimedMasteryLVL > 9)
                        {
                            isCheater = true;
                            break;
                        }
                        
                        if (hero.MasteryPoints > 24800)
                        {
                            isCheater = true;
                            break;
                        }
                        
                        if (hero.ClaimedMasteryLVL > 0)
                        {
                            var requiredPoints = new Dictionary<int, int>
                            {
                                { 1, 300 },
                                { 2, 800 },
                                { 3, 1500 },
                                { 4, 2600 },
                                { 5, 4000 },
                                { 6, 5800 },
                                { 7, 10300 },
                                { 8, 16800 },
                                { 9, 24800 }
                            };
                            
                            if (requiredPoints.ContainsKey(hero.ClaimedMasteryLVL))
                            {
                                int minRequired = requiredPoints[hero.ClaimedMasteryLVL];
                                if (hero.MasteryPoints < minRequired)
                                {
                                    isCheater = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (isCheater)
                    {
                        string tag = LogicLongCodeGenerator.ToCode(account.AccountId);
                        cheaters.Add($"{tag} ({account.AccountId}) - {account.Avatar.Name}");
                        cheaterCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking account: {ex.Message}");
                }
            }
            
            Console.WriteLine($"Found {cheaterCount} mastery cheaters:");
            foreach (var cheater in cheaters)
            {
                Console.WriteLine($"  {cheater}");
            }
            
            Console.WriteLine("Search completed.");
        }
        
        
        private static void ExecuteResetTokens(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resettokens [TAG]");
        Console.WriteLine("Сбрасывает все токены в 0:");
        Console.WriteLine("• Tokens → 0");
        Console.WriteLine("• StarTokens → 0");
        Console.WriteLine("• RareTokens → 0");
        return;
    }

    long targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Запоминаем старые значения
    int oldTokens = account.Avatar.Tokens;
    int oldStarTokens = account.Avatar.StarTokens;
    int oldRareTokens = account.Avatar.RareTokens;
    
    // Сбрасываем в 0
    account.Avatar.Tokens = 0;
    account.Avatar.StarTokens = 0;
    account.Avatar.RareTokens = 0;
    
    Console.WriteLine("🎯 Сброс всех токенов в 0:");
    Console.WriteLine($"  • Tokens: {oldTokens} → 0");
    Console.WriteLine($"  • StarTokens: {oldStarTokens} → 0");
    Console.WriteLine($"  • RareTokens: {oldRareTokens} → 0");
    
    // Уведомление
    var notification = new Notification
    {
        Id = 72,
        MessageEntry = "Все ваши токены были сброшены администратором!",
        DonationCount = 0
    };

    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();
        
    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);
    
    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "Все ваши токены сброшены!"
        });
    }
    
    Console.WriteLine($"✅ Все токены сброшены у игрока {args[1]}");
}
        
        private static void ExecuteDebugBP(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /debugbp [TAG]");
                return;
            }
            
            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }
            
            Console.WriteLine("=== DEBUG: ClientHome Structure ===");
            
            Type homeType = account.Home.GetType();
            
            Console.WriteLine("\n🔍 Поиск полей Battle Pass:");
            foreach (var field in homeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string fieldName = field.Name.ToLower();
                if (fieldName.Contains("battle") || fieldName.Contains("pass") || 
                    fieldName.Contains("season") || fieldName.Contains("level") ||
                    fieldName.Contains("reward"))
                {
                    try
                    {
                        object value = field.GetValue(account.Home);
                        Console.WriteLine($"  {field.Name} = {value}");
                    }
                    catch
                    {
                        Console.WriteLine($"  {field.Name} = [не удалось получить значение]");
                    }
                }
            }
        }
        
        
        private static void ExecuteGiveBlings(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /giveblings [TAG] [Amount] [Message...]");
        Console.WriteLine("Пример: /giveblings #9JG2P2P 1000 Блинги на улучшения!");
        Console.WriteLine("Пример: /giveblings #9JG2P2P 500 Бонус за активность");
        Console.WriteLine("\n💡 Блинги используются для:");
        Console.WriteLine("• Улучшения гаджетов и звёздных сил");
        Console.WriteLine("• Покупки релоков");
        Console.WriteLine("• Создания и улучшения шестерёнок");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int blingsAmount) || blingsAmount <= 0)
    {
        Console.WriteLine("Ошибка: Неверное количество блингов. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили {blingsAmount} блингов!";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    Console.WriteLine($"🎯 Выдача {blingsAmount} блингов игроку {args[1]}");
    
    // Добавляем блинги
    int oldBlings = account.Avatar.Blings;
    account.Avatar.Blings += blingsAmount;
    int newBlings = account.Avatar.Blings;
    
    Console.WriteLine($"💰 Блинги: {oldBlings} → {newBlings}");

    // Создаём уведомление
    var notification = new Notification
    {
        Id = 64, // CoinRewardNotification (можно использовать для блингов)
        MessageEntry = notificationMessage,
        DonationCount = blingsAmount
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    
    // Сохраняем аккаунт
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        
        // Отправляем уведомление
        session.GameListener.SendTCPMessage(serverCommand);
        
        // Общее сообщение
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили {blingsAmount} блингов!"
        });
        
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"\n✅ Успех: {blingsAmount} блингов выдано игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
    Console.WriteLine($"💰 Текущий баланс блингов: {newBlings}");
}

        private static void ExecuteBanCheater(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: /cheater [TAG] [Reason...]");
                Console.WriteLine("Example: /cheater #9JG2P2P Using hack client");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            string reason = string.Join(" ", args, 2, args.Length - 2);
            if (string.IsNullOrEmpty(reason))
                reason = "использование модифицированной версии игры";

            account.Avatar.BanID = 3;
            account.Avatar.BanEndTime = DateTime.MaxValue;
            account.Avatar.TextReason = $"Ваш аккаунт заблокирован за {reason}.";

            Accounts.Save(account);

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = account.Avatar.TextReason
                });
                Sessions.Remove(id);
            }

            Console.WriteLine($"Success: Cheater {args[1]} ({id}) has been banned for: {reason}");
        }






private static void ExecuteGiveCredits(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /givecredits [TAG] [Amount] [Message...]");
        Console.WriteLine("Пример: /givecredits #9JG2P2P 1000 Кредиты на открытие бойцов!");
        Console.WriteLine("Пример: /givecredits #9JG2P2P 5000 Бонус за активность");
        Console.WriteLine("\n💡 ResourceID для кредитов: 38");
        Console.WriteLine("💡 Кредиты используются для получения новых бойцов");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int creditsAmount) || creditsAmount <= 0)
    {
        Console.WriteLine("Ошибка: Неверное количество кредитов. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили {creditsAmount} кредитов для получения бойцов!";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    Console.WriteLine($"🎯 Выдача {creditsAmount} кредитов (ResourceID: 38) игроку {args[1]}");
    
    // Сначала пытаемся найти поле для кредитов в ClientAvatar
    Type avatarType = account.Avatar.GetType();
    
    // Возможные имена поля для кредитов с ID 38
    string[] possibleCreditFieldNames = { 
        "Credits", "BrawlerCredits", "HeroCredits", "UnlockCredits",
        "BoxCredits", "ChestCredits", "KeyCredits", "Currency38",
        "Resource38", "CreditCurrency", "BrawlerCurrency"
    };
    
    FieldInfo creditField = null;
    string foundFieldName = "";
    int oldCredits = 0;
    
    foreach (string fieldName in possibleCreditFieldNames)
    {
        creditField = avatarType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (creditField != null && (creditField.FieldType == typeof(int) || creditField.FieldType == typeof(long)))
        {
            try
            {
                oldCredits = Convert.ToInt32(creditField.GetValue(account.Avatar));
                foundFieldName = fieldName;
                Console.WriteLine($"✅ Найдено поле для кредитов: '{fieldName}' = {oldCredits}");
                break;
            }
            catch
            {
                creditField = null;
            }
        }
    }
    
    // Если не нашли в ClientAvatar, пробуем Home
    if (creditField == null && account.Home != null)
    {
        Type homeType = account.Home.GetType();
        foreach (string fieldName in possibleCreditFieldNames)
        {
            creditField = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (creditField != null && (creditField.FieldType == typeof(int) || creditField.FieldType == typeof(long)))
            {
                try
                {
                    oldCredits = Convert.ToInt32(creditField.GetValue(account.Home));
                    foundFieldName = fieldName;
                    Console.WriteLine($"✅ Найдено поле для кредитов в Home: '{fieldName}' = {oldCredits}");
                    break;
                }
                catch
                {
                    creditField = null;
                }
            }
        }
    }
    
    // Если нашли поле - обновляем значение
    if (creditField != null)
    {
        int newCredits = oldCredits + creditsAmount;
        
        if (foundFieldName.StartsWith("Home"))
            creditField.SetValue(account.Home, newCredits);
        else
            creditField.SetValue(account.Avatar, newCredits);
        
        Console.WriteLine($"💰 {foundFieldName}: {oldCredits} → {newCredits}");
    }
    else
    {
        Console.WriteLine("⚠️ Поле для кредитов не найдено. Выдаю только уведомление.");
        Console.WriteLine("\n💡 Используйте команду для поиска:");
        Console.WriteLine($"  /findcurrency {args[1]}");
    }

    // Создаём уведомление с правильным ResourceID
    var notification = new Notification
    {
        Id = 90, // ResourceRewardNotification
        MessageEntry = notificationMessage,
        ResourceID = 38, // ID кредитов
        ResourceCount = creditsAmount
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    
    // Сохраняем аккаунт
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        
        // Отправляем уведомление
        session.GameListener.SendTCPMessage(serverCommand);
        
        // Общее сообщение
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили {creditsAmount} кредитов для получения бойцов!"
        });
        
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"\n✅ Успех: {creditsAmount} кредитов (ID: 38) выдано игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
    
    if (creditField != null)
    {
        int currentValue = foundFieldName.StartsWith("Home") ? 
            Convert.ToInt32(creditField.GetValue(account.Home)) : 
            Convert.ToInt32(creditField.GetValue(account.Avatar));
        Console.WriteLine($"💰 Текущий баланс кредитов: {currentValue}");
    }
}

private static void ExecuteRemoveCredits(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /removecredits [TAG] [Amount] [Message...]");
        Console.WriteLine("Пример: /removecredits #9JG2P2P 500 Снятие кредитов");
        Console.WriteLine("\n💡 ResourceID для кредитов: 38");
        return;
    }

    long targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    if (!int.TryParse(args[2], out int amount) || amount <= 0)
    {
        Console.WriteLine("Ошибка: Неверное количество.");
        return;
    }

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    Console.WriteLine($"🎯 Изъятие {amount} кредитов у игрока {args[1]}");
    
    // Ищем поле для кредитов
    Type avatarType = account.Avatar.GetType();
    string[] possibleFields = { "Credits", "BrawlerCredits", "HeroCredits", "UnlockCredits" };
    
    FieldInfo creditField = null;
    string foundFieldName = "";
    int oldCredits = 0;
    
    foreach (string fieldName in possibleFields)
    {
        creditField = avatarType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (creditField != null && (creditField.FieldType == typeof(int) || creditField.FieldType == typeof(long)))
        {
            try
            {
                oldCredits = Convert.ToInt32(creditField.GetValue(account.Avatar));
                foundFieldName = fieldName;
                break;
            }
            catch
            {
                creditField = null;
            }
        }
    }
    
    // Если не нашли, пробуем Home
    if (creditField == null && account.Home != null)
    {
        Type homeType = account.Home.GetType();
        foreach (string fieldName in possibleFields)
        {
            creditField = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (creditField != null)
            {
                try
                {
                    oldCredits = Convert.ToInt32(creditField.GetValue(account.Home));
                    foundFieldName = "Home." + fieldName;
                    break;
                }
                catch
                {
                    creditField = null;
                }
            }
        }
    }
    
    if (creditField != null)
    {
        int newCredits = Math.Max(0, oldCredits - amount);
        
        if (foundFieldName.StartsWith("Home"))
            creditField.SetValue(account.Home, newCredits);
        else
            creditField.SetValue(account.Avatar, newCredits);
        
        Console.WriteLine($"💰 {foundFieldName}: {oldCredits} → {newCredits} (изъято: {amount})");
    }
    else
    {
        Console.WriteLine("⚠️ Поле для кредитов не найдено.");
    }
    
    // Уведомление о снятии
    string message = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"У вас изъято {amount} кредитов!";
    
    var notification = new Notification
    {
        Id = 90,
        MessageEntry = message,
        ResourceID = 38,
        ResourceCount = -amount
    };

    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();
        
    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);
    
    Console.WriteLine($"✅ {amount} кредитов изъято у игрока {args[1]}");
}

        private static void ExecuteUnlockAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Refresh();

            AccountCache.SaveAll();
            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "6"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteRemoveAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /removeall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            for (int i = 1; ; i++)
            {
                if (account.Avatar.HasHero(16000000 + i))
                {
                    CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                    if (!character.IsHero()) return;

                    account.Avatar.RemoveHero(character.GetGlobalId());
                }
            }

            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }
        
        private static void ExecuteGiveSkin(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /giveskin [TAG] [SkinID] [Message...]");
        Console.WriteLine("Пример: /giveskin #9JG2P2P 1 Вы получили крутой скин!");
        Console.WriteLine("Пример: /giveskin #9JG2P2P 3 Скин Шелли");
        Console.WriteLine("\nSkinID - это индекс скина, а не глобальный ID");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int skinIndex) || skinIndex <= 0)
    {
        Console.WriteLine("Ошибка: Неверный SkinID. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили новый скин! (ID: {skinIndex})";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Проверяем ClientHome
    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    // Проверяем UnlockedSkins
    if (account.Home.UnlockedSkins == null)
    {
        account.Home.UnlockedSkins = new List<int>();
    }

    // Добавляем скин если его нет
    // Предполагаем, что ID скина в формате 28000000 + skinIndex
    int skinGlobalId = 28000000 + skinIndex;
    if (!account.Home.UnlockedSkins.Contains(skinGlobalId))
    {
        account.Home.UnlockedSkins.Add(skinGlobalId);
        Console.WriteLine($"✅ Скин {skinIndex} (глобальный ID: {skinGlobalId}) добавлен в UnlockedSkins");
    }
    else
    {
        Console.WriteLine($"ℹ️ Скин {skinIndex} уже был разблокирован");
    }

    // Создаем уведомление
    var notification = new Notification
    {
        Id = 94, // SkinRewardNotification
        MessageEntry = notificationMessage,
        SkinID = skinIndex
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили новый скин! (ID: {skinIndex})"
        });
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"✅ Успех: Скин {skinIndex} выдан игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
}

private static void ExecuteGiveTitle(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /givetitle [TAG] [TitleID] [Message...]");
        Console.WriteLine("Пример: /givetitle #9JG2P2P 141 VIP титул!");
        Console.WriteLine("Пример: /givetitle #9JG2P2P 92 Герой!");
        Console.WriteLine("\nПопулярные TitleID:");
        Console.WriteLine("  92 - Герой");
        Console.WriteLine("  141 - VIP");
        Console.WriteLine("  141 - Легенда");
        Console.WriteLine("  112 - Рыцарь");
        Console.WriteLine("  115 - Эгоист");
        Console.WriteLine("  137 - Золотой");
        Console.WriteLine("  178 - Самый главный Индус");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int titleId) || titleId <= 0)
    {
        Console.WriteLine("Ошибка: Неверный TitleID. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили новый титул! (ID: {titleId})";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Проверяем ClientHome
    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    // Создаем глобальный ID для титула (тип 76)
    int titleGlobalId = GlobalId.CreateGlobalId(76, titleId);
    
    // Проверяем UnlockedTituls
    if (account.Home.UnlockedTituls == null)
    {
        account.Home.UnlockedTituls = new List<int>();
    }

    // Добавляем титул если его нет
    if (!account.Home.UnlockedTituls.Contains(titleGlobalId))
    {
        account.Home.UnlockedTituls.Add(titleGlobalId);
        Console.WriteLine($"✅ Титул {titleId} (глобальный ID: {titleGlobalId}) добавлен в UnlockedTituls");
    }
    else
    {
        Console.WriteLine($"ℹ️ Титул {titleId} уже был разблокирован");
    }

    // Для титулов используем ResourceRewardNotification (ID 90) с ResourceID = titleId
    var notification = new Notification
    {
        Id = 90, // ResourceRewardNotification
        MessageEntry = notificationMessage,
        ResourceID = titleId, // ID титула
        ResourceCount = 1 // Количество
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили новый титул! (ID: {titleId})"
        });
        Console.WriteLine("?? Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"✅ Успех: Титул {titleId} выдан игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
}

private static void ExecuteGiveIcon(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /giveicon [TAG] [IconID] [Message...]");
        Console.WriteLine("Пример: /giveicon #9JG2P2P 0 Базовая иконка");
        Console.WriteLine("Пример: /giveicon #9JG2P2P 1 Редкая иконка");
        Console.WriteLine("\nIconID - ID иконки профиля из CSV");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int iconId) || iconId < 0)
    {
        Console.WriteLine("Ошибка: Неверный IconID. Укажите неотрицательное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили новую иконку профиля! (ID: {iconId})";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Проверяем ClientHome
    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    // Создаем глобальный ID для иконки (тип 28)
    int iconGlobalId = GlobalId.CreateGlobalId(28, iconId);
    
    // Проверяем UnlockedThumbnails
    if (account.Home.UnlockedThumbnails == null)
    {
        account.Home.UnlockedThumbnails = new List<int>();
    }

    // Добавляем иконку если ее нет
    if (!account.Home.UnlockedThumbnails.Contains(iconGlobalId))
    {
        account.Home.UnlockedThumbnails.Add(iconGlobalId);
        Console.WriteLine($"✅ Иконка {iconId} (глобальный ID: {iconGlobalId}) добавлена в UnlockedThumbnails");
    }
    else
    {
        Console.WriteLine($"ℹ️ Иконка {iconId} уже была разблокирована");
    }

    // Для иконок также используем ResourceRewardNotification
    var notification = new Notification
    {
        Id = 90, // ResourceRewardNotification
        MessageEntry = notificationMessage,
        ResourceID = iconId, // ID иконки
        ResourceCount = 1 // Количество
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили новую иконку профиля! (ID: {iconId})"
        });
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"✅ Успех: Иконка {iconId} выдана игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
}

private static void ExecuteGivePin(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /givepin [TAG] [PinID] [Message...]");
        Console.WriteLine("Пример: /givepin #9JG2P2P 59000001 Смайлик!");
        Console.WriteLine("Пример: /givepin #9JG2P2P 59000005 Легендарный пин!");
        Console.WriteLine("\nPinID - глобальный ID пина (тип 59)");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int pinGlobalId) || pinGlobalId <= 0)
    {
        Console.WriteLine("Ошибка: Неверный PinID. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили новый пин! (ID: {pinGlobalId})";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Проверяем ClientHome - возможно пины хранятся в отдельной структуре
    Console.WriteLine("⚠️ Внимание: Структура для пинов не найдена в ClientAvatar");
    Console.WriteLine("Пин будет добавлен только в уведомление, но не в коллекцию");

    // Для пинов используем ResourceRewardNotification
    // Извлекаем индекс пина из глобального ID (59000001 -> 1)
    int pinIndex = pinGlobalId - 59000000;
    
    var notification = new Notification
    {
        Id = 90, // ResourceRewardNotification
        MessageEntry = notificationMessage,
        ResourceID = pinIndex, // Индекс пина
        ResourceCount = 1
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили новый пин! (ID: {pinGlobalId})"
        });
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"✅ Уведомление о пине отправлено игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
    Console.WriteLine("⚠️ Примечание: Пин не добавлен в коллекцию (структура не найдена)");
}

private static void ExecuteGiveEmote(string[] args)
{
    if (args.Length < 3)
    {
        Console.WriteLine("Usage: /giveemote [TAG] [EmoteID] [Message...]");
        Console.WriteLine("Пример: /giveemote #9JG2P2P 52000001 Эмоция!");
        Console.WriteLine("\nEmoteID - глобальный ID эмоции (тип 52)");
        return;
    }
    
    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    if (!int.TryParse(args[2], out int emoteGlobalId) || emoteGlobalId <= 0)
    {
        Console.WriteLine("Ошибка: Неверный EmoteID. Укажите положительное число.");
        return;
    }

    string notificationMessage = args.Length > 3 ? 
        string.Join(" ", args, 3, args.Length - 3) : 
        $"Вы получили новую эмоцию! (ID: {emoteGlobalId})";

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // Проверяем ClientHome
    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    // Проверяем UnlockedEmotes
    if (account.Home.UnlockedEmotes == null)
    {
        account.Home.UnlockedEmotes = new List<int>();
    }

    // Добавляем эмоцию если ее нет
    if (!account.Home.UnlockedEmotes.Contains(emoteGlobalId))
    {
        account.Home.UnlockedEmotes.Add(emoteGlobalId);
        Console.WriteLine($"✅ Эмоция {emoteGlobalId} добавлена в UnlockedEmotes");
    }
    else
    {
        Console.WriteLine($"ℹ️ Эмоция {emoteGlobalId} уже была разблокирована");
    }

    // Для эмоций используем ResourceRewardNotification
    // Извлекаем индекс эмоции из глобального ID (52000001 -> 1)
    int emoteIndex = emoteGlobalId - 52000000;
    
    var notification = new Notification
    {
        Id = 90, // ResourceRewardNotification
        MessageEntry = notificationMessage,
        ResourceID = emoteIndex, // Индекс эмоции
        ResourceCount = 1
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = $"Вы получили новую эмоцию! (ID: {emoteGlobalId})"
        });
        Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
    }

    Console.WriteLine($"✅ Успех: Эмоция {emoteGlobalId} выдана игроку {args[1]}");
    Console.WriteLine($"💬 Сообщение: \"{notificationMessage}\"");
}


private static void ExecuteUnlockAllCosmetics(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /unlockallcosmetics [TAG]");
        Console.WriteLine("Пример: /unlockallcosmetics #9JG2P2P");
        Console.WriteLine("Разблокирует все косметические предметы у игрока");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    Console.WriteLine($"🎯 Начинаю разблокировку всей косметики для {args[1]}...");

    // 1. Разблокируем все скины (примерный диапазон)
    Console.WriteLine("🎭 Разблокирую скины...");
    if (account.Home.UnlockedSkins == null)
        account.Home.UnlockedSkins = new List<int>();
    
    int skinsAdded = 0;
    for (int i = 1; i <= 100; i++) // Предположим 100 скинов
    {
        int skinId = 28000000 + i;
        if (!account.Home.UnlockedSkins.Contains(skinId))
        {
            account.Home.UnlockedSkins.Add(skinId);
            skinsAdded++;
        }
    }
    Console.WriteLine($"✅ Добавлено {skinsAdded} новых скинов");

    // 2. Разблокируем все титулы
    Console.WriteLine("👑 Разблокирую титулы...");
    if (account.Home.UnlockedTituls == null)
        account.Home.UnlockedTituls = new List<int>();
    
    int titlesAdded = 0;
    for (int i = 1; i <= 200; i++) // Предположим 200 титулов
    {
        int titleGlobalId = GlobalId.CreateGlobalId(76, i);
        if (!account.Home.UnlockedTituls.Contains(titleGlobalId))
        {
            account.Home.UnlockedTituls.Add(titleGlobalId);
            titlesAdded++;
        }
    }
    Console.WriteLine($"✅ Добавлено {titlesAdded} новых титулов");

    // 3. Разблокируем все иконки профиля
    Console.WriteLine("🖼️ Разблокирую иконки профиля...");
    if (account.Home.UnlockedThumbnails == null)
        account.Home.UnlockedThumbnails = new List<int>();
    
    int iconsAdded = 0;
    for (int i = 0; i <= 50; i++) // Предположим 50 иконок
    {
        int iconGlobalId = GlobalId.CreateGlobalId(28, i);
        if (!account.Home.UnlockedThumbnails.Contains(iconGlobalId))
        {
            account.Home.UnlockedThumbnails.Add(iconGlobalId);
            iconsAdded++;
        }
    }
    Console.WriteLine($"✅ Добавлено {iconsAdded} новых иконок");

    // 4. Разблокируем все эмоции
    Console.WriteLine("😀 Разблокирую эмоции...");
    if (account.Home.UnlockedEmotes == null)
        account.Home.UnlockedEmotes = new List<int>();
    
    int emotesAdded = 0;
    for (int i = 1; i <= 50; i++) // Предположим 50 эмоций
    {
        int emoteId = 52000000 + i;
        if (!account.Home.UnlockedEmotes.Contains(emoteId))
        {
            account.Home.UnlockedEmotes.Add(emoteId);
            emotesAdded++;
        }
    }
    Console.WriteLine($"✅ Добавлено {emotesAdded} новых эмоций");

    // Создаем уведомление
    var notification = new Notification
    {
        Id = 89, // GemRewardNotification для общего уведомления
        MessageEntry = "Вы получили ВСЮ КОСМЕТИКУ в игре!",
        DonationCount = 0 // Можно установить 0 или количество полученных предметов
    };

    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    Accounts.Save(account);

    // Отправляем команду клиенту
    LogicAddNotificationRealCommand addNotificationCommand = new()
    {
        Notification = notification
    };
    
    AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
    serverCommand.Command = addNotificationCommand;

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        var session = Sessions.GetSession(targetAccountId);
        session.GameListener.SendTCPMessage(serverCommand);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "Вы получили всю косметику в игре!"
        });
        Sessions.Remove(targetAccountId);
        Console.WriteLine("👤 Игрок отключен для применения изменений");
    }

    Console.WriteLine($"\n🎉 УСПЕХ: Вся косметика разблокирована для {args[1]}!");
    Console.WriteLine("📊 ИТОГ:");
    Console.WriteLine($"  • Скины: {skinsAdded} новых");
    Console.WriteLine($"  • Титулы: {titlesAdded} новых");
    Console.WriteLine($"  • Иконки: {iconsAdded} новых");
    Console.WriteLine($"  • Эмоции: {emotesAdded} новых");
    Console.WriteLine($"  • Всего: {skinsAdded + titlesAdded + iconsAdded + emotesAdded} предметов");
}

        
        private static void ExecuteResetBattlePass(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resetbp [TAG]");
        Console.WriteLine("Сбрасывает боевой пропуск до 0 уровня");
        return;
    }
    
    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }
    
    // Получаем тип ClientHome
    Type homeType = account.Home.GetType();
    
    Console.WriteLine($"📊 Текущий статус БП у {args[1]}:");
    
    // Получаем поля через Reflection
    FieldInfo brawlPassField = homeType.GetField("BrawlPassProgress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    FieldInfo premiumPassField = homeType.GetField("PremiumPassProgress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    FieldInfo plusPassField = homeType.GetField("BrawlPassPlusProgress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    FieldInfo tokensField = homeType.GetField("BrawlPassTokens", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    FieldInfo battleTokensField = homeType.GetField("BattleTokens", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
    // Показываем текущие значения
    if (brawlPassField != null) Console.WriteLine($"- BrawlPassProgress: {brawlPassField.GetValue(account.Home)}");
    if (premiumPassField != null) Console.WriteLine($"- PremiumPassProgress: {premiumPassField.GetValue(account.Home)}");
    if (plusPassField != null) Console.WriteLine($"- BrawlPassPlusProgress: {plusPassField.GetValue(account.Home)}");
    if (tokensField != null) Console.WriteLine($"- BrawlPassTokens: {tokensField.GetValue(account.Home)}");
    
    FieldInfo hasPremiumField = homeType.GetField("HasPremiumPass", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    FieldInfo hasPlusField = homeType.GetField("HasPremiumPassPlus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
    if (hasPremiumField != null) Console.WriteLine($"- HasPremiumPass: {hasPremiumField.GetValue(account.Home)}");
    if (hasPlusField != null) Console.WriteLine($"- HasPremiumPassPlus: {hasPlusField.GetValue(account.Home)}");
    
    Console.WriteLine("\n⚠️ Вы уверены что хотите сбросить БП? (yes/no)");
    if (Console.ReadLine()?.ToLower() != "yes")
    {
        Console.WriteLine("❌ Отменено.");
        return;
    }
    
    // Сбрасываем БП
    if (brawlPassField != null) brawlPassField.SetValue(account.Home, 0);
    if (premiumPassField != null) premiumPassField.SetValue(account.Home, 0);
    if (plusPassField != null) plusPassField.SetValue(account.Home, 0);
    if (tokensField != null) tokensField.SetValue(account.Home, 0);
    if (battleTokensField != null) battleTokensField.SetValue(account.Home, 0);
    
    // Сохраняем изменения
    Accounts.Save(account);
    
    Console.WriteLine($"✅ БП сброшен для {args[1]}!");
    Console.WriteLine("Теперь все уровни БП = 0, токены = 0");
    Console.WriteLine("Premium Pass и Plus Pass сохранены");
    
    // Показываем новые значения
    Console.WriteLine($"\n📊 Новый статус БП у {args[1]}:");
    if (brawlPassField != null) Console.WriteLine($"- BrawlPassProgress: {brawlPassField.GetValue(account.Home)}");
    if (premiumPassField != null) Console.WriteLine($"- PremiumPassProgress: {premiumPassField.GetValue(account.Home)}");
    if (plusPassField != null) Console.WriteLine($"- BrawlPassPlusProgress: {plusPassField.GetValue(account.Home)}");
    if (tokensField != null) Console.WriteLine($"- BrawlPassTokens: {tokensField.GetValue(account.Home)}");
    
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "Ваш боевой пропуск был сброшен администратором!"
        });
        Sessions.Remove(id);
        Console.WriteLine("👤 Игрок отключен для применения изменений");
    }
}
        
        private static void ExecuteCheckVIPStatus(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /checkvip [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }
            
            string status = account.Avatar.IsPremium ? "ACTIVE" : "INACTIVE";
            string timeLeft = account.Avatar.IsPremium ? 
                $"({(account.Avatar.PremiumTime - DateTime.UtcNow).Days} days left)" : "";
            
            Console.WriteLine($"VIP status for {args[1]}: {status} {timeLeft}");
        }

        private static void ExecuteGivePremiumToAccount(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /vip [TAG]");
        Console.WriteLine("Выдаёт VIP статус на 30 дней со следующими бонусами:");
        Console.WriteLine("• +120 гемов");
        Console.WriteLine("• VIP титул (141)");
        Console.WriteLine("• Дополнительные +3 кубка за победу (VIP TROPHY BOOST)");
        Console.WriteLine("Пример: /vip #9JG2P2P");
        return;
    }

    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    
    if (account == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }

    // VIP статус (x2 кубки + еженедельные гемы)
    account.Avatar.ActivateVip(30);
    
    // 3. Добавляем гемы
    int bonusGems = 120;
    account.Avatar.Gems += bonusGems;
    
    // 4. VIP титул (141)
    int vipTitleGlobalId = GlobalId.CreateGlobalId(76, 141);
    if (account.Home.UnlockedTituls == null)
        account.Home.UnlockedTituls = new List<int>();
    
    if (!account.Home.UnlockedTituls.Contains(vipTitleGlobalId))
        account.Home.UnlockedTituls.Add(vipTitleGlobalId);

    // 5. Создаём два уведомления
    var gemication = new Notification
    {
        Id = 89, // GemRewardNotification
        MessageEntry = $"🎉 VIP статус активирован на 30 дней!\n+{bonusGems} гемов в подарок!",
        DonationCount = bonusGems
    };

    var trophyNotification = new Notification
    {
        Id = 88, // TrophyRewardNotification (или любой другой подходящий ID)
        MessageEntry = "🏆 VIP BONUS: Теперь вы получаете дополнительных +3 кубка за победу!\nЭтот бонус действует 30 дней.",
        DonationCount = 0
    };

    // Добавляем уведомления
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(gemication);
    account.Home.NotificationFactory.Add(trophyNotification);
    Accounts.Save(account);

    // Отправляем уведомления онлайн игроку
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        
        // Отправляем уведомление о гемах
        LogicAddNotificationRealCommand gemCommand = new()
        {
            Notification = gemication
        };
        AvailableServerCommandMessage gemMessage = new AvailableServerCommandMessage();
        gemMessage.Command = gemCommand;
        session.GameListener.SendTCPMessage(gemMessage);
        
        // Отправляем уведомление о кубках
        LogicAddNotificationRealCommand trophyCommand = new()
        {
            Notification = trophyNotification
        };
        AvailableServerCommandMessage trophyMessage = new AvailableServerCommandMessage();
        trophyMessage.Command = trophyCommand;
        session.GameListener.SendTCPMessage(trophyMessage);
        
        // Отправляем общее сообщение
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "🎉 VIP АКТИВИРОВАН! +120 гемов и x2 кубки за победы на 30 дней!"
        });
        
        // Отключаем игрока для применения изменений
        Sessions.Remove(id);
        Console.WriteLine("👤 Игрок отключен для применения VIP изменений");
    }
    
    Console.WriteLine($"✅ VIP выдан {args[1]} на 30 дней!");
    Console.WriteLine($"📊 ВКЛЮЧЕНО:");
    Console.WriteLine($"   • +{bonusGems} гемов");
    Console.WriteLine($"   • VIP титул 141");
    Console.WriteLine($"   • 🏆 Дополнительные +3 кубка за победу (VIP TROPHY BOOST) (30 дней)");
    Console.WriteLine($"   • Окончание: {DateTime.UtcNow.AddDays(30):dd.MM.yyyy HH:mm}");
}

        private static void ExecuteUnbanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = false;
            account.Avatar.BanID = 0;
            account.Avatar.BanEndTime = DateTime.MinValue;
            account.Avatar.TextReason = "";
            
            Accounts.Save(account);

            Console.WriteLine($"✅ Аккаунт {args[1]} ({id}) полностью разблокирован!");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Ваш аккаунт был разблокирован администратором!"
                });
                Sessions.Remove(id);
            }
        }



private static void ExecuteCheckAccount(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /checkaccount [TAG]");
        return;
    }
    
    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    
    if (account == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }
    
    Console.WriteLine($"📊 Account структура для {args[1]}:");
    Console.WriteLine($"AccountId: {account.AccountId}");
    Console.WriteLine($"Avatar: {account.Avatar != null}");
    Console.WriteLine($"Home: {account.Home != null}");
    Console.WriteLine($"Avatar.HomeMode: {account.Avatar?.HomeMode != null}");
    
    // Проверим тип account.Home
    if (account.Home != null)
    {
        Console.WriteLine($"\n🏠 account.Home тип: {account.Home.GetType().Name}");
        
        // Посмотрим поля account.Home
        Type homeType = account.Home.GetType();
        var fields = homeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        Console.WriteLine($"\n🔍 Поля account.Home (первые 20):");
        for (int i = 0; i < Math.Min(20, fields.Length); i++)
        {
            try
            {
                object value = fields[i].GetValue(account.Home);
                Console.WriteLine($"{i:00}. {fields[i].FieldType.Name,-10} {fields[i].Name,-30} = {value}");
            }
            catch
            {
                Console.WriteLine($"{i:00}. {fields[i].FieldType.Name,-10} {fields[i].Name,-30} = [error]");
            }
        }
    }
    
    // Если есть HomeMode, проверим его
    if (account.Avatar?.HomeMode != null)
    {
        Console.WriteLine($"\n🎮 Avatar.HomeMode тип: {account.Avatar.HomeMode.GetType().Name}");
        Console.WriteLine($"HomeMode.Home: {account.Avatar.HomeMode.Home != null}");
    }
}


        private static void ExecuteBanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /ban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Banned = true;
            account.Avatar.ResetTrophies();
            account.Avatar.Name = "Brawler";
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteChangeNameForAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [NewName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Name = args[2];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGetFieldValue(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            int value = (int)field.GetValue(account.Avatar);
            Console.WriteLine($"LogicClientAvatar::{args[2]} = {value}");
        }

        private static void ExecuteChangeValueForAccount(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName] [Value]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            field.SetValue(account.Avatar, int.Parse(args[3]));
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteFindBattlePassFields(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /findbp [TAG]");
                return;
            }
            
            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }
            
            Console.WriteLine("🔍 Поиск полей Battle Pass в ClientHome...");
            
            Type homeType = account.Home.GetType();
            var allFields = homeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            string[] keywords = { "battle", "pass", "season", "level", "point", "reward", "bp", "lvl", "xp", "exp", "tier" };
            
            Console.WriteLine("\n🎯 Найдены подходящие поля:");
            bool found = false;
            
            foreach (var field in allFields)
            {
                string fieldName = field.Name.ToLower();
                
                foreach (var keyword in keywords)
                {
                    if (fieldName.Contains(keyword))
                    {
                        try
                        {
                            object value = field.GetValue(account.Home);
                            Console.WriteLine($"✅ {field.FieldType.Name} {field.Name} = {value}");
                            found = true;
                        }
                        catch
                        {
                            Console.WriteLine($"⚠️ {field.FieldType.Name} {field.Name} = [ошибка чтения]");
                        }
                        break;
                    }
                }
            }
            
            if (!found)
            {
                Console.WriteLine("❌ Не найдено полей с ключевыми словами.");
                Console.WriteLine("\n📋 Все поля ClientHome (первые 20):");
                for (int i = 0; i < Math.Min(20, allFields.Length); i++)
                {
                    Console.WriteLine($"  {allFields[i].FieldType.Name} {allFields[i].Name}");
                }
            }
        }
        
        
        private static void ExecuteResetBPNow(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resetbpnow [TAG]");
        Console.WriteLine("Сбрасывает Battle Pass через прямое изменение account.Home");
        return;
    }
    
    string tag = args[1];
    long id = LogicLongCodeGenerator.ToId(tag);
    Account account = Accounts.Load(id);
    
    if (account == null || account.Home == null)
    {
        Console.WriteLine("❌ Аккаунт или account.Home не найден!");
        return;
    }
    
    Console.WriteLine($"⚡ Немедленный сброс БП для {tag}!");
    Console.WriteLine("Использую прямое изменение полей...\n");
    
    object homeObj = account.Home;
    Type homeType = homeObj.GetType();
    
    // Поля для сброса с учетом их типов
    var fieldsToReset = new Dictionary<string, object>
    {
        { "BrawlPassProgress", (BigInteger)0 },
        { "PremiumPassProgress", (BigInteger)0 },
        { "BrawlPassPlusProgress", (BigInteger)0 },
        { "BrawlPassTokens", 0 },
        { "BattleTokens", 0 }
    };
    
    foreach (var fieldPair in fieldsToReset)
    {
        FieldInfo field = homeType.GetField(fieldPair.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            try
            {
                object oldValue = field.GetValue(homeObj);
                field.SetValue(homeObj, fieldPair.Value);
                object newValue = field.GetValue(homeObj);
                Console.WriteLine($"  ✅ {fieldPair.Key}: {oldValue} → {newValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {fieldPair.Key}: ошибка - {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"  ⚠️ {fieldPair.Key}: поле не найдено");
        }
    }
    
    // Сохраняем
    Accounts.Save(account);
    Console.WriteLine($"\n💾 Изменения сохранены!");
    
    // Кикаем игрока
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "Ваш боевой пропуск был сброшен!"
        });
        Sessions.Remove(id);
        Console.WriteLine("👤 Игрок отключен");
    }
    
    Console.WriteLine($"\n🎯 Проверьте результат: /debugbp {tag}");
}

        private static void ExecuteListHomeFields(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /listhome [TAG]");
                return;
            }
            
            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }
            
            Type homeType = account.Home.GetType();
            var fields = homeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var properties = homeType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            Console.WriteLine($"📊 Структура ClientHome для {args[1]}:");
            Console.WriteLine($"\n🎯 ПОЛЯ ({fields.Length}):");
            
            for (int i = 0; i < fields.Length; i++)
            {
                Console.WriteLine($"{i:000}. {fields[i].FieldType.Name,-15} {fields[i].Name}");
            }
            
            Console.WriteLine($"\n🎯 СВОЙСТВА ({properties.Length}):");
            for (int i = 0; i < properties.Length; i++)
            {
                Console.WriteLine($"{i:000}. {properties[i].PropertyType.Name,-15} {properties[i].Name}");
            }
        }
        
        
        private static void ExecuteResetBPFinal(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resetbpfinal [TAG]");
        Console.WriteLine("ФИНАЛЬНЫЙ сброс Battle Pass с правильными типами данных");
        return;
    }
    
    string tag = args[1];
    long id = LogicLongCodeGenerator.ToId(tag);
    Account account = Accounts.Load(id);
    
    if (account == null || account.Home == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }
    
    Console.WriteLine($"🎯 ФИНАЛЬНЫЙ СБРОС БП ДЛЯ {tag}");
    Console.WriteLine("=================================\n");
    
    object homeObj = account.Home;
    Type homeType = homeObj.GetType();
    
    // Показываем текущие значения
    Console.WriteLine("📊 ТЕКУЩИЕ ЗНАЧЕНИЯ:");
    ShowBPField(homeType, homeObj, "BrawlPassProgress");
    ShowBPField(homeType, homeObj, "PremiumPassProgress");
    ShowBPField(homeType, homeObj, "BrawlPassPlusProgress");
    ShowBPField(homeType, homeObj, "BrawlPassTokens");
    ShowBPField(homeType, homeObj, "BattleTokens");
    ShowBPField(homeType, homeObj, "HasPremiumPass");
    ShowBPField(homeType, homeObj, "HasPremiumPassPlus");
    
    Console.WriteLine("\n⚠️ Вы уверены что хотите сбросить БП? (yes/no)");
    if (Console.ReadLine()?.ToLower() != "yes")
    {
        Console.WriteLine("❌ Отменено.");
        return;
    }
    
    Console.WriteLine("\n🔄 СБРАСЫВАЮ БП...");
    
    // Сбрасываем поля
    ResetBPField(homeType, homeObj, "BrawlPassProgress", (BigInteger)0);
    ResetBPField(homeType, homeObj, "PremiumPassProgress", (BigInteger)0);
    ResetBPField(homeType, homeObj, "BrawlPassPlusProgress", (BigInteger)0);
    ResetBPField(homeType, homeObj, "BrawlPassTokens", 0);
    ResetBPField(homeType, homeObj, "BattleTokens", 0);
    // HasPremiumPass и HasPremiumPassPlus НЕ сбрасываем!
    
    // Сохраняем
    Accounts.Save(account);
    
    Console.WriteLine("\n📊 НОВЫЕ ЗНАЧЕНИЯ:");
    ShowBPField(homeType, homeObj, "BrawlPassProgress");
    ShowBPField(homeType, homeObj, "PremiumPassProgress");
    ShowBPField(homeType, homeObj, "BrawlPassPlusProgress");
    ShowBPField(homeType, homeObj, "BrawlPassTokens");
    
    Console.WriteLine($"\n🎉 БП УСПЕШНО СБРОШЕН ДЛЯ {tag}!");
    
    // Кикаем игрока
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "Ваш боевой пропуск был сброшен администратором!"
        });
        Sessions.Remove(id);
        Console.WriteLine("👤 Игрок отключен для применения изменений");
    }
    
    Console.WriteLine($"\n🔍 Проверьте: /debugbp {tag}");
}

// Вспомогательные методы
private static void ShowBPField(Type homeType, object homeObj, string fieldName)
{
    FieldInfo field = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
    {
        try
        {
            object value = field.GetValue(homeObj);
            Console.WriteLine($"  {fieldName}: {value}");
        }
        catch
        {
            Console.WriteLine($"  {fieldName}: [error]");
        }
    }
}



private static void ExecuteResetAllBP()
{
    Console.WriteLine("💥 ПОЛНЫЙ СБРОС BATTLE PASS У ВСЕХ ИГРОКОВ");
    Console.WriteLine("=============================================");
    Console.WriteLine("⚠️  ВНИМАНИЕ: Эта операция НЕОБРАТИМА!");
    Console.WriteLine("\n📊 Что будет сброшено:");
    Console.WriteLine("• BrawlPassProgress → 0");
    Console.WriteLine("• PremiumPassProgress → 0");
    Console.WriteLine("• BrawlPassPlusProgress → 0");
    Console.WriteLine("• BrawlPassTokens → 0");
    Console.WriteLine("• BattleTokens → 0");
    Console.WriteLine("\n🔒 Что НЕ будет сброшено:");
    Console.WriteLine("• HasPremiumPass (остается как есть)");
    Console.WriteLine("• HasPremiumPassPlus (остается как есть)");
    Console.WriteLine("\n👥 Кого затронет:");
    Console.WriteLine("• Все онлайн игроки (будут отключены)");
    Console.WriteLine("• Все оффлайн игроки");
    Console.WriteLine("\n⏱️  Примерное время: 1-5 минут");
    
    Console.WriteLine("\n❓ ВЫ УВЕРЕНЫ? (yes/no)");
    string confirmation = Console.ReadLine();
    
    if (confirmation?.ToLower() != "yes")
    {
        Console.WriteLine("❌ Операция отменена.");
        return;
    }
    
    Console.WriteLine("\n🔄 Начинаю полный сброс БП у всех игроков...");
    Console.WriteLine("Это может занять некоторое время...\n");
    
    // Получаем ВСЕ ID аккаунтов
    var allAccountIds = AccountCache.GetAllCachedAccountIds().ToList();
    int totalAccounts = allAccountIds.Count;
    int processedAccounts = 0;
    int successfulResets = 0;
    int failedResets = 0;
    
    Console.WriteLine($"📊 Всего аккаунтов в кэше: {totalAccounts}");
    
    // Создаем лог-файл
    string logFileName = $"BP_Reset_Log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
    List<string> logEntries = new List<string>
    {
        $"=== ПОЛНЫЙ СБРОС БП - {DateTime.UtcNow} ===",
        $"Всего аккаунтов: {totalAccounts}",
        $"Начато: {DateTime.UtcNow}",
        ""
    };
    
    // Обрабатываем каждый аккаунт
    for (int i = 0; i < allAccountIds.Count; i++)
    {
        long accountId = allAccountIds[i];
        processedAccounts++;
        
        try
        {
            // Загружаем аккаунт
            Account account = AccountCache.GetAccount(accountId);
            if (account == null || account.Home == null)
            {
                failedResets++;
                logEntries.Add($"[{i+1}/{totalAccounts}] ID {accountId}: Аккаунт не найден или Home = null");
                continue;
            }
            
            // Получаем старые значения для лога
            object homeObj = account.Home;
            Type homeType = homeObj.GetType();
            
            Dictionary<string, object> oldValues = new Dictionary<string, object>();
            string[] fieldsToReset = { "BrawlPassProgress", "PremiumPassProgress", "BrawlPassPlusProgress", "BrawlPassTokens", "BattleTokens" };
            
            foreach (string fieldName in fieldsToReset)
            {
                FieldInfo field = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    oldValues[fieldName] = field.GetValue(homeObj);
                }
            }
            
            // Сбрасываем поля
            foreach (string fieldName in fieldsToReset)
            {
                FieldInfo field = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        if (field.FieldType == typeof(int))
                            field.SetValue(homeObj, 0);
                        else if (field.FieldType.Name == "BigInteger")
                        {
                            var parseMethod = field.FieldType.GetMethod("Parse", new[] { typeof(string) });
                            if (parseMethod != null)
                            {
                                object zeroValue = parseMethod.Invoke(null, new[] { "0" });
                                field.SetValue(homeObj, zeroValue);
                            }
                        }
                    }
                    catch { }
                }
            }
            
            // Сохраняем изменения
            Accounts.Save(account);
            successfulResets++;
            
            // Логируем
            string logEntry = $"[{i+1}/{totalAccounts}] ID {accountId}: Успешно сброшен";
            if (oldValues.Count > 0)
            {
                logEntry += " (";
                foreach (var kvp in oldValues)
                {
                    logEntry += $"{kvp.Key}: {kvp.Value}→0, ";
                }
                logEntry = logEntry.TrimEnd(',', ' ') + ")";
            }
            
            logEntries.Add(logEntry);
            
            // Если игрок онлайн - кикаем
            if (Sessions.IsSessionActive(accountId))
            {
                var session = Sessions.GetSession(accountId);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "На сервере выполнен сброс Battle Pass! Перезайдите в игру."
                });
                Sessions.Remove(accountId);
                logEntries.Add($"   👤 Игрок был онлайн и отключен");
            }
            
            // Прогресс каждые 50 аккаунтов
            if (processedAccounts % 50 == 0 || processedAccounts == totalAccounts)
            {
                Console.WriteLine($"✅ Обработано: {processedAccounts}/{totalAccounts} (Успешно: {successfulResets}, Ошибок: {failedResets})");
            }
        }
        catch (Exception ex)
        {
            failedResets++;
            logEntries.Add($"[{i+1}/{totalAccounts}] ID {accountId}: ОШИБКА - {ex.Message}");
            
            if (processedAccounts % 20 == 0)
            {
                Console.WriteLine($"⚠️  Ошибки: {failedResets} из {processedAccounts}");
            }
        }
        
        // Небольшая пауза чтобы не перегружать систему
        if (processedAccounts % 100 == 0)
        {
            Thread.Sleep(100);
        }
    }
    
    // Сохраняем лог в файл
    try
    {
        File.WriteAllLines(logFileName, logEntries);
        Console.WriteLine($"\n📝 Лог сохранен в файл: {logFileName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n⚠️  Не удалось сохранить лог: {ex.Message}");
    }
    
    // Финальная статистика
    Console.WriteLine("\n🎯 ОПЕРАЦИЯ ЗАВЕРШЕНА!");
    Console.WriteLine("=======================");
    Console.WriteLine($"📊 ИТОГОВАЯ СТАТИСТИКА:");
    Console.WriteLine($"• Всего аккаунтов: {totalAccounts}");
    Console.WriteLine($"• Обработано: {processedAccounts}");
    Console.WriteLine($"• ✅ Успешно сброшено: {successfulResets}");
    Console.WriteLine($"• ❌ Ошибок: {failedResets}");
    Console.WriteLine($"• ⏱️  Время: {DateTime.UtcNow}");
    
    Console.WriteLine($"\n💾 Принудительно сохраняю все кэши...");
    AccountCache.SaveAll();
    
    Console.WriteLine($"\n📢 РЕКОМЕНДАЦИИ:");
    Console.WriteLine($"1. Выполните /maintenance для перезагрузки сервера");
    Console.WriteLine($"2. Проверьте несколько аккаунтов: /debugbp #ТЕГ");
    Console.WriteLine($"3. Если есть проблемы - проверьте лог файл");
    
    // Логируем в системный лог
    Logger.Print($"Администратор выполнил полный сброс БП у всех игроков. Успешно: {successfulResets}/{totalAccounts}");
}


private static void ResetBPField(Type homeType, object homeObj, string fieldName, object newValue)
{
    FieldInfo field = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
    {
        try
        {
            object oldValue = field.GetValue(homeObj);
            field.SetValue(homeObj, newValue);
            Console.WriteLine($"  ✅ {fieldName}: {oldValue} → {newValue}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {fieldName}: ошибка - {ex.Message}");
        }
    }
}


        
        private static void ClubNotif(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: /clubnotif [TAG] [Message...]");
                Console.WriteLine("Example: /clubnotif #9JG2P2P Welcome to the club!");
                return;
            }
        
            long targetAccountId;
            try
            {
                targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail: Cannot convert tag '{args[1]}' to ID. {ex.Message}");
                return;
            }
        
            string notificationMessage = string.Join(" ", args, 2, args.Length - 2);
            if (string.IsNullOrWhiteSpace(notificationMessage))
                notificationMessage = "You have received a club notification.";
        
            Account account = Accounts.Load(targetAccountId);
            if (account == null)
            {
                Console.WriteLine("Fail: Account not found!");
                return;
            }
        
            var notification = new Notification
            {
                Id = 82,
                MessageEntry = notificationMessage,
                IsViewed = false
            };
        
            if (account.Home.NotificationFactory == null)
                account.Home.NotificationFactory = new NotificationFactory();
        
            account.Home.NotificationFactory.Add(notification);
            Accounts.Save(account);
        
            if (Sessions.IsSessionActive(targetAccountId))
            {
                var session = Sessions.GetSession(targetAccountId);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = $"You have a new club message: {notificationMessage}"
                });
            }
        
            Console.WriteLine($"Success: Club notification sent to {args[1]} ({targetAccountId}). Message: \"{notificationMessage}\"");
        }
        
        private static void ExecuteGiveAlllSkins(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /giveskinsall [TAG]");
        Console.WriteLine("Пример: /giveskinsall #9JG2P2P");
        Console.WriteLine("\n⚠️ Внимание: Эта команда выдаст ВСЕ скины (1-500)");
        Console.WriteLine("Рекомендуется сначала протестировать на одном скине: /giveskin [TAG] 1");
        return;
    }

    long targetAccountId;
    try
    {
        targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    Account account = Accounts.Load(targetAccountId);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    if (account.Home == null)
    {
        Console.WriteLine("Ошибка: account.Home равен null!");
        return;
    }

    Console.WriteLine($"🎯 Начинаю выдачу всех скинов для {args[1]}...");
    Console.WriteLine("⚠️  Это может занять несколько секунд...\n");

    // Инициализируем список скинов если его нет
    if (account.Home.UnlockedSkins == null)
    {
        account.Home.UnlockedSkins = new List<int>();
        Console.WriteLine("ℹ️ Создан новый список UnlockedSkins");
    }

    int skinsAdded = 0;
    int skinsAlreadyUnlocked = 0;
    
    // Диапазон скинов от 1 до 500 (можно изменить по необходимости)
    for (int skinIndex = 1; skinIndex <= 500; skinIndex++)
    {
        int skinGlobalId = 28000000 + skinIndex;
        
        if (!account.Home.UnlockedSkins.Contains(skinGlobalId))
        {
            account.Home.UnlockedSkins.Add(skinGlobalId);
            skinsAdded++;
            
            // Показываем прогресс каждые 50 скинов
            if (skinsAdded % 50 == 0)
            {
                Console.WriteLine($"  Добавлено {skinsAdded} скинов...");
            }
        }
        else
        {
            skinsAlreadyUnlocked++;
        }
    }

    Console.WriteLine($"\n📊 Результат:");
    Console.WriteLine($"  ✅ Добавлено новых скинов: {skinsAdded}");
    Console.WriteLine($"  ℹ️  Уже было разблокировано: {skinsAlreadyUnlocked}");
    Console.WriteLine($"  📈 Всего скинов теперь: {account.Home.UnlockedSkins.Count}");

    // Создаем уведомление о выдаче всех скинов
    var notification = new Notification
    {
        Id = 88, // SkinRewardNotification
        MessageEntry = $"🎉 Вам выданы ВСЕ скины в игре! ({skinsAdded} новых)",
        SkinID = 1 // Произвольный ID для уведомления
    };

    // Добавляем уведомление
    if (account.Home.NotificationFactory == null)
        account.Home.NotificationFactory = new NotificationFactory();

    account.Home.NotificationFactory.Add(notification);
    
    // Сохраняем аккаунт
    Accounts.Save(account);

    // Отправляем уведомление онлайн игроку
    if (Sessions.IsSessionActive(targetAccountId))
    {
        try
        {
            var session = Sessions.GetSession(targetAccountId);
            
            // Отправляем уведомление через команду
            LogicAddNotificationRealCommand addNotificationCommand = new()
            {
                Notification = notification
            };
            
            AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
            serverCommand.Command = addNotificationCommand;
            session.GameListener.SendTCPMessage(serverCommand);
            
            // Отправляем текстовое сообщение
            session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
            {
                Message = $"🎉 Вам выданы ВСЕ скины! ({skinsAdded} новых скинов)"
            });
            
            Console.WriteLine("📨 Уведомление отправлено онлайн игроку");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Ошибка при отправке уведомления онлайн игроку: {ex.Message}");
        }
    }

    Console.WriteLine($"\n🎉 УСПЕХ: Все скины (1-500) выданы игроку {args[1]}!");
    Console.WriteLine("💾 Изменения сохранены.");
    Console.WriteLine($"\n🔍 Проверить количество скинов можно командой:");
    Console.WriteLine($"  /checkhome {args[1]} | grep -i skins");
    
    // Рекомендация для безопасности
    if (skinsAdded > 300)
    {
        Console.WriteLine($"\n⚠️  РЕКОМЕНДАЦИЯ:");
        Console.WriteLine($"Для безопасности отключите игрока на 1-2 минуты");
        Console.WriteLine($"чтобы изменения полностью применились:");
        Console.WriteLine($"  /tempban {args[1]} 1 Автоматическое применение скинов");
    }
}

        private static void gem(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: /gem [TAG] [Amount] [Message...]");
                Console.WriteLine("Example: /gem #9JG2P2P 500 Thank you for playing!");
                return;
            }
        
            long targetAccountId;
            try
            {
                targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail: Cannot convert tag '{args[1]}' to ID. {ex.Message}");
                return;
            }
        
            if (!int.TryParse(args[2], out int gemAmount) || gemAmount <= 0)
            {
                Console.WriteLine("Fail: Invalid gem amount. Please specify a positive integer.");
                return;
            }
        
            string notificationMessage = string.Join(" ", args, 3, args.Length - 3);
            if (string.IsNullOrWhiteSpace(notificationMessage))
                notificationMessage = $"You have received {gemAmount} gems!";
        
            Account account = Accounts.Load(targetAccountId);
            if (account == null)
            {
                Console.WriteLine("Fail: Account not found!");
                return;
            }
        
            var notification = new Notification
            {
                Id = 89,
                MessageEntry = notificationMessage,
                DonationCount = gemAmount
            };
        
            if (account.Home.NotificationFactory == null)
                account.Home.NotificationFactory = new NotificationFactory();
        
            account.Home.NotificationFactory.Add(notification);

            LogicAddNotificationRealCommand acm = new()
            {
                Notification = notification
            };
            AvailableServerCommandMessage asm = new AvailableServerCommandMessage();
            asm.Command = acm;
        
            if (Sessions.IsSessionActive(targetAccountId))
            {
                var session = Sessions.GetSession(targetAccountId);
                session.GameListener.SendTCPMessage(asm);
            }
        
            Console.WriteLine($"Success: {gemAmount} gems sent to {args[1]} ({targetAccountId}). Message: \"{notificationMessage}\"");
        }
        
        
        private static void ExecuteCheckHome(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /checkhome [TAG]");
        return;
    }
    
    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    
    if (account == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }
    
    if (account.Avatar.HomeMode?.Home == null)
    {
        Console.WriteLine("❌ HomeMode или Home не инициализированы!");
        return;
    }
    
    object homeObj = account.Avatar.HomeMode.Home;
    Type homeType = homeObj.GetType();
    
    Console.WriteLine($"🏠 ClientHome для {args[1]}:");
    Console.WriteLine($"Тип: {homeType.Name}");
    Console.WriteLine($"Поля HomeMode: {account.Avatar.HomeMode != null}");
    Console.WriteLine($"Поля Home: {homeObj != null}");
    
    // Посмотрим все поля
    var allFields = homeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    Console.WriteLine($"\n📋 Всего полей: {allFields.Length}");
    
    // Покажем только первые 30 полей
    Console.WriteLine("\n🔍 Первые 30 полей:");
    for (int i = 0; i < Math.Min(30, allFields.Length); i++)
    {
        try
        {
            object value = allFields[i].GetValue(homeObj);
            Console.WriteLine($"{i:00}. {allFields[i].FieldType.Name,-10} {allFields[i].Name,-30} = {value}");
        }
        catch
        {
            Console.WriteLine($"{i:00}. {allFields[i].FieldType.Name,-10} {allFields[i].Name,-30} = [error]");
        }
    }
}
        
        private static void CoinNotif(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: /coinnotif [TAG] [CoinsCount] [Message...]");
                Console.WriteLine("Example: /coinnotif #9JG2P2P 1000 Here are your coins!");
                return;
            }
        
            long targetAccountId;
            try
            {
                targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail: Cannot convert tag '{args[1]}' to ID. {ex.Message}");
                return;
            }
        
            if (!int.TryParse(args[2], out int coinsCount) || coinsCount <= 0)
            {
                Console.WriteLine("Fail: Invalid coin amount. Please specify a positive integer.");
                return;
            }
        
            string notificationMessage = string.Join(" ", args, 3, args.Length - 3);
            if (string.IsNullOrWhiteSpace(notificationMessage))
                notificationMessage = $"You have received {coinsCount} coins!";
        
            Account account = Accounts.Load(targetAccountId);
            if (account == null)
            {
                Console.WriteLine("Fail: Account not found!");
                return;
            }
        
            var notification = new Notification
            {
                Id = 88,
                MessageEntry = notificationMessage,
                DonationCount = coinsCount,
                IsViewed = false
            };
        
            if (account.Home.NotificationFactory == null)
                account.Home.NotificationFactory = new NotificationFactory();
        
            account.Home.NotificationFactory.Add(notification);
            Accounts.Save(account);
        
            if (Sessions.IsSessionActive(targetAccountId))
            {
                var session = Sessions.GetSession(targetAccountId);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = $"You have received {coinsCount} coins!"
                });
            }
        
            Console.WriteLine($"Success: {coinsCount} coins sent to {args[1]} ({targetAccountId}). Message: \"{notificationMessage}\"");
        }
        
        private static void ExecuteResetBPHome(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resetbphome [TAG]");
        Console.WriteLine("Сбрасывает Battle Pass через account.Avatar.HomeMode.Home");
        return;
    }
    
    string tag = args[1];
    long id = LogicLongCodeGenerator.ToId(tag);
    Account account = Accounts.Load(id);
    
    if (account == null)
    {
        Console.WriteLine("❌ Аккаунт не найден!");
        return;
    }
    
    if (account.Avatar.HomeMode?.Home == null)
    {
        Console.WriteLine("❌ HomeMode или Home не инициализированы!");
        return;
    }
    
    object homeObj = account.Avatar.HomeMode.Home;
    Type homeType = homeObj.GetType();
    
    Console.WriteLine($"🔄 Сбрасываю Battle Pass для {tag}...");
    
    // Список полей которые мы видели в /debugbp
    var fieldsToReset = new Dictionary<string, object>
    {
        { "BrawlPassProgress", 0 },
        { "PremiumPassProgress", 0 },
        { "BrawlPassPlusProgress", 0 },
        { "BrawlPassTokens", 0 },
        { "BattleTokens", 0 }
    };
    
    bool success = true;
    
    foreach (var fieldPair in fieldsToReset)
    {
        try
        {
            FieldInfo field = homeType.GetField(fieldPair.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                object oldValue = field.GetValue(homeObj);
                field.SetValue(homeObj, fieldPair.Value);
                object newValue = field.GetValue(homeObj);
                
                Console.WriteLine($"  ✅ {fieldPair.Key}: {oldValue} → {newValue}");
            }
            else
            {
                Console.WriteLine($"  ❌ {fieldPair.Key}: поле не найдено");
                success = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ {fieldPair.Key}: ошибка - {ex.Message}");
            success = false;
        }
    }
    
    if (success)
    {
        Accounts.Save(account);
        Console.WriteLine($"\n🎉 БП успешно сброшен для {tag}!");
        
        if (Sessions.IsSessionActive(id))
        {
            var session = Sessions.GetSession(id);
            session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
            {
                Message = "Ваш боевой пропуск был сброшен!"
            });
            Sessions.Remove(id);
            Console.WriteLine("👤 Игрок отключен");
        }
        
        Console.WriteLine($"\n📊 Проверьте результат: /debugbp {tag}");
    }
    else
    {
        Console.WriteLine($"\n⚠️ Были ошибки при сбросе БП");
        Console.WriteLine("\n🎯 Попробуйте команды по одной:");
        Console.WriteLine($"/changehomevalue {tag} BrawlPassProgress 0");
        Console.WriteLine($"/changehomevalue {tag} PremiumPassProgress 0");
        Console.WriteLine($"/changehomevalue {tag} BrawlPassPlusProgress 0");
        Console.WriteLine($"/changehomevalue {tag} BrawlPassTokens 0");
        Console.WriteLine($"/changehomevalue {tag} BattleTokens 0");
    }
}
        
        
        private static void ExecuteChangeHomeValue(string[] args)
{
    if (args.Length != 4)
    {
        Console.WriteLine("Usage: /changehomevalue [TAG] [FieldName] [Value]");
        Console.WriteLine("Пример: /changehomevalue #82LJP BrawlPassProgress 0");
        return;
    }

    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine("Fail: account not found!");
        return;
    }

    if (account.Home == null)
    {
        Console.WriteLine("Fail: account.Home is null!");
        return;
    }

    string fieldName = args[2];
    object homeObj = account.Home;
    Type homeType = homeObj.GetType();
    
    FieldInfo field = homeType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    
    if (field == null)
    {
        Console.WriteLine($"Fail: Field {fieldName} not found!");
        return;
    }

    try
    {
        // Преобразуем значение с учетом типа поля
        object value;
        Type fieldType = field.FieldType;
        
        if (fieldType == typeof(int))
            value = int.Parse(args[3]);
        else if (fieldType == typeof(bool))
            value = bool.Parse(args[3]);
        else if (fieldType == typeof(long))
            value = long.Parse(args[3]);
        else if (fieldType == typeof(BigInteger))  // Добавляем поддержку BigInteger!
            value = BigInteger.Parse(args[3]);
        else
            value = args[3];

        // Сохраняем старое значение
        object oldValue = field.GetValue(homeObj);
        
        // Устанавливаем новое значение
        field.SetValue(homeObj, value);
        
        Accounts.Save(account);
        
        Console.WriteLine($"✅ Успех: {fieldName} изменено:");
        Console.WriteLine($"   Было: {oldValue}");
        Console.WriteLine($"   Стало: {field.GetValue(homeObj)}");

        if (Sessions.IsSessionActive(id))
        {
            var session = Sessions.GetSession(id);
            session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
            {
                Message = "Ваши данные были изменены администратором!"
            });
            Sessions.Remove(id);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Fail: Ошибка при установке значения: {ex.Message}");
        Console.WriteLine($"Тип поля: {field.FieldType.Name}");
    }
}
        
        private static void ExecuteResetBPAuto(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /resetbp4 [TAG]");
        Console.WriteLine("Автоматически выполняет все команды changevalue");
        return;
    }
    
    string tag = args[1];
    
    Console.WriteLine($"Автоматический сброс БП для {tag}");
    Console.WriteLine("Выполняю команды...\n");
    
    // Список команд для выполнения
    string[] commands = {
        $"/changevalue {tag} BrawlPassProgress 0",
        $"/changevalue {tag} PremiumPassProgress 0",
        $"/changevalue {tag} BrawlPassPlusProgress 0",
        $"/changevalue {tag} BrawlPassTokens 0",
        $"/changevalue {tag} BattleTokens 0"
    };
    
    foreach (string cmd in commands)
    {
        Console.WriteLine($"Выполняю: {cmd}");
        HandleCmd(cmd);
        Console.WriteLine();
        Thread.Sleep(100); // Небольшая пауза
    }
    
    Console.WriteLine($"✅ Все команды выполнены для {tag}");
    Console.WriteLine("Проверьте результат: /debugbp " + tag);
}
        
        private static void ExecuteGivePremiumPassPlus(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /givepassplus [TAG]");
                Console.WriteLine("Example: /givepassplus #9JG2P2P");
                return;
            }

            long targetAccountId;
            try
            {
                targetAccountId = LogicLongCodeGenerator.ToId(args[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fail: Cannot convert tag '{args[1]}' to ID. {ex.Message}");
                return;
            }

            Account account = Accounts.Load(targetAccountId);
            if (account == null)
            {
                Console.WriteLine("Fail: Account not found!");
                return;
            }

            account.Home.HasPremiumPass = true;
            account.Home.HasPremiumPassPlus = true;

            var notification = new Notification
            {
                Id = 81,
                MessageEntry = "Спасибо за покупку Shuza Pass Plus!",
                IsViewed = false
            };

            if (account.Home.NotificationFactory == null)
                account.Home.NotificationFactory = new NotificationFactory();

            account.Home.NotificationFactory.Add(notification);
            Accounts.Save(account);

            if (Sessions.IsSessionActive(targetAccountId))
            {
                var session = Sessions.GetSession(targetAccountId);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Вы получили Shuza Pass Plus!"
                });
                Sessions.Remove(targetAccountId);
            }

            Console.WriteLine($"Success: Premium Pass Plus granted to {args[1]} ({targetAccountId}). Notification sent.");
        }


private static void ExecuteGivePremiumUltra(string[] args)
{
    if (args.Length != 2)
    {
        Console.WriteLine("Usage: /premiumultra [TAG]");
        return;
    }

    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);

    if (account == null)
    {
        Console.WriteLine("Fail: account not found!");
        return;
    }

    // 🔥 Включаем Premium Ultra на 30 дней
    account.Avatar.IsPremiumUltra = true;
    account.Avatar.PremiumUltraTime = DateTime.UtcNow.AddDays(30);

    // 💎 Выдаём гемы
    account.Avatar.Gems += 4000;

    // 🏅 Выдаём титул 184 (как в VIP)
    int premiumTitleGlobalId = GlobalId.CreateGlobalId(76, 184); // 76 = тип титула
    account.Home.UnlockedTituls ??= new List<int>();
    if (!account.Home.UnlockedTituls.Contains(premiumTitleGlobalId))
        account.Home.UnlockedTituls.Add(premiumTitleGlobalId);

    // 🎨 Выдаём скин 185 (как в /giveskin)
    int skinIndex = 185;
    account.Home.UnlockedSkins ??= new List<int>();
    if (!account.Home.UnlockedSkins.Contains(28000000 + skinIndex))
        account.Home.UnlockedSkins.Add(28000000 + skinIndex);

    // 🔔 Уведомление для скина
    var skinNotification = new Notification
    {
        Id = 94, // SkinRewardNotification
        SkinID = skinIndex,
        MessageEntry = $"🎨 Вы получили золотой скин {skinIndex}!"
    };

    // 🔔 Уведомление для премиума и гемов
    var premiumNotification = new Notification
    {
        Id = 89, // GemRewardNotification
        DonationCount = 4000,
        MessageEntry = $"💎 PREMIUM ULTRA активирован!\n+4000 гемов\n🏅 Титул 184 выдан!"
    };

    // Добавляем уведомления
    account.Home.NotificationFactory ??= new NotificationFactory();
    account.Home.NotificationFactory.Add(skinNotification);
    account.Home.NotificationFactory.Add(premiumNotification);

    // 💾 Сохраняем изменения
    Accounts.Save(account);

    // 🔄 Отправляем уведомления онлайн игроку
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);

        LogicAddNotificationRealCommand skinCmd = new() { Notification = skinNotification };
        LogicAddNotificationRealCommand premiumCmd = new() { Notification = premiumNotification };

        session.GameListener.SendTCPMessage(new AvailableServerCommandMessage() { Command = skinCmd });
        session.GameListener.SendTCPMessage(new AvailableServerCommandMessage() { Command = premiumCmd });

        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
        {
            Message = "💎 PREMIUM ULTRA активирован! Гемы, титул и скин выданы."
        });

        Sessions.Remove(id);
    }

    Console.WriteLine($"🔥 PREMIUM ULTRA выдан {args[1]} (4000 гемов + титул 184 + скин 185)");
}

private static void ExecuteGiveNameNew(string[] args)
{
    if (args.Length != 3)
    {
        Console.WriteLine("Usage: /givenamenew [TAG] [TitleID]");
        return;
    }

    // Преобразуем TAG в ID
    long id;
    try
    {
        id = LogicLongCodeGenerator.ToId(args[1]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[1]}'. {ex.Message}");
        return;
    }

    // Парсим локальный ID титула
    if (!int.TryParse(args[2], out int titleId) || titleId <= 0)
    {
        Console.WriteLine("Ошибка: Неверный TitleID. Укажите положительное число.");
        return;
    }

    // Загружаем аккаунт
    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine("Ошибка: Аккаунт не найден!");
        return;
    }

    // ---------------- Выдаём титул ----------------
    account.Home.UnlockedTituls ??= new List<int>();
    int globalTitleId = GlobalId.CreateGlobalId(76, titleId);

    if (!account.Home.UnlockedTituls.Contains(globalTitleId))
    {
        account.Home.UnlockedTituls.Add(globalTitleId);

        // Создаём уведомление
        var notification = new Notification
        {
            Id = 88,
            MessageEntry = $"🏅 Вам выдан титул {titleId}!"
        };

        account.Home.NotificationFactory ??= new NotificationFactory();
        account.Home.NotificationFactory.Add(notification);

        // ---------------- Отправка онлайн игроку ----------------
        // Используем ID, который ты использовал для загрузки аккаунта
        if (Sessions.IsSessionActive(id))
        {
            var session = Sessions.GetSession(id);
            var cmd = new AvailableServerCommandMessage
            {
                Command = new LogicAddNotificationRealCommand { Notification = notification }
            };
            session.GameListener.SendTCPMessage(cmd);
        }

        // Сохраняем аккаунт
        Accounts.Save(account);
    }

    Console.WriteLine($"✅ Титул {titleId} выдан игроку {args[1]} через /givenamenew");
}


private static void ExecuteRedName(string[] args)
{
    long id = LogicLongCodeGenerator.ToId(args[1]);
    Account account = Accounts.Load(id);

    if (!account.Avatar.Name.StartsWith("§c"))
        account.Avatar.Name = "§c" + account.Avatar.Name;

    Accounts.Save(account);

    if (Sessions.IsSessionActive(id))
    {
        var s = Sessions.GetSession(id);
        s.GameListener.SendTCPMessage(new AuthenticationFailedMessage
        {
            Message = "🔴 Красный ник активирован"
        });
        Sessions.Remove(id);
    }
}


private static void ExecuteDevName(string[] args)
{
    if (args.Length != 3)
    {
        Console.WriteLine("Usage: /devname on|off [TAG]");
        return;
    }

    bool enable = args[1].Equals("on", StringComparison.OrdinalIgnoreCase);

    long id;
    try
    {
        id = LogicLongCodeGenerator.ToId(args[2]);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: Неверный TAG '{args[2]}'. {ex.Message}");
        return;
    }

    Account account = Accounts.Load(id);
    if (account == null)
    {
        Console.WriteLine("Ошибка: аккаунт не найден!");
        return;
    }

    // Включаем/выключаем префикс
    if (enable)
    {
        account.Avatar.SetDevPrefix(true);                 // ставим [DEV]
        account.Avatar.Name = "[DEV]\n" + account.Avatar.Name; // красим ник в красный
    }
    else
    {
        account.Avatar.SetDevPrefix(false);               // убираем [DEV]
        account.Avatar.Name = account.Avatar.Name.Replace("[DEV]\n", ""); // убираем красный
    }

    Accounts.Save(account);

    // Если игрок онлайн, отправляем уведомление
    if (Sessions.IsSessionActive(id))
    {
        var session = Sessions.GetSession(id);

        // Просто уведомляем игрока, чтобы увидел эффект при следующей сессии
        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage
        {
            Message = enable ? "👑 DEV префикс включён! Ник станет красным в бою." 
                             : "DEV префикс отключён!"
        });
    }

    Console.WriteLine($"DEV префикс для {args[2]} {(enable ? "включён" : "выключен")}");
}

        
        private static void ExecuteShutdown()
        {
            Sessions.StartShutdown();
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;
        }
    }
}