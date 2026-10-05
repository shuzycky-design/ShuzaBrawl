using System.IO;
using IndusBrawl.Laser.Logic.Notification;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic.Message.Home
{
    public class NotificationMessage : GameMessage
    {
        public BaseNotification Notification;

        public override int GetMessageType()
        {
            return 20801;
        }
        public override void Encode()
        {
            Stream.WriteVInt(Notification.GetNotificationType());
            Notification.Encode(Stream);
        }
        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
