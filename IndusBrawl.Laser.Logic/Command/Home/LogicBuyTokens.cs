namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Logic.Avatar;

    public class LogicBuyTokens : Command
    {
        public override void Decode(ByteStream stream)
        {
            // Игнорируем все данные из потока
            base.Decode(stream);

            // Просто читаем 4 VInt, чтобы не было ошибок при декодировании
            //Console.WriteLine(stream.ReadVInt()); // Неизвестное значение 1
            //Console.WriteLine(stream.ReadVInt()); // Неизвестное значение 2
            //Console.WriteLine(stream.ReadVInt()); // Неизвестное значение 3
            //Console.WriteLine(stream.ReadVInt()); // Неизвестное значение 4
            //Console.WriteLine(stream.ReadVInt());
        }

        public override int Execute(HomeMode homeMode)
        {
            var player = homeMode.Avatar as ClientAvatar;

            if (player == null)
            {
                Debugger.Error("LogicBuyTokens: Player is null!");
                return -1;
            }

            const int DIAMONDS_REQUIRED = 40;
            const int TOKENS_TO_ADD = 1000;

            // Проверяем, хватает ли алмазов
            if (!player.UseDiamonds(DIAMONDS_REQUIRED))
            {
                Debugger.Warning("LogicBuyTokens: Not enough diamonds!");
                return -2;
            }

            // Добавляем токены
            homeMode.Home.TokenDoublers += TOKENS_TO_ADD;

            Debugger.Print($"LogicBuyTokens: {TOKENS_TO_ADD} tokens added. {DIAMONDS_REQUIRED} diamonds deducted.");

            return 0;
        }

        public override int GetCommandType()
        {
            return 509; // Тип команды — 509
        }
    }
}