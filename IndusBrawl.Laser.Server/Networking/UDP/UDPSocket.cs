namespace IndusBrawl.Laser.Server.Networking
{
    using IndusBrawl.Laser.Logic.Battle;
    using IndusBrawl.Laser.Logic.Message;
    using IndusBrawl.Laser.Logic.Message.Battle;
    using IndusBrawl.Laser.Titan.DataStream;
    using IndusBrawl.Laser.Server.Message;
    using System.Net;
    using System.Linq;
    using System.Threading.Tasks;
    using IndusBrawl.Laser.Logic.Battle.Input;

    public class UDPSocket
    {
        public readonly long SessionId;
        private EndPoint EndPoint;

        public BattleMode Battle;
        public bool IsConnected => EndPoint != null;

        public Connection TCPConnection;
        public bool IsSpectator;

        public UDPSocket(long sessionId)
        {
            SessionId = sessionId;
        }

        public void SetEndPoint(EndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public void SendMessage(GameMessage message)
        {
            if (message.GetEncodingLength() == 0) message.Encode();

            ByteStream stream = new ByteStream(10);
            stream.WriteLong(SessionId);
            stream.WriteShort(0);
            stream.WriteVInt(message.GetMessageType());
            stream.WriteVInt(message.GetEncodingLength());
            stream.WriteBytesWithoutLength(message.GetMessageBytes(), message.GetEncodingLength());

            UDPGateway.SendTo(stream.GetByteArray(), 0, stream.GetOffset(), EndPoint);
        }

        private static readonly HashSet<int> _unknownTypes = new HashSet<int>();

        public void ProcessReceive(ByteStream stream)
        {
            // async!
            Task.Run(() =>
            {
                int type = stream.ReadVInt();
                int length = stream.ReadVInt();
                byte[] data = stream.ReadBytes(length, 2000);

                GameMessage message = MessageFactory.Instance.CreateMessageByType(type);
                if (message != null)
                {
                    message.GetByteStream().SetByteArray(data, length);
                    message.Decode();
                    HandleMessage(message);
                }
                else
                {
                    lock (_unknownTypes)
                    {
                        if (_unknownTypes.Add(type))
                            Console.WriteLine($"[UDP] Ignoring message of unknown type {type}, length {length}, data {BitConverter.ToString(data, 0, Math.Min(data.Length, 48))}");
                    }
                }
            });
        }

        private void HandleMessage(GameMessage message)
        {
            if (message.GetMessageType() == 10555)
            {
                TCPConnection.MessageManager.LastKeepAlive= DateTime.UtcNow;
                ClientInputMessage clientInputMessage = (ClientInputMessage)message;
                while (clientInputMessage.Inputs.TryDequeue(out ClientInput clientInput))
                {
                    if (!IsSpectator)
                        Battle.AddClientInput(clientInput, SessionId);
                    else
                        Battle.HandleSpectatorInput(clientInput, SessionId);
                }
            }
        }
    }
}
