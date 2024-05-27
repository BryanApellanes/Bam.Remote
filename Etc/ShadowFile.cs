using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bam;

namespace Bam.Remote.Etc
{
    public class ShadowFile
    {
        public ShadowFile()
        {
            _rows = new List<ShadowEntry>();
        }

        private List<ShadowEntry> _rows;

        public ShadowEntry[] Rows
        {
            get => _rows.ToArray();
            set => _rows = new List<ShadowEntry>(value);
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
                    Rows.Each(userEntry => result.Add(userEntry.UserName, userEntry));
                    return result;
                });
            }
        }

        public ShadowEntry SetPassword(string userName, string password)
        {
            ShadowEntry entry = this[userName];
            entry.SetPassword(password);
            return entry;
        }
        
        public ShadowEntry AddEntry(string userName, string password)
        {
            ShadowEntry entry = new ShadowEntry
            {
                UserName = userName,
            };
            entry.SetPassword(password);
            _rows.Add(entry);
            return entry;
        }
        
        public ShadowEntry this[string userName] => RowsByUser[userName];
        public string Print()
        {
            StringBuilder result = new StringBuilder();
            foreach (ShadowEntry entry in Rows)
            {
                result.AppendLine(entry.ToString());
            }

            return result.ToString();
        }

        public void Save(string filePath = "/etc/shadow", bool overwrite = true)
        {
            Print().SafeWriteToFile(filePath, overwrite);
        }
        
        public static ShadowFile Load(string filePath = "/etc/shadow")
        {
            return new ShadowFile
            {
                Rows = ShadowEntry.Parse(File.ReadAllText(filePath)), 
            };
        }
    }
}