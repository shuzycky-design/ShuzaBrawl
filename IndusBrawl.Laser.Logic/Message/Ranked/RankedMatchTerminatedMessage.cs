namespace IndusBrawl.Laser.Logic.Message.Ranked
{
    public class RankedMatchTerminatedMessage : GameMessage
    {
        public string Name;
        public int Reason;
        public override void Encode()
        {
            Stream.WriteVInt(Reason); // reason
            /*
             * "TID_RANKED_MATCH_TERMINATED_REASON_1","Матч прерван: игрок <NAME> отключился."
"TID_RANKED_MATCH_TERMINATED_REASON_2","Матч прерван: игрок <NAME> не отвечает."
"TID_RANKED_MATCH_TERMINATED_REASON_3","Матч прерван: сезон закончился."
"TID_RANKED_MATCH_TERMINATED_REASON_4","Матч прерван. <CODE> Попробуй ещё раз."
"TID_RANKED_MATCH_TERMINATED_REASON_5","Матч остановлен. Матч занимает слишком много времени."
"TID_RANKED_MATCH_TERMINATED_REASON_6","Матч остановлен. Бой занимает слишком много времени."
            */
            Stream.WriteVInt(1);
            Stream.WriteString(Name ?? string.Empty);
        }

        public override int GetMessageType()
        {
            return 0x568F;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
