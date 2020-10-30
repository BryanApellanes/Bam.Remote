using System.Linq;
using Bam.Net;

namespace Bam.Remote.Etc
{
    /// <summary>
    /// Represents a single line in the /etc/passwd file.
    /// </summary>
    public class PasswdEntry
    {
        public PasswdEntry()
        {
            CommandShell = "/bin/sh";
        }
        
        public string UserName { get; set; }
        public string Password { get; set; }
        public uint UserId { get; set; }
        public uint GroupId { get; set; }
        public string Comment { get; set; }
        public string HomeDirectory { get; set; }
        public string CommandShell { get; set; }
        
        public override string ToString()
        {
            return $"{UserName}:{Password}:{UserId}:{GroupId}:{Comment}:{HomeDirectory}:{CommandShell}";
        }

        public static PasswdEntry[] Parse(string fileContent)
        {
            string[] lines = fileContent.DelimitSplit("\n");
            return lines.Select(line => ParseLine(line.Trim())).ToArray();
        }

        private static PasswdEntry ParseLine(string line)
        {
            string[] split = line.Split(':');
            return new PasswdEntry
            {
                UserName = split[0],
                Password = split[1],
                UserId = uint.Parse(split[2]),
                GroupId = uint.Parse(split[3]),
                Comment = split[4],
                HomeDirectory = split[5],
                CommandShell = split[6],
            };
        }
    }
}