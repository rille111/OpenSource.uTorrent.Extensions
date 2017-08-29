using System;
using System.Linq;
using NLog;
using Rille.uTorrent.Extensions.PostProcess.Model;
using Rille.uTorrent.Extensions.PostProcess.Services;

namespace Rille.uTorrent.Extensions.PostProcess
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static string[] _arguments;
        private static Config _config;
        private static ITorrentManager _torrentManager;
        private static FileManager _fileManager;

        static void Main(string[] args)
        {
            try
            {
                var processedTorrentsCount = 0;
                _logger.Info("Application starting.");
                _config = Config.Create();
                ValidateConfig(_config);
                _fileManager = new FileManager(_config);
                var unpacker = new Unpacker(_config, _fileManager);
                if (_config.OperatingMode == OperatingMode.UnpackTorrentsFolderOnly)
                    _torrentManager = new FolderBasedTorrentManager(_config, _fileManager);
                else
                    _torrentManager = new UTorrentManager(_config);

                var torrents = _torrentManager.GetTorrentList();

                _logger.Info($"LOOPING TORRENTS");
                foreach (var torrent in torrents)
                {
                    _logger.Info($"Processing: {torrent.Name} -");
                    _logger.Debug($"- Path: {torrent.Path}");
                    _logger.Debug($"- IsFolder: {torrent.IsFolder}");
                    _logger.Debug($"- IsSingleFileAndArchive: {torrent.IsSingleFileAndArchive}");
                    _logger.Debug($"- IsSingleFileButNotArchive: {torrent.IsSingleFileButNotArchive}");
                    _logger.Debug($"- DoesFolderContainAnyArchive: {_fileManager.DoesFolderContainAnyArchive(torrent.Path)}");

                    if (_torrentManager.HasTorrentBeenPostProcessed(torrent))
                    {
                        // Torrent has already been processed.
                        _logger.Info($"- Torrent has already been processed!");

                        // So why is it here? Leftover from a crash? We must make sure.
                        var unpackedOk = unpacker.UnpackAndCopy(torrent);
                        if (unpackedOk)
                        {
                            if (_config.DeleteAlreadyProcessedTorrents)
                            {
                                // Delete already processed torrents was true, so delete it.
                                _torrentManager.DeleteTorrent(torrent);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (processedTorrentsCount >= _config.MaxProcessTorrentsInBatch)
                        {
                            // Enough in this  batch. Exit.
                            _logger.Info($"- Processed {processedTorrentsCount} in this batch which was the configured max, exiting..");
                            WaitAndExit();
                        }

                        // Otherwise, lets unpack/process!
                        processedTorrentsCount++;

                        var unpackedOk = unpacker.UnpackAndCopy(torrent);
                        if (unpackedOk)
                        {
                            _logger.Info($"- Torrent unpacked OK.");
                            //TODO: Mark as processed

                            if (_torrentManager.HasTorrentGoalsBeenReached(torrent))
                            {
                                // Delete if goals reached and torrent processed ok?
                                if (_config.DeleteFromTorrentsFolderWhenUnpacked)
                                    _torrentManager.DeleteTorrent(torrent);
                            }

                        }
                        else
                        {
                            // Unpack error!! Quit!
                            _logger.Error($"- Failed to unpack!!");
                            WaitAndExit();
                        }
                    }
                }
                _logger.Info($"FINISHED");

                //if (ShouldProcessAllTorrents())
                //    ProcessAllTorrents();

                //if (ShouldProcessOneTorrent())
                //    ProcessOneTorrent();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Unexpected error occurred.");
            }
            WaitAndExit();
        }

        private static void WaitAndExit()
        {
            Console.WriteLine("-- Finished, press any key to exit --");
            Console.ReadKey();
            Environment.Exit(1);
        }

        private static void ValidateConfig(Config config)
        {
            var result = new ConfigValidator().Validate(config);

            if (result.IsValid)
            {
                _logger.Info("Config is valid.");
            }
            else
            {
                var errors = string.Join("\n", result.Errors.Select(p => p.PropertyName + ": " + p.ErrorMessage + " Value: " + p.AttemptedValue));
                throw new InvalidProgramException("Invalid configuration!\n" + errors);
            }
        }

        private static bool ShouldProcessAllTorrents()
        {
            return _arguments.Any() && _arguments[0] == "-all";
        }

        private static void ProcessAllTorrents()
        {
            // TODO : Loop ...
        }

        private static bool ShouldProcessOneTorrent()
        {
            // Anything other than - will be interpreted as a Hash.
            return _arguments.Any() && _arguments[0] != "-";
        }

        private static void ProcessOneTorrent()
        {
            var torrent = new Torrent(_arguments[0], _config);
            _logger.Info($"Torrent hash is: {torrent.Hash}. I will now load all torrents.");

            var torrents = _torrentManager.GetTorrentList();
            torrent = torrents.Single(p => p.Hash == torrent.Hash);
            _logger.Info($"Found torrent. Name: {torrent.Name}, NumericStatus: {torrent.NumericStatus}, Status: {torrent.TorrentStatus}");

            if (!_torrentManager.HasTorrentBeenPostProcessed(torrent))
            {
                // Execute PostProcess
                // TODO: Create method and move logging here
                _logger.Info("Post processes starting.");
            }

            if (_torrentManager.HasTorrentBeenPostProcessed(torrent) && _torrentManager.HasTorrentGoalsBeenReached(torrent))
            {
                // Delete
                // TODO: Create method and move logging here
                _logger.Info("Deleting torrent, goals has been reached.");
            }
            else
            {
                _logger.Warn($"Torrent has either not been post processed yet, or the goals haven't been reached. Not doing anything.");
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {

        }
    }
}
