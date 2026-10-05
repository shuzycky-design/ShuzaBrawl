﻿namespace IndusBrawl.Laser.Logic.Home.Structures
{
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home.Structures;
    using IndusBrawl.Laser.Logic.Avatar.Structures;
    using IndusBrawl.Laser.Logic.Helper;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Math;

    public class Profile
    {
        public PlayerDisplayData DisplayData;
        public long AccountId;
        public Hero[] Heroes;
        public List<LogicVector2> Stats;
        public int Hero;
        public int HeroSkin;
        public int T1;
        public int T2;
        public int E;
        public int Ti;
        public ClientAvatar Avatar;

        public Profile()
        {
            DisplayData = new PlayerDisplayData();
            Stats = new List<LogicVector2>();
        }

        public void AddStat(int key, int value)
        {
            Stats.Add(new LogicVector2(key, value));
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVLong(AccountId);
            ByteStreamHelper.WriteDataReference(stream, Hero);

            stream.WriteVInt(Heroes.Length);
            foreach (Hero hero in Heroes)
            {
                hero.Encode(stream);
            }

            stream.WriteVInt(Stats.Count);
            foreach (LogicVector2 stat in Stats)
            {
                stat.Encode(stream);
            }

            DisplayData.Encode(stream);

            stream.WriteBoolean(false);

            stream.WriteString("str");
            stream.WriteVInt(100);
            stream.WriteVInt(200);
            stream.WriteVInt(Avatar.MaxWinstreak);
            ByteStreamHelper.WriteDataReference(stream, HeroSkin);
            ByteStreamHelper.WriteDataReference(stream, T1);
            ByteStreamHelper.WriteDataReference(stream, T2);
            ByteStreamHelper.WriteDataReference(stream, E);
            ByteStreamHelper.WriteDataReference(stream, Ti);
        }

        public static Profile Create(ClientHome home, ClientAvatar avatar)
        {
            Profile profile = new Profile();
            profile.Avatar = avatar;

            profile.AccountId = avatar.AccountId;
            
            PlayerDisplayData playerDisplayData = new PlayerDisplayData();
            playerDisplayData.ThumbnailId = home.ThumbnailId;
            playerDisplayData.NameColorId = home.NameColorId;
            playerDisplayData.Name = avatar.Name;
            profile.DisplayData = playerDisplayData;

            profile.Heroes = avatar.Heroes.ToArray();

            // Основная статистика
            profile.AddStat(1, avatar.TrioWins);           // Победы в трио
            profile.AddStat(2, 0);                         // Experience или место в топе
            profile.AddStat(3, avatar.Trophies);           // Всего кубков
            profile.AddStat(4, avatar.HighestTrophies);    // Максимум кубков
            profile.AddStat(5, profile.Heroes.Length);     // Количество бойцов
            profile.AddStat(7, home.ThumbnailId);          // ID аватарки
            profile.AddStat(8, avatar.SoloWins);           // Победы в соло
            profile.AddStat(20, avatar.FamePoints);        // Очки славы
            
            // Ранкед статистика (из первого файла)
            profile.AddStat(18, home.RankedSoloMaxRank > 0 ? home.RankedSoloMaxRank : 1);  // Макс ранг в соло
            profile.AddStat(17, home.RankedTrioMaxRank > 0 ? home.RankedTrioMaxRank : 1);  // Макс ранг в трио

            profile.Hero = home.FavouriteCharacter;        // Любимый боец
            
            // Получаем выбранный скин любимого бравлера
            Hero favoriteHero = avatar.GetHero(home.FavouriteCharacter);
            if (favoriteHero != null)
            {
                profile.HeroSkin = 29000000 + favoriteHero.SelectedSkinId;
            }
            else
            {
                profile.HeroSkin = 0;
            }
            
            // Данные боевой карточки
            profile.T1 = home.DefaultBattleCard != null ? home.DefaultBattleCard.Thumbnail1 : 0;
            profile.T2 = home.DefaultBattleCard != null ? home.DefaultBattleCard.Thumbnail2 : 0;
            profile.E = home.DefaultBattleCard != null ? home.DefaultBattleCard.Emote : 0;
            profile.Ti = home.DefaultBattleCard != null ? home.DefaultBattleCard.Title : 0;
            
            return profile;
        }
    }
}