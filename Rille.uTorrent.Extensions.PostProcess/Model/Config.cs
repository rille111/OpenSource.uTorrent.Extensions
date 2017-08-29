using System;
using FluentValidation;
using System.Collections.Generic;
using Rille.uTorrent.Extensions.PostProcess.Services;

namespace Rille.uTorrent.Extensions.PostProcess.Model
{
    public class Config
    {
        public OperatingMode OperatingMode { get; set; }
        public int MaxProcessTorrentsInBatch { get; set; }
        public bool DeleteFromTorrentsFolderWhenUnpacked { get; set; }
        public bool DeleteAlreadyProcessedTorrents { get; set; }
        public string FinalFolder { get; set; }
        public string UnpackerExeFileFullPath { get; set; }
        public string UnpackerParameters { get; set; }
        public bool UnpackerHideWindow { get; set; }
        public IEnumerable<string> ArchiveFirstFilePossibleFileExtensions { get; set; }
        public IEnumerable<string> IsArchivePatterns { get; set; }
        public string[] IgnoreFileExtensionPatterns { get; private set; }
        public string[] IgnoreFileNamePatterns { get; private set; }
        public string[] IgnoreFolderPatterns { get; private set; }

        /// <summary>
        /// ie: http://localhost:8080
        /// </summary>
        public string TorrentWebApiUrl { get; set; } 
        public string TorrentWebApiLogin { get; set; } 
        public string TorrentWebApiPassword { get; set; }
        public string DownloadedTorrentsFolder { get; internal set; }

        public static Config Create()
        {
            var _config = new Config();
            var jReader = new JsonConfigFileReader();

            _config.OperatingMode = jReader.GetValue<OperatingMode>(nameof(_config.OperatingMode));
            _config.MaxProcessTorrentsInBatch = jReader.GetValue<int>(nameof(_config.MaxProcessTorrentsInBatch));
            _config.DeleteFromTorrentsFolderWhenUnpacked = jReader.GetValue<bool>(nameof(_config.DeleteFromTorrentsFolderWhenUnpacked));
            _config.DeleteAlreadyProcessedTorrents= jReader.GetValue<bool>(nameof(_config.DeleteAlreadyProcessedTorrents));

            _config.FinalFolder = jReader.GetValue<string>(nameof(_config.FinalFolder));
            _config.DownloadedTorrentsFolder = jReader.GetValue<string>(nameof(_config.DownloadedTorrentsFolder));

            _config.UnpackerParameters = jReader.GetValue<string>(nameof(_config.UnpackerParameters));
            _config.UnpackerExeFileFullPath = jReader.GetValue<string>(nameof(_config.UnpackerExeFileFullPath));
            _config.UnpackerHideWindow = jReader.GetValue<bool>(nameof(_config.UnpackerHideWindow));

            _config.ArchiveFirstFilePossibleFileExtensions = jReader.GetValue<string[]>(nameof(_config.ArchiveFirstFilePossibleFileExtensions));
            _config.IsArchivePatterns = jReader.GetValue<string[]>(nameof(_config.IsArchivePatterns));

            _config.TorrentWebApiUrl = jReader.GetValue<string>(nameof(_config.TorrentWebApiUrl));
            _config.TorrentWebApiLogin = jReader.GetValue<string>(nameof(_config.TorrentWebApiLogin));
            _config.TorrentWebApiPassword = jReader.GetValue<string>(nameof(_config.TorrentWebApiPassword));


            _config.IgnoreFileExtensionPatterns = jReader.GetValue<string[]>(nameof(_config.IgnoreFileExtensionPatterns));
            _config.IgnoreFileNamePatterns = jReader.GetValue<string[]>(nameof(_config.IgnoreFileNamePatterns));
            _config.IgnoreFolderPatterns = jReader.GetValue<string[]>(nameof(_config.IgnoreFolderPatterns));

            return _config;
        }
    }

    public enum OperatingMode
    {
        WorkWithTorrentApi, UnpackTorrentsFolderOnly
    }

    public enum UnpackerDecideIfProcessed
    {
        TorrentHasLabelProcessed, FolderExists
    }

    public class ConfigValidator : AbstractValidator<Config>
    {
        public ConfigValidator()
        {
            RuleFor(p => p.TorrentWebApiLogin).NotEmpty();
            RuleFor(p => p.TorrentWebApiPassword).NotEmpty();
            RuleFor(p => p.TorrentWebApiUrl).NotEmpty();
            RuleFor(p => p.ArchiveFirstFilePossibleFileExtensions).NotEmpty();
            RuleFor(p => p.FinalFolder).NotEmpty();
            RuleFor(p => p.DownloadedTorrentsFolder).NotEmpty();
            RuleFor(p => p.UnpackerExeFileFullPath).NotEmpty();
            RuleFor(p => p.UnpackerParameters).NotEmpty();

            RuleFor(p => p.TorrentWebApiUrl)
                .Must(BeValidUri)
                .WithMessage("Invalid format. Example: http://localhost:111/some/api");

            RuleFor(p => p.UnpackerExeFileFullPath)
                .Must(System.IO.File.Exists)
                .WithMessage("Didnt exist at the configured path.");

            RuleFor(p => p.FinalFolder)
                .Must(System.IO.Directory.Exists)
                .WithMessage("Didnt exist at the configured path.");

            RuleFor(p => p.DownloadedTorrentsFolder)
                            .Must(System.IO.Directory.Exists)
                            .WithMessage("Didnt exist at the configured path.");
        }

        private bool BeValidUri(string arg)
        {
            return Uri.TryCreate(arg, UriKind.Absolute, out Uri outUri);
        }
    }
}