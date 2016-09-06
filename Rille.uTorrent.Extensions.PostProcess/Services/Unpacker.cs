using Rille.uTorrent.Extensions.PostProcess.Model;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public class Unpacker
    {
        private readonly Config _config;

        public Unpacker(Config config)
        {
            _config = config;
        }
    }
}