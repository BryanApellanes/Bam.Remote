using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using Bam;
using Bam.Data.Repositories;

namespace Bam.Remote.Deployment.Data
{
    public class SshHostIdentifier : CompositeKeyAuditRepoData
    {
        [CompositeKey]
        public string HostName { get; set; }
        
        [CompositeKey]
        public int Port { get; set; }
        
        [CompositeKey]
        public string MacAddress { get; set; }
        
        /// <summary>
        /// A comma delimited list of ip addresses for this host.
        /// </summary>
        public string HostAddresses { get; set; }
        
        public virtual List<SshHostCredentials> SshHostCredentials { get; set; }
/*
        private static SshHostIdentifier _current;
        private static readonly object _currentLock = new object();
        public static SshHostIdentifier Current
        {
            get
            {
                return _currentLock.DoubleCheckLock(ref _current, () =>
                    new SshHostIdentifier
                    {
                        HostName = Bam.CoreServices.ApplicationRegistration.Data.Machine.Current.Name,
                        Port = 22,
                        MacAddress = Bam.CoreServices.ApplicationRegistration.Data.Machine.Current.GetFirstMac(),
                        HostAddresses = string.Join(", ", Bam.CoreServices.ApplicationRegistration.Data.Machine.Current.HostAddresses.Select(ha=> ha.IpAddress).ToArray())
                    });
            }
        }*/
    }
}