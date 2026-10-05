namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Battle.Structures;
    using IndusBrawl.Laser.Logic.Data;
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Home.Gatcha;
    using IndusBrawl.Laser.Titan.DataStream;
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public class LogicRefreshRandomRewardsCommand : Command
    {
        public bool guaranteed;
        public int Количество = 0;
        public int Rarity = 0;
        public bool Disable;
        public StarrDropLabConfig Lab;   // подбор деталей (см. StarrDropLab)
        public bool CleanEmpty;   // правильное пустое состояние (0 контейнеров, 0 наград)

        public override void Encode(ByteStream stream)
        {
            
            stream.WriteVInt(1);
            stream.WriteVInt(-1);
            stream.WriteVInt(-1);
            stream.WriteVInt(0);
            stream.WriteVInt(1);
            stream.WriteVInt(1);


                stream.WriteVInt(5);

                stream.WriteDataReference(80, 1);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
                stream.WriteDataReference(80, 2);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
                stream.WriteDataReference(80, 3);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
                stream.WriteDataReference(80, 4);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
                stream.WriteDataReference(80, 5);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);

            if (CleanEmpty)
            {
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteInt(0);
                stream.WriteVInt(-1);
                stream.WriteVInt(0);
                stream.WriteVInt(11111111);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                base.Encode(stream);
                return;
            }

            if (!Disable)
            {
                stream.WriteVInt(Количество);
                if (Lab != null) StarrDropLabConfig.WriteContainer(stream, Lab.PendingContainer, Rarity);
                else stream.WriteDataReference(80, Rarity);
            } else
            {
                stream.WriteBoolean(false);
                stream.WriteBoolean(false);
            }

                stream.WriteVInt(1);
                stream.WriteByte(1);
                stream.WriteVInt(5);
                stream.WriteVInt(1);
                stream.WriteDataReference(0);
                stream.WriteVInt(613);

                stream.WriteInt(0);
                stream.WriteVInt(-1); // Battle Progression Step
                stream.WriteVInt(0);

                stream.WriteVInt(11111111); // timer until refresh

                stream.WriteVInt(0);

                stream.WriteVInt(1);
                stream.WriteVInt(Lab == null ? 0 : (Lab.PendingC == -1 ? Rarity : Lab.PendingC));

            
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {

            Rarity = homeMode.Home.StarrDrop.Rarity;
            return 0;
        }

        public override int GetCommandType()
        {
            return 228;
        }
    }
}
