using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rille.uTorrent.Extensions.PostProcess.Model
{
    public class Torrent
    {
        private readonly Config _config;

        public Torrent(string torrentHash, Config config)
        {
            this.Hash = torrentHash;
            this._config = config;
        }

        public string Hash { get; set; }
        public int NumericStatus { get; set; }
        public TorrentStatus TorrentStatus => (TorrentStatus)NumericStatus;
        public string Name { get; set; }
        public string Path { get; set; }
        public int ActualSeedRatio { get; set; }
        public bool IsFolder
        {
            get
            {
                return System.IO.Directory.Exists(Path);
            }
        }
        public bool IsSingleFileAndArchive
        {
            get
            {
                var file = new System.IO.FileInfo(Path);

                if (!file.Exists)
                    return false;

                var extension = file.Extension.ToLower().Trim('.');

                return _config.ArchiveFirstFilePossibleFileExtensions.Contains(extension);
            }
        }
        public bool IsSingleFileButNotArchive
        {
            get
            {
                if (!System.IO.File.Exists(Path))
                    return false;

                return !IsSingleFileAndArchive;
            }
        }
        /// <summary>
        /// Where the torrent should be unpacked/processed to. Full path
        /// </summary>
        public string DestinationFolder
        {
            get
            {
                return $"{_config.FinalFolder}\\{this.Name}\\".Replace(@"\\", @"\");
            }
        }

        // Methods

    }

    public enum TorrentStatus
    {
        CompletelyFinished = 136,
        ActivelySeeding = 201,
        ErrorNetworkPathNA = 152,
        ForcedSeeding = 137
    }
}