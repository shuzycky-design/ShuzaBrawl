using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic.Home.Items
{
    public class Notification
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public bool IsViewed { get; set; }

        // Хранит время создания уведомления (в UTC)
        [JsonProperty]
        public DateTime CreatedAt { get; set; }

        public string MessageEntry { get; set; }
        public string PrimaryMessageEntry { get; set; }
        public string SecondaryMessageEntry { get; set; }
        public string ButtonMessageEntry { get; set; }
        public string FileLocation { get; set; }
        public string FileSha { get; set; }
        public string ExtLint { get; set; }

        public List<int> HeroesIds { get; set; }
        public List<int> HeroesTrophies { get; set; }
        public List<int> HeroesTrophiesReseted { get; set; }
        public List<int> StarpointsAwarded { get; set; }

        public int CoinsCount;
        public int DonationCount;
        public int SkinID;
        public int BrawlerID;
        public int ResourceID;
        public int ResourceCount;

        public int RevokePersonHighID;
        public int RevokePersonLowID;
        public int RevokeCount;

        // Конструктор — устанавливает время создания
        public Notification()
        {
            CreatedAt = DateTime.UtcNow;
            HeroesIds = new List<int>();
            HeroesTrophies = new List<int>();
            HeroesTrophiesReseted = new List<int>();
            StarpointsAwarded = new List<int>();
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Id);
            stream.WriteInt(Index);
            stream.WriteBoolean(IsViewed);

            // Вычисляем время, прошедшее с создания (в секундах)
            int timePassed = (int)(DateTime.UtcNow - CreatedAt).TotalSeconds;
            stream.WriteInt(timePassed);

            stream.WriteString(MessageEntry); // base || FloaterTextNotification
            stream.WriteVInt(0);

            switch (Id)
            {
                case 2: // DonateNotification
                    stream.WriteString(null);
                    break;

                case 63: // ChallengeRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(1);
                    stream.WriteDataReference(0, 0);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteString(null);
                    break;

                case 64: // BoxRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    break;

                case 66: // FloaterTextNotification
                    break;

                case 67: // RankedMidSeasonRewardNotification
                    break;

                case 68: // RankedSeasonEndNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteBoolean(true);
                    stream.WriteVInt(1);
                    stream.WriteDataReference(0, 0);
                    stream.WriteVInt(1);
                    stream.WriteVInt(1);
                    break;

                case 69: // BrawlPassAutoCollectSeasonNotification
                    break;

                case 70: // ChallengeRewardNotification
                    break;

                case 71: // BrawlPassPointRewardNotification
                    break;

                case 72: // VanityItemRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    break;

                case 73: // BrawlPassRewardNotification
                    stream.WriteVInt(0);
                    break;

                case 75: // ChallengeSkinRewardNotification
                    stream.WriteVInt(0);
                    break;

                case 76: // QualifyNotification
                    break;

                case 77: // ProLeagueSeasonEndNotification
                    break;

                case 78: // RankRewardNotification
                    break;

                case 79: // StarPointsNotification
                    stream.WriteVInt(HeroesIds.Count);
                    for (int i = 0; i < HeroesIds.Count; i++)
                    {
                        stream.WriteVInt(HeroesIds[i]);
                        stream.WriteVInt(HeroesTrophies[i]);
                        stream.WriteVInt(HeroesTrophiesReseted[i]);
                        stream.WriteVInt(StarpointsAwarded[i]);
                    }
                    break;

                case 81: // text notification
                    stream.WriteVInt(1);
                    break;

                case 82: // BandNotification
                    stream.WriteVInt(0);
                    stream.WriteString("da");
                    break;

                case 83: // PromoPopupNotification
                    stream.WriteInt(0);
                    stream.WriteStringReference(PrimaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(SecondaryMessageEntry);
                    stream.WriteInt(0);
                    stream.WriteStringReference(ButtonMessageEntry);
                    stream.WriteStringReference(FileLocation);
                    stream.WriteStringReference(FileSha);
                    stream.WriteStringReference(ExtLint);
                    break;

                case 84: // StarPowerRewardNotification
                    break;

                case 85: // RevokeNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(RevokeCount);
                    stream.WriteInt(RevokePersonHighID);
                    stream.WriteInt(RevokePersonLowID);
                    stream.WriteVInt(0);
                    stream.WriteString("");
                    break;

                case 86: // IAPDeliveryNotification
                    break;

                case 87: // crash, do not use
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(DonationCount);
                    break;

                case 88: // TokensDoublerNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(DonationCount);
                    break;

                case 89: // GemRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(DonationCount);
                    break;

                case 90: // ResourceRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(5000000 + ResourceID);
                    stream.WriteVInt(ResourceCount);
                    break;

                case 93: // HeroRewardNotification
                    stream.WriteVInt(0);
                    stream.WriteVInt(16000000 + BrawlerID);
                    break;

                case 94: // SkinRewardNotification
                    stream.WriteVInt(29000000 + SkinID);
                    break;

                default: // FreeTextNotification
                    stream.WriteVInt(0);
                    break;
            }
        }
    }
}