using CryptSharp;

namespace Bam.Remote.Etc
{
    public class CrypterPasswordSetter: IShadowPasswordSetter
    {
        public string Salt { get; set; }
        public ShadowPassword Set(string password)
        {         
            return ShadowPassword.Parse(Crypter.Sha512.Crypt(password, $"$6${Salt}"));
        }
    }
}