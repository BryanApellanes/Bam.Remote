using System.Collections.Generic;
using System.Linq;
using Bam.Net;

namespace Bam.Remote.Etc
{
    /// <summary>
    /// Represents a single line in the /etc/group file.
    /// </summary>
    public class GroupEntry
    {
        public GroupEntry()
        {
            Password = "x";
        }
        
        public string GroupName { get; set; }
        public string Password { get; set; }
        public uint GroupId { get; set; }

        private HashSet<string> _users;
        private readonly object _usersLock = new object();
        public string Users
        {
            get => string.Join(",", _users.ToArray());
            set
            {
                lock (_usersLock)
                {
                    _users = new HashSet<string>(value.Split(','));
                }
            } 
        }

        public GroupEntry AddMember(string userName)
        {
            lock (_usersLock)
            {
                _users.Add(userName);
                return this;
            }
        }

        public bool IsMember(string userName)
        {
            return _users.Contains(userName);
        }
        
        /// <summary>
        /// Return users as an array
        /// </summary>
        /// <returns></returns>
        public string[] GetUsers()
        {
            return _users.ToArray();
        }
        
        public override string ToString()
        {
            return $"{GroupName}:{Password}:{GroupId}:{Users}";
        }

        public static GroupEntry[] Parse(string fileContent)
        {
            string[] lines = fileContent.DelimitSplit("\n");
            return lines.Select(line => ParseLine(line.Trim())).ToArray();
        }

        private static GroupEntry ParseLine(string line)
        {
            string[] split = line.Split(":");
            return new GroupEntry
            {
                GroupName = split[0],
                Password = split[1],
                GroupId = uint.Parse(split[2]),
                Users = split[3],
            };
        }
    }
}