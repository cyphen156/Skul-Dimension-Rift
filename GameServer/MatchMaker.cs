using System.Net;

namespace GameServer
{
    internal class MatchMaker
    {
        private IPAddress[] clientIps = new IPAddress[2];
        
        public bool AllocatePlayer(IPAddress PlayerIP)
        {
            for (int i = 0; i < clientIps.Length; ++i)
            {
                if (clientIps[i] == null)
                {
                    clientIps[i] = PlayerIP;
                    return true;
                }
            }
            return false;
        }
    }
}
