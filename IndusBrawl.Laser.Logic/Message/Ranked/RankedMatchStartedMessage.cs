using System.Collections.Generic;
using System.IO;
using System.Linq;
using IndusBrawl.Laser.Logic.Avatar.Structures;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Logic.Ranked;

namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchStartedMessage : GameMessage
    {
        public bool Stub;
        public int Modificator;
        public int SelectedMap;
        public int Hero;
        public int Team;
        /// <summary>Команда зрителя (0 = синие, 1 = красные). Нужна, чтобы у каждой стороны свои в слотах 0,1,2, противники в 3,4,5.</summary>
        public int ViewerTeam;
        public List<mp_thestealdev> Players;
        public long MatchId;

        public override void Encode()
        {
            Stream.WriteBoolean(true);
            ByteStreamHelper.EncodeLogicLong(Stream, MatchId);
            Stream.WriteDataReference(15, SelectedMap);

            if (Players == null)
                Players = new List<mp_thestealdev>();

            // Порядок как от RankedMatch: свои первые 3, чужие следующие 3. Не чередуем — иначе
            // сломается связь слот ↔ Next в пике/бане (клиент по индексу 0..5 подсвечивает игрока в списке).
            var ordered = Players.ToList();

            Stream.WriteVInt(ordered.Count);
            // У каждого зрителя: первые 3 в списке = своя команда (синяя сторона 0,1,2), последние 3 = противники (красная 3,4,5).
            for (int i = 0; i < ordered.Count; i++)
            {
                Stream.WriteVInt(i < 3 ? 0 : 1);
            }

            Stream.WriteVInt(Modificator);
            Stream.WriteVInt(ordered.Count);

            if (Stub)
            {
                Stream.WriteVInt(1);
                {
                    Stream.WriteLogicLong(0, 1);
                    Stream.WriteBoolean(true);
                    Stream.WriteString("Shelly");
                    Stream.WriteVInt(5000);
                    Stream.WriteVInt(GlobalId.CreateGlobalId(28, 0));
                    Stream.WriteVInt(GlobalId.CreateGlobalId(43, 0));
                    Stream.WriteVInt(-1);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(1);
                    Stream.WriteVInt(1);
                    ByteStreamHelper.WriteDataReference(Stream, 0);

                    Stream.WriteVInt(0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteBoolean(true);
                    Stream.WriteVInt(999);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteVInt(1);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteVInt(0);
                }
            }
            else
            {
                Stream.WriteVInt(ordered.Count);
                // Истинные синие (ViewerTeam==0): свои уже в 0,1,2, враги в 3,4,5 — пишем реальные слоты.
                // Истинные красные (ViewerTeam==1): свои в списке первые 3, но у них реальные слоты 3,4,5 — подменяем на 0,1,2; враги (последние 3) реальные 0,1,2 — подменяем на 3,4,5.
                for (int i = 0; i < ordered.Count; i++)
                {
                    int? displaySlot = ViewerTeam == 1 ? i : null;
                    ordered[i].Encode(Stream, true, displaySlot);
                }
            }

            // Команда зрителя для UI: всегда 0 (своя сторона = синяя).
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 22150;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}