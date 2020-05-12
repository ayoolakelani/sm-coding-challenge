using System.Runtime.Serialization;

namespace sm_coding_challenge.Models
{
    [DataContract]
    public class PlayerModel
    {
        [DataMember(Name = "player_id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "position")]
        public string Position { get; set; }

        [DataMember(Name = "entry_id")]
        public string EntryId { get; set; }

        [DataMember(Name = "yds")]
        public string Yds { get; set; }

        [DataMember(Name = "att")]
        public string Att { get; set; }

        [DataMember(Name = "tds")]
        public string Tds { get; set; }

        [DataMember(Name = "fum")]
        public string Fum { get; set; }

    }
}

