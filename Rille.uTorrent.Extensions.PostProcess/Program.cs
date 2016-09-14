using System;
using System.Diagnostics;
using System.Linq;
using NLog;
using Rille.uTorrent.Extensions.PostProcess.Model;
using Rille.uTorrent.Extensions.PostProcess.Services;

namespace Rille.uTorrent.Extensions.PostProcess
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string[] _arguments;
        private static Config _config;
        private static ITorrentManager _torrentManager;

        static void Main(string[] args)
        {
            try
            {
                Logger.Debug("Application starting.");

                Initialize(args);
                ValidateConfig(_config);

                if (ShouldProcessAllTorrents())
                    ProcessAllTorrents();

                if (ShouldProcessOneTorrent())
                    ProcessOneTorrent();
            }
            catch (Exception ex)
            {
                Logger.Error("Unexpected error occurred.", ex);
                
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private static void Initialize(string[] args)
        {
            _arguments = args;
            _config = new Config();
            var fileManager = new FileManager(_config);
            _torrentManager = new UTorrentManager(_config, fileManager);
        }

        private static void ValidateConfig(Config config)
        {
            var result = new ConfigValidator().Validate(config);
            
            if (result.IsValid)
            {
                Logger.Debug("Config is valid.");
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
            var torrent = new Torrent(_arguments[0]);
            Logger.Debug($"Torrent hash is: {torrent.Hash}. I will now load all torrents.");

            var torrents = _torrentManager.GetTorrentList();
            torrent = torrents.Single(p => p.Hash == torrent.Hash);
            Logger.Info($"Found torrent. Name: {torrent.Name}, NumericStatus: {torrent.NumericStatus}, Status: {torrent.TorrentStatus}");

            if (!_torrentManager.TorrentHasBeenPostProcessed(torrent))
            {
                // Execute PostProcess
                // TODO: Create method and move logging here
                Logger.Info("Post processes starting.");
            }

            if (_torrentManager.TorrentHasBeenPostProcessed(torrent) && _torrentManager.TorrentGoalsReached(torrent))
            {
                // Delete
                // TODO: Create method and move logging here
                Logger.Info("Deleting torrent, goals has been reached.");
            }
            else
            {
                Logger.Warn($"Torrent has either not been post processed yet, or the goals haven't been reached. Not doing anything.");
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {

        }
    }
}
