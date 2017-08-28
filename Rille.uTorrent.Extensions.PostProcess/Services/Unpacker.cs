using System;
using Rille.uTorrent.Extensions.PostProcess.Model;
using NLog;
using System.Diagnostics;
using System.IO;

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

        public bool UnpackAndCopy(Torrent torrent)
        {
            if (torrent.IsSingleFileButNotArchive)
            {
                _logger.Info($" - Copying single file (not archive) torrent.");

                var sourceFile = new FileInfo(torrent.Path);
                var newDir = Directory.CreateDirectory(torrent.DestinationFolder);
                File.Copy(torrent.Path, $"{torrent.DestinationFolder}\\{sourceFile.Name}");
                return true;
            }

            if (torrent.IsFolder)
            {
                // First copy the non-archive files here:
                _logger.Info($" - Copying non-archive files in folder.");

                var files = torrent.GetNonArchiveFiles();
                var subFolders = torrent.GetSubfolders();
                if (!Directory.Exists(torrent.DestinationFolder))
                    Directory.CreateDirectory(torrent.DestinationFolder);

                foreach (var file in files)
                {
                    File.Copy(file.FullName, $"{torrent.DestinationFolder}\\{file.Name}");
                }
                foreach (var subfolder in subFolders)
                {
                    Unpacker.DirectoryCopy(subfolder.FullName, $"{torrent.DestinationFolder}\\{subfolder.Name}", true);
                }
            }

            // Unpack!!
            _logger.Info($" - Unpacking {torrent.Path} to {torrent.DestinationFolder}");
            
            var exitCode = StartUnpackingProcess(torrent);

            if (exitCode == 0)
            {
                _logger.Info($" - Unpacked {torrent.Path} OK!");
                return true;
            }
            else
            {
                _logger.Error($" - Error! When unpacking {torrent.Path}. ExitCode from unpacker was: {exitCode}. Investigate log for details, see Warnings.");
                Directory.Delete(torrent.DestinationFolder, true);
                return false;
            }
        }

        private int StartUnpackingProcess(Torrent torrent)
        {
            // We're working with either a folder or a single file archive.
            var exitCode = 0;

            foreach (var item in _fileManager.GetFolderArchivesFirstFile(torrent.Path))
            {
                var unpackCommand = _config.UnpackerParameters
                    .Replace("[Archive]", $"\"{item.FullName}\"")
                    .Replace("[DestinationFolder]", $"\"{torrent.DestinationFolder}\"")
                    .Replace(@"\\", @"\");

                var startInfo = new ProcessStartInfo(_config.UnpackerExeFileFullPath, unpackCommand);
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.UseShellExecute = false;

                var process = new Process();
                process.StartInfo = startInfo;
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        _logger.Debug(args.Data);
                };
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        _logger.Warn(args.Data);
                };

                _logger.Debug($" - Starting process: {_config.UnpackerExeFileFullPath} {unpackCommand}");

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                exitCode += process.ExitCode;
            }
            return exitCode;
        }

        /// <summary>
        /// Directories the copy.
        /// </summary>
        /// <param name="sourceDirPath">The source dir path.</param>
        /// <param name="destDirName">Name of the destination dir.</param>
        /// <param name="isCopySubDirs">if set to <c>true</c> [is copy sub directories].</param>
        /// <returns></returns>
        public static void DirectoryCopy(string sourceDirPath, string destDirName, bool isCopySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirPath);
            DirectoryInfo[] directories = directoryInfo.GetDirectories();
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: "
                    + sourceDirPath);
            }
            DirectoryInfo parentDirectory = Directory.GetParent(directoryInfo.FullName);
            destDirName = System.IO.Path.Combine(parentDirectory.FullName, destDirName);

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = directoryInfo.GetFiles();

            foreach (FileInfo file in files)
            {
                string tempPath = System.IO.Path.Combine(destDirName, file.Name);

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                file.CopyTo(tempPath, false);
            }
            // If copying subdirectories, copy them and their contents to new location using recursive  function. 
            if (isCopySubDirs)
            {
                foreach (DirectoryInfo item in directories)
                {
                    string tempPath = System.IO.Path.Combine(destDirName, item.Name);
                    DirectoryCopy(item.FullName, tempPath, isCopySubDirs);
                }
            }
        }
    }
}