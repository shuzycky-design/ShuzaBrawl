using IndusBrawl.Laser.Server.Networking.Security;
using IndusBrawl.Laser.Titan.Library.Blake;

namespace IndusBrawl.Laser.Server.Networking
{
    using IndusBrawl.Laser.Logic.Message;
    using IndusBrawl.Laser.Server.Message;
    using IndusBrawl.Laser.Titan.Cryptography;
    using IndusBrawl.Laser.Titan.Library;
    using IndusBrawl.Laser.Logic.Message.Home;
    using IndusBrawl.Laser.Titan.Math;
    using System.Linq;
    using IndusBrawl.Laser.Titan.Debug;
    using IndusBrawl.Laser.Logic.Message.Account.Auth;
    using IndusBrawl.Laser.Server.Settings;
    using System.Text;
    
    public class Messaging
    {
        public byte[] SessionToken { get; }

        private Connection Connection;
        private int PepperState;

        private StreamEncrypter Encrypter;
        private StreamEncrypter Decrypter;

        private MessageFactory MessageFactory;
        
        // Android ключи (из первого файла)
        private static readonly byte[] Ssk = Convert.FromHexString(
            "7fba4ad5a0dac24719fd540fb96a42b0f72061bee3daa7dc16bbd5fc46c027f8");
        private static readonly byte[] AndroidPublicKey = TweetNaCl.CryptoScalarmultBase(Ssk);
        
        // iOS ключи (из второго файла) - FIXED!
        private byte[] iOSPrivateKey = { 158, 217, 110, 5, 87, 249, 222, 234, 204, 121, 177, 228, 59, 79, 93, 217, 25, 33, 113, 185, 119, 171, 205, 246, 11, 185, 185, 22, 140, 152, 107, 20 };
        private byte[] iOSPublicKey;
        
        private byte[] s;
        private bool isiOSClient = false;

        private byte[] RNonce;
        private byte[] SNonce;
        
        // Android секретный ключ (из первого файла)
        private byte[] AndroidSecretKey = { 88, 110, 35, 233, 167, 161, 39, 53, 21, 167, 241, 17, 254, 59, 196, 55, 86, 128, 178, 125, 149, 200, 141, 115, 0, 246, 109, 195, 220, 34, 1, 241 };
        
        // iOS случайный ключ
        private byte[] iOSSecretKey = new byte[32];

        public int Seed { get; set; }
        public bool DisableCrypto;

        public Messaging(Connection connection)
        {
            Connection = connection;
            MessageFactory = MessageFactory.Instance;

            PepperState = 2;

            SessionToken = new byte[24];
            SNonce = new byte[24];
            
            TweetNaCl.RandomBytes(SessionToken);
            TweetNaCl.RandomBytes(SNonce);
            
            // Генерируем iOS публичный ключ
            iOSPublicKey = TweetNaCl.CryptoScalarmultBase(iOSPrivateKey);
            
            // Для iOS используем случайный секретный ключ
            TweetNaCl.RandomBytes(iOSSecretKey);

            DisableCrypto = false;
        }

        public void Send(GameMessage message)
        {
            if (message.GetMessageType() == 20100)
            {
                EncryptAndWrite(message);
            }
            else
            {
                Processor.Send(Connection, message);
            }
        }

        public void EncryptAndWrite(GameMessage message)
        {
            if (message.GetEncodingLength() == 0) message.Encode();

            byte[] payload = new byte[message.GetEncodingLength()];
            Buffer.BlockCopy(message.GetMessageBytes(), 0, payload, 0, payload.Length);

            int messageType = message.GetMessageType();
            int version = message.GetVersion();

            if (!DisableCrypto)
                switch (PepperState)
                {
                    case 4:
                        payload = SendPepperLoginResponse(payload);
                        break;
                    case 5:
                        byte[] encrypted = new byte[payload.Length + Encrypter.GetEncryptionOverhead()];
                        Encrypter.Encrypt(payload, encrypted, payload.Length);
                        payload = encrypted;
                        break;
                }

            byte[] stream = new byte[payload.Length + 7];

            int length = payload.Length;

            stream[0] = (byte)(messageType >> 8);
            stream[1] = (byte)(messageType);
            stream[2] = (byte)(length >> 16);
            stream[3] = (byte)(length >> 8);
            stream[4] = (byte)(length);
            stream[5] = (byte)(version >> 8);
            stream[6] = (byte)(version);

            Buffer.BlockCopy(payload, 0, stream, 7, payload.Length);
            Connection.Write(stream);
        }

        public int OnReceive()
        {
            long position = Connection.Memory.Position;
            Connection.Memory.Position = 0;

            byte[] headerBuffer = new byte[7];
            Connection.Memory.Read(headerBuffer, 0, 7);

            int type = headerBuffer[0] << 8 | headerBuffer[1];
            int length = headerBuffer[2] << 16 | headerBuffer[3] << 8 | headerBuffer[4];
            int version = headerBuffer[5] << 8 | headerBuffer[6];

            byte[] payload = new byte[length];
            if (Connection.Memory.Read(payload, 0, length) < length)
            {
                Connection.Memory.Position = position;
                return 0;
            }

            if (this.ReadNewMessage(type, length, version, payload) != 0)
            {
                return -1;
            }

            byte[] all = Connection.Memory.ToArray();
            byte[] buffer = all.Skip(length + 7).ToArray();

            Connection.Memory = new MemoryStream();
            Connection.Memory.Write(buffer, 0, buffer.Length);

            if (buffer.Length >= 7)
            {
                OnReceive();
            }
            return 0;
        }

