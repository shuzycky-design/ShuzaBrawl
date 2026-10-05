using System;
using IndusBrawl.Laser.Logic.Listener;
using IndusBrawl.Laser.Logic.Message;

namespace IndusBrawl.Laser.Server.Networking.UDP.Game
{
    public class UDPGameListener : LogicGameListener
    {
        private UDPSocket Socket;
        private Connection TCPConnection;
        public bool IsSpectator { get; set; }

        public UDPGameListener(UDPSocket socket, Connection connection)
        {
            Socket = socket;
            TCPConnection = connection;
            
            Console.WriteLine($"[UDPGameListener] Конструктор: Socket={socket != null}, Connection={connection != null}");
            
            if (socket != null)
            {
                IsSpectator = socket.IsSpectator;
                Console.WriteLine($"[UDPGameListener] IsSpectator={IsSpectator}, SessionId={socket.SessionId}");
            }
        }

        public override void SendMessage(GameMessage message)
        {
            try
            {
                if (Socket == null)
                {
                    Console.WriteLine($"[UDPGameListener] Ошибка: Socket is null");
                    return;
                }
                
                if (IsSpectator)
                {
                    string messageType = message.GetType().Name;
                    int messageId = message.GetMessageType();
                    
                    Console.WriteLine($"[SPECTATE ALL] Отправка: {messageType} (ID: {messageId})");
                }
                
                Socket.SendMessage(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDPGameListener] Ошибка: {ex.Message}");
            }
        }

        public override void SendTCPMessage(GameMessage message)
        {
            try
            {
                if (TCPConnection == null)
                {
                    Console.WriteLine($"[UDPGameListener] Ошибка: TCPConnection is null");
                    return;
                }
                
                TCPConnection.Send(message);
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"[UDPGameListener] Ошибка: {ex.Message}");
            }
        }
    }
}