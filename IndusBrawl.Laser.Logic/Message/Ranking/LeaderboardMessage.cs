// LeaderboardMessage.cs
namespace IndusBrawl.Laser.Logic.Message.Ranking
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Club;
    using IndusBrawl.Laser.Logic.Helper;
    using System.Collections.Generic;
    using System.Linq;

    public class LeaderboardMessage : GameMessage
    {
        public int LeaderboardType { get; set; }

        public List<KeyValuePair<ClientHome, ClientAvatar>> Avatars;
        public List<Alliance> AllianceList;
        public long OwnAvatarId;
        public string Region { get; set; }
        public int HeroDataId;

        public string AvatarRegion;
                public int idk;
        // Добавляем словарь для имен клубов
        public Dictionary<long, string> AllianceNames { get; set; }

        public LeaderboardMessage() : base()
        {
            Avatars = new List<KeyValuePair<ClientHome, ClientAvatar>>();
            AllianceList = new List<Alliance>();
            AllianceNames = new Dictionary<long, string>(); // Инициализация
            LeaderboardType = 1; // По умолчанию общий лидерборд
        }

        public override void Encode()
        {
            int playerIndex = 0;

            Stream.WriteVInt(LeaderboardType);
            Stream.WriteVInt(idk); // Unknown
            ByteStreamHelper.WriteDataReference(Stream, HeroDataId);
            Stream.WriteString(Region); // Регион

            if (LeaderboardType == 1)
            {
                // Лидерборд по общим кубкам
                var sortedAvatars = Avatars
                    .Where(pair => pair.Value != null)
                    .OrderByDescending(pair => pair.Value.Trophies)
                    .ToList();

                for (int i = 0; i < sortedAvatars.Count; i++)
                {
                    if (sortedAvatars[i].Value.AccountId == OwnAvatarId)
                    {
                        playerIndex = i + 1;
                        break;
                    }
                }

                WriteLeaderboardEntries(sortedAvatars);
            }
            
                else if (LeaderboardType == 2)
            {
                Stream.WriteVInt(AllianceList.Count);
                foreach (var alliance in AllianceList)
                {
                    Stream.WriteVLong(alliance.Id);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(alliance.Trophies);

                    Stream.WriteVInt(2);

                    Stream.WriteString(alliance.Name);
                    Stream.WriteVInt(alliance.Members.Count);
                    ByteStreamHelper.WriteDataReference(Stream, alliance.AllianceBadgeId);
                }
            }
            else if (LeaderboardType == 0)
            {
                // Лидерборд по кубкам героя
                var validHeroes = Avatars
                    .Where(pair => pair.Value != null &&
                                   pair.Value.GetHero(HeroDataId) != null)
                    .ToList();

                var sortedAvatars = validHeroes
                    .OrderByDescending(pair => pair.Value.GetHero(HeroDataId).Trophies)
                    .ToList();

                for (int i = 0; i < sortedAvatars.Count; i++)
                {
                    if (sortedAvatars[i].Value.AccountId == OwnAvatarId)
                    {
                        playerIndex = i + 1;
                        break;
                    }
                }

                WriteLeaderboardEntries(sortedAvatars);
            }

            Stream.WriteVInt(0); // Unknown
            Stream.WriteVInt(playerIndex); // Позиция игрока
            Stream.WriteVInt(0); // Unknown
            Stream.WriteVInt(0); // Unknown
            Stream.WriteString(AvatarRegion);
        }

        private void WriteLeaderboardEntries(List<KeyValuePair<ClientHome, ClientAvatar>> entries)
        {
            Stream.WriteVInt(entries.Count);

            foreach (var pair in entries)
            {
                var avatar = pair.Value;
                var home = pair.Key;

                Stream.WriteVLong(avatar.AccountId);

                Stream.WriteVInt(1); // Неизвестное значение
                switch(LeaderboardType)
                {
                	case 0 or 1:
                	Stream.WriteVInt(LeaderboardType == 1 ? avatar.Trophies : avatar.GetHero(HeroDataId).Trophies);
			break;	
			case 4 or 5:
			Stream.WriteVInt(LeaderboardType == 4 ? home.RankedSoloRank : home.RankedTrioRank);
			break;
			default:
			Stream.WriteVInt(0);
			break;
                }

                Stream.WriteBoolean(true);
                
                // Изменяем только эту строку - получаем имя клуба из AllianceNames
                string clubName = "";
                if (avatar.AllianceId > 0 && AllianceNames.ContainsKey(avatar.AllianceId))
                {
                    clubName = AllianceNames[avatar.AllianceId];
                }
                Stream.WriteString(clubName); // имя клуба

                Stream.WriteString(avatar.Name ?? "Player");
                Stream.WriteVInt(100); // Level
                Stream.WriteVInt(home.ThumbnailId); // Иконка профиля

                // ✅ Берём цвет имени из базы данных
                int nameColorId = home.NameColorId;
                Stream.WriteVInt(nameColorId); // Цвет имени из базы

                Stream.WriteVInt(0); // ID альянса (фейк, должно быть 0)
                Stream.WriteBoolean(false); // Есть ли альянс (фейк)
            }
        }

        public override int GetMessageType()
        {
            return 24403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}