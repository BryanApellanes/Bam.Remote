using Bam.Data.Repositories;

namespace Bam.Remote.Deployment.Data
{
    public class SshHostCredentials : CompositeKeyAuditRepoData
    {
        public virtual SshHostIdentifier SshHostIdentifier { get; set; }
        
        [CompositeKey]
        public string UserName { get; set; }
        
        public string SharedSecret { get; set; }
        
        [CompositeKey]
        public double JulianDate { get; set; }
    }
}