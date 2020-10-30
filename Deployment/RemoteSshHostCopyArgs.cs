using System;

namespace Bam.Remote.Deployment
{
    public class RemoteSshHostCopyArgs: EventArgs
    {
        public Exception Exception { get; set; }
        public ManagedFileSet ManagedFileSet { get; set; }
        public ManagedFile ManagedFile { get; set; }
        public bool WillRetry { get; set; }
    }
}