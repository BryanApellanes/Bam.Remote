using System.Collections.Generic;
using System.Linq;

namespace Bam.Remote.Deployment
{
    public class DeploymentSet
    {
        public DeploymentSet()
        {
            FileSets = new ManagedFileSet[]{};
        }
        public string RemoteCommand { get; set; }
        public ManagedFileSet[] FileSets { get; private set; }

        public DeploymentSet AddFileSet(ManagedFileSet managedFileSet)
        {
            FileSets = new HashSet<ManagedFileSet>(FileSets).ToArray();
            return this;
        }
    }
}