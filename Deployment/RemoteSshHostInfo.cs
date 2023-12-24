using Bam.Net;
//using Bam.Net.CoreServices.ApplicationRegistration.Data;

namespace Bam.Remote.Deployment
{
    public class RemoteSshHostInfo
    {
        public RemoteSshHostInfo()
        {
            UserName = "bambot";
            //HostName = Machine.Current.Name;
            Port = 22;
        }

        public RemoteSshHostInfo(string hostName, int port = 22)
        {
            HostName = hostName;
            Port = port;
        }
        
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        //public ManagedPassword ManagedPassword { get; set; }

        public RemoteSshHost ToRemoteSshHost()
        {
            return RemoteSshHost.FromHostInfo(this);
        }

        public string Execute(string command)
        {
            return ToRemoteSshHost().Execute(command);
        }
        
        public static RemoteSshHostInfo For(string hostName)
        {
            return new RemoteSshHostInfo
            {
                HostName =  hostName
            };
        }
    }
}