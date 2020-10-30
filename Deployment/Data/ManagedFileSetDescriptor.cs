namespace Bam.Remote.Deployment.Data
{
    public class ManagedFileSetDescriptor
    {
        public virtual SshHostIdentifier[] SshHosts { get; set; }
        
        /// <summary>
        /// A comma or semi-colon separated list of directory paths.
        /// </summary>
        public string Directories { get; set; }
        
        /// <summary>
        /// A comma or semi-colon separated list of file paths
        /// </summary>
        public string Files { get; set; }
    }
}