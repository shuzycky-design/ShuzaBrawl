namespace IndusBrawl.Laser.Server.Logic.Game
{
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Home.Items;
    using IndusBrawl.Laser.Server.Database;
    using IndusBrawl.Laser.Titan.Json;

    public static class Events
    {
        public const int REFRESH_MINUTES = 2100;

        private static Timer RefreshTimer;
        private static EventSlotConfig[] ConfigSlots;
        private static Dictionary<int, EventData> Slots;

        public static void Init()
        {
            LoadSettings();
            Slots = new Dictionary<int, EventData>();
            GenerateEvents(); // ВАЖНО: сразу генерируем ивенты при старте

            RefreshTimer = new Timer(new TimerCallback(RefreshTimerElapsed), null, 0, REFRESH_MINUTES * 60 * 1000);
        }

        private static void GenerateEvents()
        {
            if (Slots == null)
                Slots = new Dictionary<int, EventData>();

            if (ConfigSlots == null || ConfigSlots.Length == 0)
            {
                LoadSettings(); // пробуем перезагрузить
                if (ConfigSlots == null || ConfigSlots.Length == 0)
                    return; // нет конфига - выходим
            }

            for (int i = 0; i < ConfigSlots.Length; i++)
            {
                EventData ev = GenerateEvent(
                    ConfigSlots[i].AllowedModes,
                    ConfigSlots[i].Slot,
                    ConfigSlots[i].location,
                    ConfigSlots[i].modifi
                );

                if (ev != null)
                {
                    Slots[ConfigSlots[i].Slot] = ev;
                }
                else
                {
                    // Если ивент не сгенерировался - создаем дефолтный
                    Slots[ConfigSlots[i].Slot] = CreateDefaultEvent(ConfigSlots[i].Slot);
                }
            }
        }

        private static EventData CreateDefaultEvent(int slot)
        {
            EventData ev = new EventData();
            ev.EndTime = DateTime.Now.AddMinutes(REFRESH_MINUTES);
            
            // Берем первую доступную локацию
            LocationData defaultLocation = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(0);
            ev.LocationId = defaultLocation?.GetGlobalId() ?? 0;
            ev.Slot = slot;
            ev.modifi = new HashSet<int>();
            
            return ev;
        }

        private static void RefreshTimerElapsed(object s)
        {
            GenerateEvents();
        }

        private static EventData GenerateEvent(string[] gameModes, int slot, int presetlocation, HashSet<int> modifi)
        {
            try
            {
                if (presetlocation != 0)
                {
                    LocationData location = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(presetlocation);
                    if (location != null)
                    {
                        EventData ev = new EventData();
                        ev.EndTime = DateTime.Now.AddMinutes(REFRESH_MINUTES);
                        ev.LocationId = location.GetGlobalId();
                        ev.Slot = slot;
                        ev.modifi = modifi ?? new HashSet<int>();
                        return ev;
                    }
                }

                int count = DataTables.Get(DataType.Location).Count;
                if (count == 0) return null;

                Random rand = new Random();
                int tries = 0;
                
                // Сначала ищем неудаленные локации
                while (tries < 1000)
                {
                    LocationData location = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(rand.Next(0, count));
                    if (location != null && !location.Disabled && gameModes.Contains(location.GameModeVariation))
                    {
                        EventData ev = new EventData();
                        ev.EndTime = DateTime.Now.AddMinutes(REFRESH_MINUTES);
                        ev.LocationId = location.GetGlobalId();
                        ev.Slot = slot;
                        ev.modifi = modifi ?? new HashSet<int>();
                        return ev;
                    }
                    tries++;
                }

                tries = 0;
                // Если не нашли - ищем любые подходящие
                while (tries < 1000)
                {
                    LocationData location = DataTables.Get(DataType.Location).GetDataWithId<LocationData>(rand.Next(0, count));
                    if (location != null && gameModes.Contains(location.GameModeVariation))
                    {
                        EventData ev = new EventData();
                        ev.EndTime = DateTime.Now.AddMinutes(REFRESH_MINUTES);
                        ev.LocationId = location.GetGlobalId();
                        ev.Slot = slot;
                        ev.modifi = modifi ?? new HashSet<int>();
                        return ev;
                    }
                    tries++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Events] Error generating event for slot {slot}: {ex.Message}");
            }

            return null;
        }

        private static void LoadSettings()
        {
            try
            {
                if (!File.Exists("gameplay.json"))
                {
                    Console.WriteLine("[Events] gameplay.json not found!");
                    ConfigSlots = new EventSlotConfig[0];
                    return;
                }

                LogicJSONObject settings = LogicJSONParser.ParseObject(File.ReadAllText("gameplay.json"));
                LogicJSONArray slots = settings.GetJSONArray("slots");
                
                if (slots == null)
                {
                    ConfigSlots = new EventSlotConfig[0];
                    return;
                }

                ConfigSlots = new EventSlotConfig[slots.Size()];

                for (int i = 0; i < slots.Size(); i++)
                {
                    EventSlotConfig config = new EventSlotConfig();
                    LogicJSONObject slot = slots.GetJSONObject(i);

                    config.Slot = slot.GetJSONNumber("slot")?.GetIntValue() ?? 0;
                    config.location = slot.GetJSONNumber("location")?.GetIntValue() ?? 0;
                    config.modifi = new HashSet<int>();
                    config.AllowedModes = new string[0];

                    // Загружаем модификаторы
                    LogicJSONArray modifi = slot.GetJSONArray("modificators");
                    if (modifi != null)
                    {
                        for (int k = 0; k < modifi.Size(); k++)
                        {
                            LogicJSONNumber numberNode = modifi.GetJSONNumber(k);
                            if (numberNode != null)
                                config.modifi.Add(numberNode.GetIntValue());
                        }
                    }

                    // Загружаем режимы игры
                    LogicJSONArray gameModes = slot.GetJSONArray("game_modes");
                    if (gameModes != null)
                    {
                        config.AllowedModes = new string[gameModes.Size()];
                        for (int j = 0; j < gameModes.Size(); j++)
                        {
                            LogicJSONString modeNode = gameModes.GetJSONString(j);
                            if (modeNode != null)
                                config.AllowedModes[j] = modeNode.GetStringValue();
                        }
                    }

                    ConfigSlots[i] = config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Events] Error loading settings: {ex.Message}");
                ConfigSlots = new EventSlotConfig[0];
            }
        }

        public static EventData GetEvent(int i)
        {
            if (Slots == null)
            {
                Slots = new Dictionary<int, EventData>();
                GenerateEvents();
            }

            if (Slots.ContainsKey(i) && Slots[i] != null)
                return Slots[i];
                
            return null;
        }

        public static bool HasSlot(int slot)
        {
            if (Slots == null) return false;
            return Slots.ContainsKey(slot) && Slots[slot] != null;
        }

        public static EventData[] GetEvents()
        {
            if (Slots == null)
            {
                Slots = new Dictionary<int, EventData>();
                GenerateEvents();
            }

            // Фильтруем null значения и возвращаем массив
            return Slots.Values
                .Where(e => e != null)
                .ToArray();
        }

        private class EventSlotConfig
        {
            public int Slot { get; set; }
            public string[] AllowedModes { get; set; }
            public int location;
            public HashSet<int> modifi = new HashSet<int>();
        }
    }
}