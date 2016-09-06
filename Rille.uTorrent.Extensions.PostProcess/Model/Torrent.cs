namespace Rille.uTorrent.Extensions.PostProcess.Model
{
    public class Torrent
    {
        public Torrent(string torrentHash)
        {
            this.Hash = torrentHash;
        }

        public string Hash { get; set; }
        public int NumericStatus { get; set; }
        public TorrentStatus TorrentStatus => (TorrentStatus)NumericStatus;
        public string Name { get; set; }
        public string Path { get; set; }
        public int ActualSeedRatio { get; set; }
    }

    public enum TorrentStatus
    {
        CompletelyFinished = 136,
        ActivelySeeding = 201,
        ErrorNetworkPathNA = 152,
        ForcedSeeding = 137
    }
}