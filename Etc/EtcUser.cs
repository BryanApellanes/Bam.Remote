using Bam.Net;

namespace Bam.Remote.Etc
{
    public class EtcUser
    {
        internal IEtcCredentialManager EtcCredentialManager { get; set; }
        public string UserName { get; set; }
        public IManagedPassword Password { get; set; }
        public EtcGroup[] Groups { get; set; }
    }
}