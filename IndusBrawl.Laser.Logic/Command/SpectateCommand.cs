using IndusBrawl.Laser.Logic.Home;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic.Command
{
    public class SpectateCommand : Command
    {
        public long TargetAccountId { get; set; }
        
        public override int GetCommandType()
        {
            return 600; // Уникальный ID для команды наблюдения
        }
        
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            TargetAccountId = stream.ReadLong();
        }
        
        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteLong(TargetAccountId);
        }
        
        public override int Execute(HomeMode homeMode)
        {
            // Возвращаем 0 при успехе, -1 при ошибке
            // Реальная обработка будет на сервере
            return 0; // Успех
        }
    }
}
