using Microsoft.VisualBasic;
using IndusBrawl.Laser.Logic.Helper;
using IndusBrawl.Laser.Titan.DataStream;

namespace IndusBrawl.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;
        public int ClientMajor;
        public int ClientMinor;
        public int ClientBuild;
        public string ResourceSha;
        public string Version;
        private string? Device;
        private int DeviceIMEI;
        private bool IsAdvertisingEnabled;
        private bool IsAndroid;
        private string? OSVersion;
        private string? PreferredDeviceLanguage;
        private int PreferredLanguage;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();
            ClientMajor = Stream.ReadInt();
            ClientMinor = Stream.ReadInt();
            ClientBuild = Stream.ReadInt();
            ResourceSha = Stream.ReadString();
            Device = Stream.ReadString(1024);
            PreferredLanguage = ByteStreamHelper.ReadDataReference(Stream);
            PreferredDeviceLanguage = Stream.ReadString(1024);
            OSVersion = Stream.ReadString(1024);
            IsAndroid = Stream.ReadBoolean();
            Stream.ReadStringReference(1024);
            Stream.ReadStringReference(1024);
            IsAdvertisingEnabled = Stream.ReadBoolean();
            Stream.ReadString(1024);
            DeviceIMEI = Stream.ReadInt();
            Stream.ReadVInt();
            Version = Stream.ReadStringReference(1024); // Client Version( why? )
        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
