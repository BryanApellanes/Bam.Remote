namespace Bam.Remote.Etc
{
    public class EtcGroup
    {
        internal IEtcCredentialManager EtcCredentialManager { get; set; }
        public string GroupName { get; set; }
        public uint GroupId { get; set; }

        public static EtcGroup FromGroupEntry(GroupEntry groupEntry)
        {
            return new EtcGroup
            {
                GroupName = groupEntry.GroupName,
                GroupId = groupEntry.GroupId
            };
        }
    }
}