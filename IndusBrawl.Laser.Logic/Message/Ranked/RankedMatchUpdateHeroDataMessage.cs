namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchUpdateHeroDataMessage : GameMessage
    {
        public string Name;
        public override void Decode()
        {
            /*
             *   WriteVint(a1 + 8, *(_DWORD *)(a1 + 144));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 152));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 160));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 168));
  WriteVint(a1 + 8, *(_DWORD *)(a1 + 192));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 176));
  WriteVint(a1 + 8, *(_DWORD *)(a1 + 196));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 184));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 200));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 208));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 216));
  return ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 224));
            */
        }

        public override int GetMessageType()
        {
            return 12157;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