        private int ReadNewMessage(int type, int length, int version, byte[] payload)
        {
            if (PepperState == 2 && type == 10101) DisableCrypto = true;
            
            if (!DisableCrypto)
                switch (PepperState)
                {
                    case 2:
                        if (type == 10100) PepperState = 3;
                        else
                        {
                            Connection.Send(new AuthenticationFailedMessage()
                            {
                                ErrorCode = 8,
                                UpdateUrl = "https://www.bilibili.com/opus/777054674628902913"
                            });
                            return -1;
                        }
                        break;
                    case 3:
                        if (type != 10101) return -1;
                        payload = HandlePepperLogin(payload);
                        if (payload == null) return -1;
                        break;
                    case 5:
                        byte[] decrypted = new byte[length - Decrypter.GetEncryptionOverhead()];
                        int result = Decrypter.Decrypt(payload, decrypted, length);
                        payload = decrypted;
                        if (result != 0) return -1;
                        break;
                }

            GameMessage message = MessageFactory.CreateMessageByType(type);
            if (message != null)
            {
                message.GetByteStream().SetByteArray(payload, payload.Length);
                message.Decode();
                if (message.GetMessageType() == 10100)
                {
                    Connection.MessageManager.ReceiveMessage(message);
                }
                else
                {
                    Processor.Receive(Connection, message);
                }
                
                if(Configuration.Instance.MsgLogger && type != 10108 && type != 14102 && type != 10110) 
                    Logger.Print("Received Message type " + type);
            }
            else
            {
                // Сообщения, которые сервер не умеет обрабатывать, пишем в лог всегда (с содержимым, до 64 байт):
                // по ним видно, какие кнопки в игре ничего не делают
                Logger.Print($"Ignoring message of unknown type {type}, length {payload?.Length ?? 0}, data {(payload == null ? "" : BitConverter.ToString(payload, 0, Math.Min(payload.Length, 64)))}");
            }

            return 0;
        }

        private byte[] client_pk;

        private byte[] HandlePepperLogin(byte[] payload)
        {
            try
            {
                if (payload.Length < 32)
                {
                    Console.WriteLine("Payload too short for 10101");
                    return null;
                }

                client_pk = payload.Take(32).ToArray();
                Blake2BHasher hasher = new Blake2BHasher();
                byte[] nonce;
                
                // Пробуем сначала Android метод
                try
                {
                    hasher.Update(client_pk);
                    hasher.Update(AndroidPublicKey);
                    nonce = hasher.Finish();
                    
                    s = TweetNaCl.CryptoBoxBeforenm(client_pk, Ssk);
                    
                    byte[] decrypted = TweetNaCl.CryptoBoxOpenAfternm(payload.Skip(32).ToArray(), nonce, s);
                    
                    isiOSClient = false;
                    if (Configuration.Instance.MsgLogger)
                        Logger.Print("Android client detected");
                    
                    PepperState = 4;
                    RNonce = decrypted.Skip(24).Take(24).ToArray();

                    return decrypted.Skip(48).ToArray();
                }
                catch (TweetNaCl.InvalidCipherTextException)
                {
                    // Если Android не сработал, пробуем iOS
                    try
                    {
                        hasher = new Blake2BHasher(); // Сбрасываем хешер
                        hasher.Update(client_pk);
                        hasher.Update(iOSPublicKey);
                        nonce = hasher.Finish();
                        
                        s = TweetNaCl.CryptoBoxBeforenm(client_pk, iOSPrivateKey);
                        
                        byte[] decrypted = TweetNaCl.CryptoBoxOpenAfternm(payload.Skip(32).ToArray(), nonce, s);
                        
                        isiOSClient = true;
                        if (Configuration.Instance.MsgLogger)
                            Logger.Print("iOS client detected");
                        
                        PepperState = 4;
                        RNonce = decrypted.Skip(24).Take(24).ToArray();

                        return decrypted.Skip(48).ToArray();
                    }
                    catch (TweetNaCl.InvalidCipherTextException ex2)
                    {
                        Console.WriteLine($"Failed to decrypt 10101 (both Android and iOS): {ex2.Message}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HandlePepperLogin: {ex.Message}");
                return null;
            }
        }

        private byte[] SendPepperLoginResponse(byte[] payload)
        {
            byte[] packet = new byte[payload.Length + 32 + 24];

            Buffer.BlockCopy(SNonce, 0, packet, 0, 24);
            
            if (isiOSClient)
            {
                // iOS: используем случайный секретный ключ
                Buffer.BlockCopy(iOSSecretKey, 0, packet, 24, 32);
            }
            else
            {
                // Android: используем фиксированный секретный ключ
                Buffer.BlockCopy(AndroidSecretKey, 0, packet, 24, 32);
            }
            
            Buffer.BlockCopy(payload, 0, packet, 24 + 32, payload.Length);

            Blake2BHasher hasher = new Blake2BHasher();
            hasher.Update(RNonce);
            hasher.Update(client_pk);
            byte[] nonce;
            
            if (isiOSClient)
            {
                hasher.Update(iOSPublicKey);
                nonce = hasher.Finish();
            }
            else
            {
                hasher.Update(AndroidPublicKey);
                nonce = hasher.Finish();
            }
            
            byte[] encrypted = TweetNaCl.CryptoBoxAfternm(packet, nonce, s);

            PepperState = 5;

            // Создаем энкриптеры с соответствующими ключами
            if (isiOSClient)
            {
                Decrypter = new PepperEncrypter(iOSSecretKey, RNonce);
                Encrypter = new PepperEncrypter(iOSSecretKey, SNonce);
            }
            else
            {
                Decrypter = new PepperEncrypter(AndroidSecretKey, RNonce);
                Encrypter = new PepperEncrypter(AndroidSecretKey, SNonce);
            }

            return encrypted;
        }
    }
}