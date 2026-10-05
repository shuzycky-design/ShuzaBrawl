namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class ReportPlayerMessage : GameMessage
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public int Id2 { get; set; }
        public int Id3 { get; set; }
        public string Name { get; set; }

public override void Decode()
{
    Console.WriteLine(Stream.ReadVInt());
    Console.WriteLine(Stream.ReadVInt());
    Console.WriteLine(Stream.ReadVInt());

    Count = Stream.ReadVInt();
    Console.WriteLine($"Count: {Count} (количество символов в нике)");

    int zeroCount = 0;
    int value;

    while (true)
    {
        value = Stream.ReadVInt();
        Console.WriteLine($"Проверка: {value}");

        if (value == 0)
        {
            zeroCount++;
            if (zeroCount == 5)
            {
                Console.WriteLine("Найдено 5 нулей подряд!");
                break;
            }
        }
        else
        {
            zeroCount = 0;
        }
    }

    while (true)
    {
        value = Stream.ReadVInt();
        Console.WriteLine(value);
        if (value > 0)
        {
            Id = value;
            Console.WriteLine(Id);
            break;
        }
    }
}

        public override int GetMessageType()
        {
            return 10511;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
