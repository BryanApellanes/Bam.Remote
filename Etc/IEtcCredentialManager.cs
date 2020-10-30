namespace Bam.Remote.Etc
{
    public interface IEtcCredentialManager
    {
        EtcUser AddUser(string userName, string password);
        EtcUser SetPassword(string userName, string password);
        EtcGroup AddGroup(string groupName, params string[] members);
        EtcGroup AddGroupMember(string groupName, string member);
        EtcGroup AddGroupMembers(string groupName, params string[] members);
        bool UserExists(string userName);
        bool GroupExists(string groupName);
    }
}