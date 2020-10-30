using Bam.Net;
using Bam.Remote.Deployment.Data;

namespace Bam.Remote.Deployment
{
    public interface IDeterministicSshHostPasswordGenerator
    {
        ManagedPassword Generate(SshHostIdentifier sshHostIdentifier, double? julianDate = null);
    }
}