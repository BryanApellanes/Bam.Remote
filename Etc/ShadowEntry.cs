using System;
using System.Linq;
using Bam;

namespace Bam.Remote.Etc
{
    /// <summary>
    /// Represents an entry in the /etc/shadow file
    /// </summary>
    public class ShadowEntry
    {
        public ShadowEntry()
        {
            Password = new ShadowPassword();
            LastChanged = EpochDay.Today;
        }
        public string UserName { get; set; }
        public ShadowPassword Password { get; set; }
        public EpochDay LastChanged { get; set; }
        
        /// <summary>
        /// The number of days left before the user is allowed to change their password.
        /// </summary>
        public string Minimum { get; set; }
        
        /// <summary>
        /// The maximum number of days before the user is required to change their password.
        /// </summary>
        public string Maximum { get; set; }
        
        /// <summary>
        /// The number of days before password expiration that the user is warned their password must be changed.
        /// </summary>
        public string Warn { get; set; }

        /// <summary>
        /// The number of days after the password expires that the account is disabled.
        /// </summary>
        public string Inactive { get; set; }
        
        /// <summary>
        /// The specific date that the login may no longer be used.
        /// </summary>
        public EpochDay Expire { get; set; }

        public void SetPassword(string password)
        {
            Password.Set(password);
        }
        
        public override string ToString()
        {
            return $"{UserName}:{Password.ToString()}:{LastChanged.ToString()}:{Minimum}:{Maximum}:{Warn}:{Inactive}:{Expire?.ToString()}:";
        }

        public static ShadowEntry[] Parse(string fileContent)
        {
            string[] lines = fileContent.DelimitSplit("\n");
            return lines.Select(line => ParseLine(line.Trim())).ToArray();
        }

        private static ShadowEntry ParseLine(string line)
        {
            string[] split = line.Split(":");
            return new ShadowEntry
            {
                UserName = split[0],
                Password = ShadowPassword.Parse(split[1]),
                LastChanged = new EpochDay(int.Parse(split[2])),
                Minimum = split[3],
                Maximum = split[4],
                Warn = split[5],
                Inactive = split[6],
                Expire = string.IsNullOrEmpty(split[7]) ? new EpochDay(0): new EpochDay(int.Parse(split[7])),
            };
        }
    }
}