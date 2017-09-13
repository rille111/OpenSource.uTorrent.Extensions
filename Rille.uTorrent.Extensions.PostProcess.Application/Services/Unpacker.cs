using System;
using Rille.uTorrent.Extensions.PostProcess.Model;
using NLog;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public class Unpacker
    {
        private readonly Config _config;
        private readonly FileManager _fileManager;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public Unpacker(Config config, FileManager fileManager)
        {
            _config = config;
            _fileManager = fileManager;
        }

        public bool CopyAndUnpack(Torrent torrent)
        {
            try
            {
                var exitCode = 0;

                if (torrent.IsSingleFileButNotArchive)
                {
                    _logger.Debug($" - Copying single file (not archive) torrent.");

                    var sourceFile = new FileInfo(Path.Combine(torrent.Path, torrent.FileList[0]));
                    var newDir = Directory.CreateDirectory(torrent.DestinationFolder);
                    File.Copy(sourceFile.FullName, $"{torrent.DestinationFolder}{sourceFile.Name}", false);
                    return true;
                }
                else if (torrent.IsSingleFileAndArchive)
                {
                    _logger.Debug($" - Unpacking single file torrent.");

                    var sourceFile = new FileInfo(Path.Combine(torrent.Path, torrent.FileList[0]));
                    var newDir = Directory.CreateDirectory(torrent.DestinationFolder);
                    exitCode = UnpackSingleArchive(torrent, torrent.DestinationFolder, sourceFile);
                }
                else if (torrent.IsFolder)
                {
                    // First copy the non-archive files! in torrent folder
                    _logger.Debug($" - Copying non-archive files in Torrent parent folder.");

                    if (!Directory.Exists(torrent.DestinationFolder))
                        Directory.CreateDirectory(torrent.DestinationFolder);

                    var filesToIgnore = _fileManager.GetIgnoredFiles(torrent.Path).ToList();
                    var filesToCopy = _fileManager
                        .GetAllFilesNotPartOfArchiveVolume(torrent.Path).ToList()
                        .RemoveFromList(filesToIgnore);

                    var subFolderToIgnore = _fileManager.GetIgnoredFolders(torrent.Path);
                    var subFolders = new DirectoryInfo(torrent.Path)
                        .EnumerateDirectories().ToList()
                        .RemoveFromList(subFolderToIgnore);

                    foreach (var file in filesToCopy)
                    {
                        // Skips existing
                        if (!File.Exists($"{torrent.DestinationFolder}\\{file.Name}"))
                            File.Copy(file.FullName, $"{torrent.DestinationFolder}\\{file.Name}");
                    }


                    foreach (var subfolder in subFolders)
                    {
                        _fileManager.CopyFolderRecursivelyExceptIgnoredStuffAndArchiveFirstFiles(subfolder.FullName, $"{torrent.DestinationFolder}\\{subfolder.Name}", true);
                    }

                    _logger.Debug($" - Processing sub folders (Unpack archives, copy non-archives) to {torrent.DestinationFolder}");
                    exitCode = UnpackTorrentRecursively(torrent);
                }

                if (exitCode == 0)
                {
                    _logger.Debug($" - Process OK!");
                    return true;
                }
                else
                {
                    _logger.Error($" - Error! When unpacking: {torrent.Name}. ExitCode from unpacker was: {exitCode}. Investigate log for details, see Warnings.");
                    Directory.Delete(torrent.DestinationFolder, true);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($" - Error! Exception {ex.Message} - when copying or unpacking: {torrent.Name}. Investigate log for details.");
                Directory.Delete(torrent.DestinationFolder, true);
                return false;
            }
        }

        private int UnpackTorrentRecursively(Torrent torrent)
        {
            // We're working with either a folder or a single file archive.
            var exitCode = 0;

            var allArchivesFirstFile = _fileManager.GetAllFirstFileArchivesRecursive(torrent.Path);

            foreach (var fileFirstInArchive in allArchivesFirstFile)
            {
                var torrentFolderName = torrent.Path;
                // Get only the subfolder path (not the entire path) so that we know where to copy to (a new folder in the destination)
                var sourceFileSubFolder = fileFirstInArchive.DirectoryName
                    .Replace(torrentFolderName, string.Empty)
                    .Trim('\\')
                    ;
                var destinationFolder = Path.Combine(torrent.DestinationFolder, sourceFileSubFolder);

                var unpackCommand = _config.UnpackerParameters
                    .Replace("[Archive]", $"\"{fileFirstInArchive.FullName}\"")
                    .Replace("[DestinationFolder]", $"\"{destinationFolder}\"")
                    .Replace(@"\\", @"\");
                exitCode += ExecuteUnpackCommand(unpackCommand);
            }
            return exitCode;
        }

        private int UnpackSingleArchive(Torrent torrent, string destinationDir, FileInfo sourceFile)
        {
            var unpackCommand = _config.UnpackerParameters
                .Replace("[Archive]", $"\"{sourceFile.FullName}\"")
                .Replace("[DestinationFolder]", $"\"{destinationDir}\"")
                .Replace(@"\\", @"\");

            return ExecuteUnpackCommand(unpackCommand);
        }

        private int ExecuteUnpackCommand(string unpackCommand)
        {
            var exitCode = 0;
            var startInfo = new ProcessStartInfo(_config.UnpackerExeFileFullPath, unpackCommand);
            startInfo.CreateNoWindow = _config.UnpackerHideWindow;
            startInfo.RedirectStandardOutput = _config.UnpackerHideWindow;
            startInfo.RedirectStandardError = _config.UnpackerHideWindow;
            startInfo.RedirectStandardInput = _config.UnpackerHideWindow;
            startInfo.UseShellExecute = !_config.UnpackerHideWindow;

            var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    _logger.Debug(args.Data);
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (string.IsNullOrEmpty(args?.Data?.Trim().Trim('.')))
                    _logger.Warn(args.Data);
            };

            _logger.Debug($" - Starting process: {_config.UnpackerExeFileFullPath} {unpackCommand}");

            process.Start();
            if (_config.UnpackerHideWindow)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

            }
            process.WaitForExit();
            exitCode += process.ExitCode;

            return exitCode;
        }
    }
}