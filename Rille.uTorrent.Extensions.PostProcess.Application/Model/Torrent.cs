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
        public TorrentStatus TorrentStatus { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int ActualSeedRatioPercent { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsFolder
        {
            get
            {
                // Bugfix for single-files that are downloaded in the root torrent folder.
                if (Path.ToLower().TrimEnd('\\') == _config.DownloadedTorrentsFolder.ToLower().TrimEnd('\\'))
                    return false;

                return Directory.Exists(Path);
            }
        }

        private bool IsSingleFile(out string fileExtension)
        {
            fileExtension = "";

            if (FileList != null && FileList.Count == 1)
            {
                fileExtension = new FileInfo(FileList[0]).Extension.ToLower().Trim('.');
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsSingleFileAndArchive
        {
            get
            {
                var isSingleFile = IsSingleFile(out string extension);
                return isSingleFile && _config.ArchiveFirstFilePossibleFileExtensions.Contains(extension);
            }
        }
        public bool IsSingleFileButNotArchive
        {
            get
            {
                var isSingleFile = IsSingleFile(out string extension);
                return isSingleFile && !_config.ArchiveFirstFilePossibleFileExtensions.Contains(extension);
            }
        }
        public List<string> FileList { get; set; }
        public bool IsDownloaded
        {
            get
            {
                return ProgressPercent >= 100;
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
        public SeedingGoal SeedingGoal { get; set; }
        public string Trackers { get; internal set; }
        public string Label { get; internal set; }
        public ProcessingStatus ProcessingStatus { get; set; }
    }

    [Flags]
    public enum TorrentStatus
    {
        Started = 1,
        Checking = 2,
        StartAfterCheck = 4,
        Checked = 8,
        Error = 16,
        Paused = 32,
        Queued = 64,
        Loaded = 128
    }
}