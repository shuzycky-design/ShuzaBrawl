using Newtonsoft.Json; // ���������, ��� ��� ����������
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Titan.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndusBrawl.Laser.Logic.Home.Items
{
    public class GemOffer
    {
        [JsonProperty("Type")]
        public int Type { get; set; }

        [JsonProperty("Count")]
        public int Count { get; set; }

        [JsonProperty("BrawlerData")]
        public int BrawlerData { get; set; }

        [JsonProperty("BrawlerData1")]
        public int BrawlerData1 { get; set; }

        [JsonProperty("BrawlerData2")]
        public int BrawlerData2 { get; set; }

        [JsonProperty("ExtraData")]
        public int ExtraData { get; set; }

        [JsonProperty("SettedUp")]
        public bool SettedUp { get; set; }
        public GemOffer()
        {
            // needed constructor!!
        }

        public GemOffer(int _type, int _count, int _brawlerData, int _extraData)
        {
            Type = _type;
            Count = _count;
            BrawlerData = _brawlerData;
            ExtraData = _extraData;
        }

        public GemOffer(int _type, int _count, int _brawlerData1, int _brawlerData2, int _extraData)
        {
            Type = _type;
            Count = _count;
            BrawlerData1 = _brawlerData1;
            BrawlerData2 = _brawlerData2;
            ExtraData = _extraData;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteVInt(Count);
            if (BrawlerData1 <= 0)
                ByteStreamHelper.WriteDataReference(stream, BrawlerData);
            else
                stream.WriteDataReference(BrawlerData1, BrawlerData2);
            stream.WriteVInt(ExtraData);
        }

        public void Encode(ChecksumEncoder stream)
        {
            stream.WriteVInt(Type);
            stream.WriteVInt(Count);
            if (BrawlerData1 <= 0)
                ByteStreamHelper.WriteDataReference(stream, BrawlerData);
            else
                ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(BrawlerData1, BrawlerData2));
            stream.WriteVInt(ExtraData);
        }
    }
}