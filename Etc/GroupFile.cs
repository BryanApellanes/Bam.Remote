using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Bam.Net;

namespace Bam.Remote.Etc
{
    public class GroupFile
    {
        public GroupFile()
        {
            _rows = new List<GroupEntry>();
            FilePath = "/etc/group";
        }
        
        public string FilePath { get; private set; }
        
        private List<GroupEntry> _rows;
        public GroupEntry[] Rows
        {
            get => _rows.ToArray();
            set => _rows = new List<GroupEntry>(value);
        }

        /// <summary>
        /// Returns true if the specified group exists.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public bool Exists(string groupName)
        {
            return RowsByGroup.ContainsKey(groupName);
        }
        
        /// <summary>
        /// Gets a list of GroupEntry instances where the specified user is a member.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<GroupEntry> GetUserGroups(string userName)
        {
            return Rows.Where(ge => ge.IsMember(userName)).ToList();
        }

        public GroupFile AddGroupMember(string groupName, string userName)
        {
            this[groupName].AddMember(userName);
            return this;
        }
        
        public GroupEntry AddGroup(string groupName)
        {
            GroupEntry groupEntry = new GroupEntry()
            {
                GroupName = groupName,
                GroupId = NextGroupId()
            };
            _rows.Add(groupEntry);
            return groupEntry;
        }

        private Dictionary<string, GroupEntry> _rowsByGroup;
        private readonly object _rowsByGroupLock = new object();
        protected Dictionary<string, GroupEntry> RowsByGroup
        {
            get
            {
                return _rowsByGroupLock.DoubleCheckLock(ref _rowsByGroup, () =>
                {
                    Dictionary<string, GroupEntry> result = new Dictionary<string, GroupEntry>();
                    Rows.Each(groupEntry => result.Add(groupEntry.GroupName, groupEntry));
                    return result;
                });
            }
        }
        
        public GroupEntry this[string groupName] => RowsByGroup[groupName];
        private Dictionary<uint, GroupEntry> _rowsByGroupId;
        private readonly object _rowsByGroupIdLock = new object();
        protected Dictionary<uint, GroupEntry> RowsByGroupId
        {
            get
            {
                return _rowsByGroupIdLock.DoubleCheckLock(ref _rowsByGroupId, () =>
                {
                    Dictionary<uint, GroupEntry> result = new Dictionary<uint, GroupEntry>();
                    Rows.Each(groupEntry => result.Add(groupEntry.GroupId, groupEntry));
                    return result;
                });
            }
        }

        public uint NextGroupId()
        {
            return RowsByGroupId.Keys.ToArray().Largest() + 1;
        }
        
        public string Print()
        {
            StringBuilder result = new StringBuilder();
            foreach (GroupEntry entry in Rows)
            {
                result.AppendLine(entry.ToString());
            }

            return result.ToString();
        }

        public void Save()
        {
            Save(string.IsNullOrEmpty(FilePath) ? "/etc/group": FilePath);
        }
        
        public void Save(string filePath = "/etc/group", bool overwrite = true)
        {
            Print().SafeWriteToFile(filePath, overwrite);
        }
        
        public static GroupFile Load(string filePath = "/etc/group")
        {
            return new GroupFile
            {
                Rows = GroupEntry.Parse(File.ReadAllText(filePath)),
                FilePath = filePath
            };
        }
    }
}