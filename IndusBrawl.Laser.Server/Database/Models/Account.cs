namespace IndusBrawl.Laser.Server.Database.Models
{
    using Newtonsoft.Json;
    using IndusBrawl.Laser.Logic.Avatar;
    using IndusBrawl.Laser.Logic.Home;

    public class Account
    {
        [JsonProperty] public long AccountId;
        [JsonProperty] public string PassToken;

        [JsonProperty] public ClientHome Home;
        [JsonProperty] public ClientAvatar Avatar;

        public Account()
        {
            Home = new ClientHome();
            Avatar = new ClientAvatar();
        }
    }
}
