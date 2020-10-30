using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bam.Net;
using Bam.Net.Automation;
using Bam.Net.Logging;
using Bam.Remote.Deployment.Data;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace Bam.Remote.Deployment
{
    public class DeploymentManager
    {
        private DeterministicSshHostPasswordGenerator _deterministicSshHostPasswordGenerator;

        public DeploymentManager(RemoteSshHostInfo hostInfo, ILogger logger = null)
        {
            _deterministicSshHostPasswordGenerator = new DeterministicSshHostPasswordGenerator();
            Logger = logger ?? Log.Default;
            HostInfo = hostInfo;
        }

        public ILogger Logger { get; set; }
        
        public RemoteSshHostInfo HostInfo { get; set; }

        public void CopyFile(string filePath)
        {
            if (HostInfo == null)
            {
                throw new ArgumentNullException(nameof(HostInfo));
            }
            Copy(HostInfo, ManagedFileSet.FromFile(filePath));
        }

        public void CopyDirectory(string directoryPath, string remoteDirectory = null)
        {
            if (HostInfo == null)
            {
                throw new ArgumentNullException(nameof(HostInfo));
            }
            Copy(HostInfo, ManagedFileSet.FromDirectory(directoryPath, remoteDirectory));
        }
        
        public void Copy(string host, ManagedFileSetDescriptor managedFileSetDescriptor)
        {
            Copy(new RemoteSshHostInfo(), ManagedFileSet.FromDescriptor(managedFileSetDescriptor));
        }
        
        public void Copy(string host, int port, string userName, string password, ManagedFileSet managedFileSet, string remoteFolder = null, bool overwrite = true)
        {
            Copy(new RemoteSshHostInfo
                {
                    HostName = host,
                    Port = port,
                    UserName = userName,
                    ManagedPassword = new ManagedPassword(password)
                }, managedFileSet, remoteFolder, overwrite
            );
        }
        
        public void Copy(RemoteSshHostInfo hostInfo, ManagedFileSet managedFileSet, string remoteFolder = null, bool overwrite = true)
        {
            RemoteSshHost remoteSshHost = hostInfo.ToRemoteSshHost();
            remoteSshHost.Copy(managedFileSet, remoteFolder, overwrite);
        }

        public void EnsureDirectory(RemoteSshHostInfo hostInfo, string path)
        {
            RemoteSshHost.EnsureDirectory(hostInfo.ToRemoteSshHost().GetSftpClient(), path);
        }

        public void Deploy(string localDirectoryPath, string remoteCommand)
        {
            Deploy(HostInfo.ToRemoteSshHost(), localDirectoryPath, remoteCommand);
        }
        
        public void Deploy(RemoteSshHost remoteSshHost, string localDirectoryPath, string remoteCommand, string remoteDirectoryPath = null)
        {
            Deploy
            (
                remoteSshHost, new DeploymentSet { RemoteCommand = remoteCommand }
                    .AddFileSet(ManagedFileSet.FromDirectory(localDirectoryPath, remoteDirectoryPath))
            );
        }

        public void Deploy(RemoteSshHost remoteSshHost, DeploymentSet deploymentSet)
        {
            // copy all the filesets to the remote
            Parallel.ForEach(deploymentSet.FileSets, (managedFileSet) =>
            {
                remoteSshHost.Copy(managedFileSet, managedFileSet.RemoteDirectory, true);
            });
            // execute the the RemoteExecutable with nohup
            string command = $"nohup {deploymentSet.RemoteCommand} > /dev/null 2>&1 &;";
            remoteSshHost.Execute(command);
        }
    }
}