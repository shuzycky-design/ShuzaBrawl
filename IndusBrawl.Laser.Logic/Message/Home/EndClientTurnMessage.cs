namespace IndusBrawl.Laser.Logic.Message.Home
{
    using IndusBrawl.Laser.Logic.Command;

    public class EndClientTurnMessage : GameMessage
    {
        public int Tick;
        public int Checksum;
        public int type;

        public List<Command> Commands;

        public EndClientTurnMessage() : base()
        {
            Commands = new List<Command>();
        }

        public int UnhandledType;
        public int CommandsAfterUnhandled;   // сколько команд шло в ходе после неразобранной

        public override void Decode()
        {
            Stream.ReadBoolean();
            Tick = Stream.ReadVInt();
            Checksum = Stream.ReadVInt();

            int count = Stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                type = Stream.ReadVInt();
                Command command = CommandManager.DecodeCommand(Stream, type);
                if (command == null)
                {
                    // Клиент возвращает в ходе выполненные серверные команды (203, 228...), сервер их не разбирает
                    // и на первой такой бросает остаток хода. Запоминаем её тип.
                    UnhandledType = type;
                    CommandsAfterUnhandled = count - i - 1;
                    return;
                }

                Commands.Add(command);
            }
        }

        public override int GetMessageType()
        {
            return 14102;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
