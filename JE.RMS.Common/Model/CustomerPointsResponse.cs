using Newtonsoft.Json;

namespace JE.RMS.Common.Model
{
    public class CustomerPointsResponse
    {
        public string ChannelCode { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MasterID { get; set; }
        public string SourceSystemID { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemUniqueID { get; set; }
        public string SourceSystemUniqueIDType { get; set; }
        public double Points { get; set; }
        [JsonIgnore]
        public string UniqueID { get; set; }
    }
}
