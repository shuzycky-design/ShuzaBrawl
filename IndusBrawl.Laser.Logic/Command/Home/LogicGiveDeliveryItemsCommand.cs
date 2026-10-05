namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Titan.DataStream;
    using System.Collections.Generic;

    public class LogicGiveDeliveryItemsCommand : Command
    {
        public readonly List<DeliveryUnit> DeliveryUnits;
        public int RewardTrackType { get; set; }
        public int RewardForRank { get; set; }
        public int BrawlPassSeason { get; set; }

        public bool BrawlPassExecute { get; set; }
        public bool Credits { get; set; }

        public bool StarrDropExecute { get; set; }

        // Испытательный режим стардропов (см. StarrDropLab)
        // Точный формат клиента для стардропа: номер ячейки пути славы/пропуска стоит внутри блока (см. Encode)
        public bool StarrTwoPhase;
        public int Presentation;   // способ показа на клиенте: 0 - обычное окно наград, 5 - окно стардропа, 2/4/6/7 - ничего не показывать
        public StarrDropLabConfig Lab;
        public int LabRarity;

        public LogicGiveDeliveryItemsCommand() : base()
        {
            DeliveryUnits = new List<DeliveryUnit>();
        }

        public override void Encode(ByteStream stream)
        {
            // СОЗДАЕМ КОПИЮ списка для безопасного перебора
            DeliveryUnit[] unitsCopy;
            
            lock (DeliveryUnits)  // ← БЛОКИРУЕМ доступ к списку
            {
                // Создаем копию ВНУТРИ блокировки
                unitsCopy = DeliveryUnits.ToArray();
                
                if (BrawlPassExecute && Credits)
                {
                    stream.WriteBoolean(false);
                    stream.WriteVInt(RewardTrackType);
                    stream.WriteVInt(RewardForRank);
                    stream.WriteVInt(BrawlPassSeason);
                    stream.WriteVInt(0);
                    return;
                }

                stream.WriteVInt(0);
                stream.WriteVInt(DeliveryUnits.Count);
            }  // ← КОНЕЦ блокировки (освобождаем как можно раньше)

            // Теперь безопасно работаем с КОПИЕЙ
            foreach (DeliveryUnit unit in unitsCopy)
            {
                unit.Encode(stream);
            }

            if (StarrDropExecute && StarrTwoPhase)
            {
                // Разобрано по клиенту (LogicGiveDeliveryItemsCommand::encode):
                // Boolean есть-ли-данные-шансов + сами данные, затем дорожка, ячейка, сезон, способ показа (5 = окно стардропа),
                // два Boolean, ссылка на данные, два VInt. Отдельный блок пропуска после этого НЕ пишется.
                stream.WriteByte(1);
                stream.WriteVInt(200);
                stream.WriteVInt(200);
                stream.WriteVInt(5);
                stream.WriteVInt(93);
                stream.WriteVInt(206);
                stream.WriteVInt(456);
                stream.WriteVInt(1001);
                stream.WriteVInt(2264);
                stream.WriteVInt(RewardTrackType);
                stream.WriteVInt(RewardForRank);
                stream.WriteVInt(BrawlPassSeason);
                stream.WriteVInt(5);
                stream.WriteByte((byte)(Lab?.FlagsByte ?? 2));
                StarrDropLabConfig.WriteContainer(stream, Lab?.DeliveryContainer ?? -2, LabRarity);
                stream.WriteVInt(Lab?.F60 ?? -1);
                stream.WriteVInt(Lab?.F64 ?? -1);

                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                base.Encode(stream);
                return;
            }

            if (StarrDropExecute && Lab?.DeliveryTrailer != null)
            {
                StarrDropLab.Write(stream, Lab.DeliveryTrailer, LabRarity);
            }
            else if (StarrDropExecute)
            {
                stream.WriteByte(1);
                stream.WriteVInt(200);
                stream.WriteVInt(200);
                stream.WriteVInt(5);
                stream.WriteVInt(93);
                stream.WriteVInt(206);
                stream.WriteVInt(456);
                stream.WriteVInt(1001);
                stream.WriteVInt(2264);

                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(5);
                stream.WriteByte(2);
                stream.WriteDataReference(0, 0);
                stream.WriteVInt(-1);
                stream.WriteVInt(-1);
            }

            if (BrawlPassExecute)
            {
                if (!(StarrDropExecute && Lab != null && Lab.SkipPassBool)) stream.WriteBoolean(false);
                stream.WriteVInt(RewardTrackType);
                stream.WriteVInt(RewardForRank);
                stream.WriteVInt(BrawlPassSeason);
                stream.WriteVInt(Presentation);
            }
            else
            {
                stream.WriteVInt(0);
                stream.WriteVInt(0);
            }

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            // Создаем копию списка для безопасного перебора
            List<DeliveryUnit> unitsCopy;

            lock (DeliveryUnits)
            {
                unitsCopy = new List<DeliveryUnit>(DeliveryUnits);
            }

            foreach (DeliveryUnit unit in unitsCopy)
            {
                List<GatchaDrop> dropsCopy;

                // Также копируем drops для безопасности
                lock (unit.GetDrops())
                {
                    dropsCopy = new List<GatchaDrop>(unit.GetDrops());
                }

                foreach (GatchaDrop drop in dropsCopy)
                {
                    drop.DoDrop(homeMode);
                }
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 203;
        }
    }
}