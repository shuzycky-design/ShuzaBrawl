using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IndusBrawl.Laser.Logic.Message.Club
{
    public class BattleLogMessage : GameMessage
    {


        public HomeMode homeMode;

        public BattleLogMessage() : base()
        {
            ;
        }
        public override void Encode()
        {
            Stream.WriteBoolean(true); // enabled

            // Выключатель без перезапуска: если рядом с сервером лежит файл battlelog_off, история отправляется пустой
            // всем, кроме аккаунтов, чьи номера перечислены в этом файле (через пробел или с новой строки)
            bool hidden = false;
            try
            {
                if (homeMode != null && System.IO.File.Exists("battlelog_off"))
                {
                    string[] allowed = System.IO.File.ReadAllText("battlelog_off").Split(new[] { ' ', ',', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                    hidden = System.Array.IndexOf(allowed, homeMode.Avatar.AccountId.ToString()) < 0;
                }
            }
            catch (System.Exception) { hidden = true; }

            if (hidden) Stream.WriteVInt(0);
            else if (homeMode != null) homeMode.Home.BattleLogs.Encode(Stream);
            else
            {
                Console.WriteLine("[BATTLELOGMESSAGE] HOMEMODE IS NULL!");
                Stream.WriteBoolean(false);
            }

        }

        public override int GetMessageType()
        {
            return 23458;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}