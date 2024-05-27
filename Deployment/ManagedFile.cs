using System;
using System.IO;
using Bam;
using Bam.CommandLine;

namespace Bam.Remote.Deployment
{
    public class ManagedFile
    {
        public ManagedFile(FileInfo localFile = null)
        {
            LocalFile = localFile;
        }

        private FileInfo _localFile;

        public FileInfo LocalFile
        {
            get => _localFile;
            set
            {
                _localFile = value;
                if (string.IsNullOrEmpty(RemoteFilePath) && _localFile != null)
                {
                    RemoteFilePath = _localFile.FullName;
                }
            }
        }

        public string RemoteFilePath { get; set; }

        /// <summary>
        /// Same as CopyTo, set RemoteFilePath to the path specified.
        /// </summary>
        /// <param name="remoteFilePath"></param>
        /// <returns></returns>
        public ManagedFile To(string remoteFilePath)
        {
            return CopyTo(remoteFilePath);
        }
        
        /// <summary>
        /// Set RemoteFilePath to the path specified.
        /// </summary>
        /// <param name="remoteFilePath"></param>
        /// <returns></returns>
        public ManagedFile CopyTo(string remoteFilePath)
        {
            RemoteFilePath = remoteFilePath;
            return this;
        }

        /// <summary>
        /// Same as CopyFrom, get a ManagedFile representing the specified local file.
        /// </summary>
        /// <param name="localFile"></param>
        /// <returns></returns>
        public static ManagedFile From(string localFile, string remoteDirectory = null)
        {
            return CopyFrom(localFile, remoteDirectory);
        }
        
        /// <summary>
        /// Get a ManagedFile representing the specified local file.
        /// </summary>
        /// <param name="localFile"></param>
        /// <returns></returns>
        public static ManagedFile CopyFrom(string localFile, string remoteDirectory = null)
        {
            FileInfo file = new FileInfo(localFile);
            return GetReRootedFile(file.Directory.FullName, remoteDirectory ?? file.Directory.FullName, file);
        }

        public static ManagedFile GetReRootedFile(string localRoot, string remoteRoot, FileInfo file)
        {
            Args.ThrowIfNullOrEmpty(localRoot, nameof(localRoot));
            Args.ThrowIfNullOrEmpty(remoteRoot, nameof(remoteRoot));
            
            if (!localRoot.EndsWith("/"))
            {
                localRoot += "/";
            }

            if (!remoteRoot.EndsWith("/"))
            {
                remoteRoot += "/";
            }
            
            string filePath = file.FullName;
            if (!filePath.StartsWith(localRoot))
            {
                throw new InvalidOperationException($"The specified file is not in the specified '{nameof(localRoot)}' ({localRoot})");
            }

            string subPath = filePath.TruncateFront(localRoot.Length);
            return new ManagedFile
            {
                RemoteFilePath = Path.Combine(remoteRoot, subPath),
                LocalFile = file
            };
        }
    }
}