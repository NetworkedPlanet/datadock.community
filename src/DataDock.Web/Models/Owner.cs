using Newtonsoft.Json;

namespace DataDock.Web.Models
{
    public class Owner
    {
        [JsonProperty("ownerId")]
        public string OwnerId { get; set; }
        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; }
    }
}
