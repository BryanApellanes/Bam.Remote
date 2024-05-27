using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using Bam;

namespace Bam.Remote.Etc
{
    public class PasswdFile
    {
        public PasswdFile()
        {
            _rows = new List<PasswdEntry>();
        }

        private List<PasswdEntry> _rows;

        public PasswdEntry[] Rows
        {
            get => _rows.ToArray();
            set => _rows = new List<PasswdEntry>(value);
        }
        public PasswdEntry AddUser(string userName)
        {
            PasswdEntry passwdEntry = new PasswdEntry
            {
                UserName = userName,
                UserId = NextUserId()
            };
            _rows.Add(passwdEntry);
            return passwdEntry;
        }

        private Dictionary<string, PasswdEntry> _rowsByUser;
        private readonly object _rowsByUserLock = new object();
        protected Dictionary<string, PasswdEntry> RowsByUser
        {
            get
            {
                return _rowsByUserLock.DoubleCheckLock(ref _rowsByUser, () =>
                {
                    Dictionary<string, PasswdEntry> result = new Dictionary<string, PasswdEntry>();
                    Rows.Each(passwdEntry => result.Add(passwdEntry.UserName, passwdEntry));
                    return result;
                });
            }
        }
        
        public PasswdEntry this[string userName] => RowsByUser[userName];
        private Dictionary<uint, PasswdEntry> _rowsByUserId;
        private readonly object _rowsByUserIdLock = new object();
        protected Dictionary<uint, PasswdEntry> RowsByUserId
        {
            get
            {
                return _rowsByUserIdLock.DoubleCheckLock(ref _rowsByUserId, () =>
                {
                    Dictionary<uint, PasswdEntry> result = new Dictionary<uint, PasswdEntry>();
                    Rows.Each(passwdEntry => result.Add(passwdEntry.UserId, passwdEntry));
                    return result;
                });
            }
        }

        public uint NextUserId()
        {
            return RowsByUserId.Keys.ToArray().Largest() + 1;
        }
        
        public string Print()
        {
            StringBuilder result = new StringBuilder();
            foreach (PasswdEntry entry in Rows)
            {
                result.AppendLine(entry.ToString());
            }

            return result.ToString();
        }

        public void Save(string filePath = "/etc/passwd", bool overwrite = true)
        {
            Print().SafeWriteToFile(filePath, overwrite);
        }
        
        public static PasswdFile Load(string filePath = "/etc/passwd")
        {
            return new PasswdFile
            {
                Rows = PasswdEntry.Parse(File.ReadAllText(filePath)),
            };
        }
    }
}