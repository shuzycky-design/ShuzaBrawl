namespace IndusBrawl.Laser.Logic.Command.Home
{
    using IndusBrawl.Laser.Logic.Home;
    using IndusBrawl.Laser.Logic.Notification;
    using IndusBrawl.Laser.Titan.DataStream;

    public class LogicAddNotificationCommand : Command
    {
        public BaseNotification Notification;

        public override void Encode(ByteStream stream)
        {
            if (stream.WriteBoolean(Notification != null))
            {
                stream.WriteVInt(Notification.GetNotificationType());
                Notification.Encode(stream);
            }

            stream.WriteVInt(0);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 206;
        }
    }
}
