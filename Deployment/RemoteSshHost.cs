using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bam.Net;
using Bam.Net.CoreServices.ApplicationRegistration.Data;
using Bam.Net.Logging;
using Bam.Net.Testing;
using Bam.Remote.Etc;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace Bam.Remote.Deployment
{
    public class RemoteSshHost : Loggable, IRemoteSshHost
    {
        public RemoteSshHost(ILogger logger = null)
        {
            Logger = logger ?? Log.Default;
        }
        
        public string HostName { get; set; }
        public int Port { get; set; }
        public string LoginUserName { get; set; }
        public string LoginPassword { get; set; }
        
        public ILogger Logger { get; set; }

        public string[] ListUsers()
        {
            string etcPasswdContent = Execute("cat /etc/passwd");
            PasswdEntry[] etcPasswdEntries = PasswdEntry.Parse(etcPasswdContent);
            return etcPasswdEntries.Select(pwd=> pwd.UserName).ToArray();
        }

        public bool AddUser(string userName, string password)
        {
            try
            {
                string output = AddUser(HostName, Port, LoginUserName, LoginPassword, userName, password);
                Log.Info("{0}:{1}:{2}: {3}", nameof(RemoteSshHost), HostName, nameof(AddUser), output);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Exception adding user to {0}: {1}", HostName, ex.Message);
                return false;
            }
        }

        public bool DeleteUser(string userName)
        {
            try
            {
                string output = DeleteUser(HostName, Port, LoginUserName, LoginPassword, userName);
                Log.Info("{0}:{1}:{2}: {3}", nameof(RemoteSshHost), HostName, nameof(AddUser), output);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception deleting user to {0}: {1}", HostName, ex.Message);
                return false;
            }

            return true;
        }

        public string[] ListGroups()
        {
            
            throw new NotImplementedException();
        }
        
        public bool DeleteGroup(string groupName)
        {
            throw new NotImplementedException();
        }

        public bool Upload(string localFilePath, string remoteFilePath)
        {
            throw new NotImplementedException();
        }

        public bool Download(string remoteFilePath, string localFilePath)
        {
            throw new NotImplementedException();
        }
        
        public string DeleteUser(string hostName, int port, string loginUserName, string loginPassword,
            string userNameToDelete)
        {
            return Execute(hostName, port, loginUserName, loginPassword, $"userdel {loginUserName}");
        }
        
        public string AddUser(string hostName, int port, string loginUserName, string loginPassword,
            string newUserName, string newUserPassword)
        {
            throw new NotImplementedException("This method is not properly implemented");
            // TODO: fix this implementation to modify files directly or set sudo to not require password on the host or both
            return Execute(hostName, port, loginUserName, loginPassword,
                $"useradd {newUserName}; echo -e \"{newUserPassword}\n{newUserPassword}\" | passwd {newUserName}");
        }
        
        public string[] ListNetworkInterfaces(string hostName, int port, string userName, string password)
        {
            return Execute(hostName, port, userName, password, "ls /sys/class/net").DelimitSplit(" ");
        }
        
        public string GetMacAddress(string hostName, int port, string userName, string password, string interfaceName = "eth0")
        {
            return Execute(hostName, port, userName, password, $"cat /sys/class/net/{interfaceName}/address");
        }
        
        public void Copy(DirectoryInfo localDirectory, DirectoryInfo remoteDirectory = null, bool overwrite = true)
        {
            Copy(ManagedFileSet.FromDirectory(localDirectory.FullName), 3, remoteDirectory.FullName, overwrite);
        }

        public void Copy(ManagedFileSet managedFileSet, string remoteFolder = null, bool overwrite = true)
        {
            Copy(managedFileSet, 3, remoteFolder, overwrite);
        }

        [Verbosity(VerbosityLevel.Error)] 
        public event EventHandler CopyException;

        [Verbosity(VerbosityLevel.Information)]
        public event EventHandler CopyFileSetStarted;
        
        [Verbosity(VerbosityLevel.Information)]
        public event EventHandler CopyFileSetComplete;
        
        [Verbosity(VerbosityLevel.Information)]
        public event EventHandler CopyFileStarted;
        
        [Verbosity(VerbosityLevel.Information)]
        public event EventHandler CopyFileComplete;

        public void Copy(ManagedFileSet managedFileSet, int retryCount = 3, string remoteFolder = null, bool overwrite = true)
        {
            using (SftpClient sftpClient = GetSftpClient())
            {
                sftpClient.Connect();
                Thread.Sleep(30);
                FireEvent(CopyFileSetStarted, new RemoteSshHostCopyArgs {ManagedFileSet = managedFileSet});
                Parallel.ForEach(managedFileSet.GetManagedFiles(remoteFolder), (managedFile) =>
                {
                    FireEvent(CopyFileStarted, new RemoteSshHostCopyArgs {ManagedFile = managedFile});
                    int? tryCount = 0;
                    while (tryCount < retryCount)
                    {
                        try
                        {
                            FileInfo file = managedFile.LocalFile;
                            DirectoryInfo directory = new FileInfo(managedFile.RemoteFilePath).Directory;
                            EnsureDirectory(sftpClient, directory.FullName);
                            using (Stream fileStream = File.OpenRead(file.FullName))
                            {
                                Logger.Info("Uploading {0} to host {1}", file.FullName, HostName);
                                if (!overwrite)
                                {
                                    if (RemoteFileExists(sftpClient, directory.FullName, file.Name, out string filePath))
                                    {
                                        Logger.AddEntry("Remote file exists => {0}:{1}", HostName, filePath);
                                        return;
                                    }
                                }

                                sftpClient.UploadFile(fileStream, managedFile.RemoteFilePath);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            ++tryCount;
                            string msgSignature = "Error copying file ({0}) to remote: {1}";
                            bool willRetry = false;
                            if (tryCount < retryCount)
                            {
                                msgSignature = "Error copying file ({0}) to remote, (WILL RETRY): {1}";
                                willRetry = true;
                            }
                            Logger.AddEntry(msgSignature, ex, managedFile?.LocalFile?.FullName ?? "[null]", ex.Message);
                            FireEvent(CopyException, new RemoteSshHostCopyArgs{Exception = ex, ManagedFile = managedFile, WillRetry = willRetry});
                        }
                    }
                    FireEvent(CopyFileComplete, new RemoteSshHostCopyArgs {ManagedFile = managedFile});
                });
                FireEvent(CopyFileSetComplete, new RemoteSshHostCopyArgs {ManagedFileSet = managedFileSet});
            }
        }
        
        public static void Copy(string host, int port, string userName, string password, ManagedFileSet managedFileSet, string remoteFolder = null, bool overwrite = true)
        {
            new RemoteSshHost
            {
                HostName = host,
                Port = port,
                LoginUserName = userName,
                LoginPassword = password
            }.Copy(managedFileSet, remoteFolder, overwrite);
        }
        
        public SshClient GetSshClient()
        {
            return new SshClient(HostName, Port, LoginUserName, LoginPassword);
        }
        
        public SftpClient GetSftpClient()
        {
            return new SftpClient(HostName, Port, LoginUserName, LoginPassword);
        }

        public string Execute(string command)
        {
            return Execute(GetSshClient(), command);
        }
        
        public static RemoteSshHost FromHostInfo(RemoteSshHostInfo hostInfo)
        {
            throw new NotImplementedException("ManagedPassword implementation is incomplete");
            /*return new RemoteSshHost
            {
                HostName = hostInfo.HostName,
                Port = hostInfo.Port,
                LoginUserName = hostInfo.UserName,
                LoginPassword = hostInfo.ManagedPassword.Show()
            };*/
        }
        
        public static string Execute(string hostName, int port, string userName, string password, string command)
        {
            using (SshClient sshClient = new SshClient(hostName, port, userName, password))
            {
                return Execute(sshClient, command);
            }
        }

        public static string Execute(SshClient sshClient, string command)
        {
            sshClient.Connect();
            SshCommand sshCommand = sshClient.RunCommand(command);
            return sshCommand.Result;
        }

        public void EnsureDirectory(string path)
        {
            using (SftpClient client = GetSftpClient())
            {
                client.Connect();
                EnsureDirectory(client, path);
            }
        }

        public static void EnsureDirectory(SftpClient client, string path)
        {
            string current = "";

            if (path[0] == '/')
            {
                path = path.Substring(1);
            }

            while (!string.IsNullOrEmpty(path))
            {
                int p = path.IndexOf('/');
                current += '/';
                if (p >= 0)
                {
                    current += path.Substring(0, p);
                    path = path.Substring(p + 1);
                }
                else
                {
                    current += path;
                    path = "";
                }

                try
                {
                    SftpFileAttributes attrs = client.GetAttributes(current);
                    if (!attrs.IsDirectory)
                    {
                        throw new Exception("not directory");
                    }
                }
                catch (SftpPathNotFoundException)
                {
                    client.CreateDirectory(current);
                }
            }
        }

        public bool RemoteFileExists(FileInfo file)
        {
            return RemoteFileExists(file.Directory.FullName, file.Name);
        }
        
        public bool RemoteFileExists(string remoteFolder, string remoteFileName)
        {
            return RemoteFileExists(GetSftpClient(), remoteFolder, remoteFileName);
        }
        
        protected static bool RemoteFileExists(SftpClient sftpClient, string remoteFolder, string remoteFileName)
        {
            return RemoteFileExists(sftpClient, remoteFolder, remoteFileName);
        }
        
        protected static bool RemoteFileExists(SftpClient sftpClient, string remoteFolder, string remoteFileName, out string filePath)
        {
            bool fileExists = sftpClient.ListDirectory(remoteFolder).Any(f =>
                f.IsRegularFile && f.Name.ToLowerInvariant().Equals(remoteFileName.ToLowerInvariant()));
            filePath = Path.Combine(remoteFolder, remoteFileName);
            return fileExists;
        }
    }
}