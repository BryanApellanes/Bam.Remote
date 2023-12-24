using System;
using System.Security.Cryptography;
using Bam.Net;
using Bam.Net.CommandLine;
using CryptSharp.Core;

namespace Bam.Remote.Etc
{
    public class OpenSslShadowPasswordSetter: IShadowPasswordSetter
    {
        public OpenSslShadowPasswordSetter()
        {
            OpenSslPath = "which openssl".Run().StandardOutput.Trim();
        }
        
        public string Salt { get; set; }
        public ShadowPassword Set(string password)
        {
            if (string.IsNullOrEmpty(Salt))
            {
                Salt = GenerateSalt();
            }
            return ShadowPassword.Parse($"{OpenSslPath} passwd -6 -salt {Salt} {password}".Run().StandardOutput.Trim());
        }
        
        protected string OpenSslPath { get; set; }
        
        protected string GenerateSalt()
        {
            return Crypter.Sha512.GenerateSalt().TruncateFront("$6$".Length);
        }
    }
}