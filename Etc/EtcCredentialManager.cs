using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bam.Net;

namespace Bam.Remote.Etc
{
    public class EtcCredentialManager : IEtcCredentialManager
    {
        public EtcCredentialManager(string etcDirectory = "/etc/")
        {
            Etc = new DirectoryInfo(etcDirectory);
            Passwd = PasswdFile.Load(Path.Combine(etcDirectory, "passwd"));
            Group = GroupFile.Load(Path.Combine(etcDirectory, "group"));
            Shadow = ShadowFile.Load(Path.Combine(etcDirectory, "shadow"));
        }
        
        public PasswdFile Passwd { get; set; }
        public GroupFile Group { get; set; }
        public ShadowFile Shadow { get; set; }
        
        protected DirectoryInfo Etc { get; set; }

        public string[] Users => Passwd.Rows.Select(passwdEntry => passwdEntry.UserName).ToArray();
        
        public EtcUser AddUser(string userName, string password)
        {
            PasswdEntry passwdEntry = Passwd.AddUser(userName);
            GroupEntry groupEntry = Group.AddGroup(userName);
            ShadowEntry shadowEntry = Shadow.AddEntry(userName, password);
            passwdEntry.GroupId = groupEntry.GroupId;

            return new EtcUser
            {
                EtcCredentialManager = this,
                UserName = userName,
                Password = new ManagedPassword(password)
            };
        }

        public EtcUser SetPassword(string userName, string password)
        {
            Args.ThrowIf(!UserExists(userName), "Cannot change password, specified user does not exist: {0}", userName);
            RowsByUser[userName].Password.Set(password);
            return new EtcUser
            {
                EtcCredentialManager = this,
                UserName = userName,
                Password = new ManagedPassword(password)
            };
        }
        
        public EtcGroup AddGroup(string groupName, params string[] members)
        {
            GroupEntry groupEntry = Group.AddGroup(groupName);
            AddGroupMembers(groupName, members);
            
            return new EtcGroup
            {
                EtcCredentialManager = this,
                GroupName = groupEntry.GroupName,
                GroupId = groupEntry.GroupId
            };
        }

        public EtcGroup AddGroupMember(string groupName, string member)
        {
            return AddGroupMembers(groupName, member);
        }

        public EtcGroup AddGroupMembers(string groupName, params string[] members)
        {
            Args.ThrowIf(!Group.Exists(groupName), "Specified group does not exist {0}", groupName);
            HashSet<string> missingMembers = new HashSet<string>();
            foreach (string member in members)
            {
                if (!UserExists(member))
                {
                    missingMembers.Add(member);
                }
            }

            if (missingMembers.Count > 0)
            {
                Args.Throw<InvalidOperationException>("Failed to add members to group ({0}), one or more members specified do not exist: {1}", groupName, string.Join("\r\n\t", missingMembers));
            }

            foreach (string member in members)
            {
                Group.AddGroupMember(groupName, member);
            }
            Group.Save();
            return new EtcGroup
            {
                EtcCredentialManager = this,
                GroupName = groupName,
                GroupId = Group[groupName].GroupId
            };
        }
        
        public ShadowEntry this[string userName] => RowsByUser[userName];

        public void Save(string etcDirectoryPath = "/etc/")
        {
            DirectoryInfo dirArg = new DirectoryInfo(etcDirectoryPath);
            DirectoryInfo toUse = !(Etc.FullName.Equals(dirArg.FullName)) ? dirArg : Etc;
            Passwd.Save(Path.Combine(toUse.FullName, "passwd"));
            Group.Save(Path.Combine(toUse.FullName, "group"));
            Shadow.Save(Path.Combine(toUse.FullName, "shadow"));
        }
        
        /// <summary>
        /// Set the password setter implementation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void SetPasswordSetter<T>() where T : IShadowPasswordSetter, new()
        {
            foreach (string user in Users)
            {
                this[user].Password = this[user].Password.Copy<T>(this[user].Password);
            }
        }

        public bool UserExists(string userName)
        {
            return RowsByUser.ContainsKey(userName);
        }

        public bool GroupExists(string groupName)
        {
            return Group.Exists(groupName);
        }
        
        private Dictionary<string, ShadowEntry> _rowsByUser;
        private readonly object _rowsByUserLock = new object();
        protected Dictionary<string, ShadowEntry> RowsByUser
        {
            get
            {
                return _rowsByUserLock.DoubleCheckLock(ref _rowsByUser, () =>
                {
                    Dictionary<string, ShadowEntry> result = new Dictionary<string, ShadowEntry>();
                    Shadow.Rows.Each(passwdEntry => result.Add(passwdEntry.UserName, passwdEntry));
                    return result;
                });
            }
        }
    }
}