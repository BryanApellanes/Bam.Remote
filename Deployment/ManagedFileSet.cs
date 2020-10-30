using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bam.Net;
using Bam.Remote.Deployment.Data;
using CsQuery.ExtensionMethods;

namespace Bam.Remote.Deployment
{
    public class ManagedFileSet
    {
        public static implicit operator ManagedFileSet(ManagedFileSetDescriptor descriptor)
        {
            return FromDescriptor(descriptor);
        }

        public static ManagedFileSet FromDescriptor(ManagedFileSetDescriptor descriptor)
        {
            return new ManagedFileSet
            {
                Directories = descriptor.Directories.DelimitSplit(",", ";").Select(path => new DirectoryInfo(path)).ToArray(),
                Files = descriptor.Files.DelimitSplit(",", ";").Select(path => new FileInfo(path)).ToArray(),
            };
        }

        public static ManagedFileSet FromFile(string filePath)
        {
            return FromFile(new FileInfo(filePath));
        }

        public static ManagedFileSet FromFile(FileInfo file)
        {
            return new ManagedFileSet()
            {
                Files = new FileInfo[] {file}
            };
        }

        public static ManagedFileSet FromDirectory(string directoryPath, string remoteDirectory = null)
        {
            return FromDirectory(new DirectoryInfo(directoryPath), remoteDirectory);
        }
        
        public static ManagedFileSet FromDirectory(DirectoryInfo directoryInfo, string remoteDirectory = null)
        {
            return new ManagedFileSet()
            {
                RemoteDirectory = remoteDirectory,
                Directories = new DirectoryInfo[] {directoryInfo}
            };
        }
        
        public ManagedFileSet()
        {
            Directories = new DirectoryInfo[]{};
            Files = new FileInfo[]{};
        }
        
        /// <summary>
        /// If specified, the remote base directory that all files and directories are copied to.  If not specified,
        /// Directories and Files are copied to the remote using the equivalent local file path on the remote.
        /// </summary>
        public string RemoteDirectory { get; set; }
        
        public DirectoryInfo[] Directories { get; set; }
        public FileInfo[] Files { get; set; }

        public ManagedFileSet AddFile(string filePath)
        {
            return AddFile(new FileInfo(filePath));
        }
        
        public ManagedFileSet AddFile(FileInfo fileInfo)
        {
            Files = new HashSet<FileInfo>(Files) {fileInfo}.ToArray();
            return this;
        }

        public ManagedFileSet AddDirectory(string directoryPath)
        {
            return AddDirectory(new DirectoryInfo(directoryPath));
        }

        public ManagedFileSet AddDirectory(DirectoryInfo directoryInfo)
        {
            Directories = new HashSet<DirectoryInfo>(Directories){directoryInfo}.ToArray();
            return this;
        }
        
        public IEnumerable<ManagedFile> GetManagedFiles(string remoteDirectory = null)
        {
            HashSet<string> filePaths = new HashSet<string>();
            foreach (FileInfo file in Files)
            {
                string remoteRoot = remoteDirectory ?? RemoteDirectory ?? file.Directory.FullName;
                yield return ManagedFile.GetReRootedFile(file.Directory.FullName, remoteRoot, file);
            }

            HashSet<string> directories = Directories.Select(d => d.FullName).ToHashSet();
            foreach (string directoryPath in directories)
            {
                foreach (FileInfo file in new DirectoryInfo(directoryPath).GetFiles("*", SearchOption.AllDirectories))
                {
                    string remoteRoot = remoteDirectory ?? RemoteDirectory ?? file.Directory.FullName;
                    yield return ManagedFile.GetReRootedFile(directoryPath, remoteRoot, file);
                }
            }
        }
    }
}