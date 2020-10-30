using System;
using System.Security.Cryptography;
using Bam.Net;
using Bam.Net.CommandLine;
using Bam.Net.Services;
using CryptSharp;
using Org.BouncyCastle.Security;

namespace Bam.Remote.Etc
{
    public class ShadowPassword<T>: ShadowPassword where T: IShadowPasswordSetter, new()
    {
        public ShadowPassword()
        {
            PasswordSetter = new T();
        }
    }
    
    public class ShadowPassword
    {
        public ShadowPassword(IShadowPasswordSetter passwordSetter = null)
        {
            PasswordSetter = passwordSetter ?? new CrypterPasswordSetter();
        }

        public ShadowPassword(string salt)
        {
            _salt = salt;
        }

        public ShadowPassword<T> Copy<T>(ShadowPassword password) where T : IShadowPasswordSetter, new()
        {
            ShadowPassword<T> result = new ShadowPassword<T>();
            result.CopyProperties(this);
            result.PasswordSetter = new T();
            return result;
        }
        
        [Inject]
        public IShadowPasswordSetter PasswordSetter { get; set; }
        
        public PasswordHashAlgorithm Algorithm { get; set; }
        
        private string _salt;
        private readonly object _saltLock = new object();

        public string Salt
        {
            get => _saltLock.DoubleCheckLock(ref _salt, GenerateSalt);
            set => _salt = value;
        }
        public string Hash { get; set; }

        public bool IsNotEstablished { get; set; }
        public bool IsLocked { get; set; }

        public void Set(string value)
        {
            if (string.IsNullOrEmpty(Salt))
            {
                Salt = GenerateSalt();
            }

            Algorithm = PasswordHashAlgorithm.Sha512;
            PasswordSetter.Salt = Salt;
            ShadowPassword newPassword = PasswordSetter.Set(value);
            this.CopyProperties(newPassword);
        }
        
        public override string ToString()
        {
            if (IsNotEstablished)
            {
                return "*";
            }

            if (IsLocked)
            {
                return "!";
            }
            return $"${GetAlgorithmString()}${Salt}${Hash}";
        }

        public static ShadowPassword Parse(string shadowPassword)
        {
            Args.ThrowIfNullOrEmpty(shadowPassword);
            if (shadowPassword.Equals("*"))
            {
                return new ShadowPassword {IsNotEstablished = true};
            }

            if (shadowPassword.Equals("!"))
            {
                return new ShadowPassword {IsLocked = true};
            }
            
            string[] split = shadowPassword.DelimitSplit("$");
            return new ShadowPassword
            {
                Algorithm = GetPasswordHashAlgorithm(split[0]),
                Salt = split[1],
                Hash = split[2],
            };
        }
        
        private static PasswordHashAlgorithm GetPasswordHashAlgorithm(string algorithmString)
        {
            switch (algorithmString)
            {
                case "2a":
                    return PasswordHashAlgorithm.BlowfishA;
                case "2y":
                    return PasswordHashAlgorithm.BlowfishY;
                case "5":
                    return PasswordHashAlgorithm.Sha256;
                case "6":
                    return PasswordHashAlgorithm.Sha512;
                case "1":
                default:
                    return PasswordHashAlgorithm.Md5;
            }
        }
        private string GetAlgorithmString()
        {
            switch (Algorithm)
            {
                case PasswordHashAlgorithm.BlowfishA:
                    return "2a";
                case PasswordHashAlgorithm.BlowfishY:
                    return "2y";
                case PasswordHashAlgorithm.Sha256:
                    return "5";
                case PasswordHashAlgorithm.Sha512:
                    return "6";
                case PasswordHashAlgorithm.Md5:
                default:
                    return "1";
            }
        }

        private string GenerateSalt()
        {
            return Crypter.Sha512.GenerateSalt().TruncateFront("$6$".Length);
        }
    }
}