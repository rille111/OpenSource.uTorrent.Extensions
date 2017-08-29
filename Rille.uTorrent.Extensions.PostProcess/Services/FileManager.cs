using System;
using Rille.uTorrent.Extensions.PostProcess.Model;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public class FileManager
    {
        private readonly Config _config;

        public FileManager(Config config)
        {
            _config = config;
        }

        public bool DoesFolderContainAnyArchive(string path)
        {
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
                return false;

            var files = dir.EnumerateFiles("*", SearchOption.AllDirectories);
            var distinctFileExtensions = files.Select(fi => fi.Extension.ToLower()).Distinct();
            foreach (var supportedArchive in _config.ArchiveFirstFilePossibleFileExtensions)
            {
                if (distinctFileExtensions.Contains($".{supportedArchive}"))
                    return true;
            }
            return false;
        }

        public bool IsArchiveFirstFileInVolume(FileInfo file)
        {
            foreach (var supportedArchive in _config.ArchiveFirstFilePossibleFileExtensions)
            {
                if (file.Extension.ToLower().Substring(1) == supportedArchive)
                    return true;
            }
            return false;
        }

        public bool IsPartOfArchiveVolume(FileInfo file)
        {
            var ext = file.Extension.ToLower().Substring(1); // remove the dot
            foreach (var pattern in _config.IsArchivePatterns)
            {
                if (Regex.IsMatch(ext, $"{pattern}"))
                {
                    // Is archive or part of.
                    return true;
                }
            }
            return false;
        }

        public bool IsPartOfIgnoredFileExtensions(FileInfo file)
        {
            var ext = file.Extension.ToLower().Substring(1); // remove the dot
            foreach (var pattern in _config.IgnoreFileExtensionPatterns)
            {
                if (Regex.IsMatch(ext, $"{pattern}"))
                {
                    // Is part of ignored extension
                    return true;
                }
            }
            return false;
        }

        public bool IsPartOfIgnoredFileNames(FileInfo file)
        {
            var fileName = file.Name.ToLower();
            foreach (var pattern in _config.IgnoreFileNamePatterns)
            {
                if (Regex.IsMatch(fileName, $"{pattern}"))
                {
                    // Is part of ignored file name
                    return true;
                }
            }
            return false;
        }

        public bool IsPartOfIgnoredFolderNames(DirectoryInfo folder)
        {
            foreach (var pattern in _config.IgnoreFolderPatterns)
            {
                if (Regex.IsMatch(folder.Name.ToLower(), $"{pattern}"))
                {
                    // Is part of ignored folder name
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// If single volume archive, return one.
        /// If multi volume archive, return first file of the series.
        /// </summary>
        public List<FileInfo> GetAllFirstFileArchivesRecursive(string pathToFileOrFolder)
        {
            var retur = new List<FileInfo>();
            var dir = new DirectoryInfo(pathToFileOrFolder);

            if (!dir.Exists)
            {
                //Then this is a file! Is it an archive?
                var file = new FileInfo(pathToFileOrFolder);
                if (IsArchiveFirstFileInVolume(file))
                    retur.Add(new FileInfo(pathToFileOrFolder));
                return retur;
            }
            else
            {
                var files = dir.EnumerateFiles("*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (IsArchiveFirstFileInVolume(file))
                        retur.Add(file);
                }
                return retur;
            }
        }

        /// <summary>
        /// Only the current folder. Looks only at files.
        /// </summary>
        public List<FileInfo> GetAllFirstFileArchives(string path)
        {
            var archives = new List<FileInfo>();
            var dir = new DirectoryInfo(path);

            var files = dir.EnumerateFiles();
            foreach (var file in files)
            {
                if (IsArchiveFirstFileInVolume(file))
                    archives.Add(file);
            }
            return archives;
        }

        public List<FileInfo> GetAllFilesNotPartOfArchiveVolume(string path)
        {
            var dir = new DirectoryInfo(path);
            var archives = new List<FileInfo>();

            var allfiles = dir.EnumerateFiles().ToList();

            foreach (var file in allfiles)
            {
                if (IsPartOfArchiveVolume(file))
                        archives.Add(file);
            }

            var normalFiles = allfiles.Except(archives);
            return normalFiles.ToList();
        }

        public List<FileInfo> GetIgnoredFiles(string path)
        {
            var dir = new DirectoryInfo(path);
            var ignoredFiles = new List<FileInfo>();

            var allfiles = dir.EnumerateFiles().ToList();

            foreach (var file in allfiles)
            {
                if (IsPartOfIgnoredFileExtensions(file) || IsPartOfIgnoredFileNames(file))
                    ignoredFiles.Add(file);
            }

            return ignoredFiles.ToList();
        }

        public List<DirectoryInfo> GetIgnoredFolders(string path)
        {
            var dir = new DirectoryInfo(path);
            var ignoredFolders = new List<DirectoryInfo>();

            var subFolders = dir.EnumerateDirectories().ToList();

            foreach (var folder in subFolders)
            {
                if (IsPartOfIgnoredFolderNames(folder))
                    ignoredFolders.Add(folder);
            }

            return ignoredFolders.ToList();
        }

        /// <summary>
        /// Both first file AND archive volume fileparts
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public List<FileInfo> GetAllArchiveFiles(string path)
        {
            var dir = new DirectoryInfo(path);
            var archives = new List<FileInfo>();
            if (!dir.Exists)
                return archives;

            var allfiles = dir.EnumerateFiles().ToList();
            foreach (var file in allfiles)
            {
                if (IsPartOfArchiveVolume(file))
                    archives.Add(file);
            }

            return archives.ToList();
        }

        /// <summary>
        /// Directories the copy.
        /// </summary>
        /// <param name="sourceDirPath">The source dir path.</param>
        /// <param name="destDirName">Name of the destination dir.</param>
        /// <param name="doCopySubdirs">if set to <c>true</c> [is copy sub directories].</param>
        /// <returns></returns>
        public void CopyFolderRecursivelyExceptIgnoredStuffAndArchiveFirstFiles(string sourceDirPath, string destDirName, bool doCopySubdirs)
        {
            DirectoryInfo thisDirInfo = new DirectoryInfo(sourceDirPath);

            // Get all subdirs for the specified directory.
            DirectoryInfo[] subDirs = thisDirInfo
                .GetDirectories().ToList()
                .RemoveFromList(GetIgnoredFolders(thisDirInfo.FullName))
                .ToArray();
            DirectoryInfo parentDir = Directory.GetParent(thisDirInfo.FullName);
            destDirName = Path.Combine(parentDir.FullName, destDirName).Replace(@"\\",@"\");

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = thisDirInfo
                .GetFiles().ToList()
                .RemoveFromList(GetIgnoredFiles(thisDirInfo.FullName))
                .RemoveFromList(GetAllFirstFileArchives(thisDirInfo.FullName))
                .ToArray();

            foreach (FileInfo file in files)
            {
                string targetPath = Path.Combine(destDirName, file.Name);

                // Skip existing files and archive first files
                if (!File.Exists(targetPath) && !IsArchiveFirstFileInVolume(file))
                {
                    file.CopyTo(targetPath, false);
                }
            }
            // If copying subdirectories, copy them and their contents to new location using recursive  function. 
            if (doCopySubdirs)
            {
                foreach (DirectoryInfo item in subDirs)
                {
                    string tempPath = Path.Combine(destDirName, item.Name);
                    CopyFolderRecursivelyExceptIgnoredStuffAndArchiveFirstFiles(item.FullName, tempPath, doCopySubdirs);
                }
            }
        }

        public void DeleteDirectoryRecurse(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectoryRecurse(dir);
            }

            Directory.Delete(path, false);
        }
    }

    public static class FileAndDirInfoExtensions
    {
        public static List<FileInfo> RemoveFromList(this List<FileInfo> source, List<FileInfo> removeThese)
        {
            foreach (var removeItem in removeThese)
            {
                var inSource = source.FirstOrDefault(p => p.FullName == removeItem.FullName);
                if (inSource != null)
                    source.Remove(inSource);
            }
            return source;
        }

        public static List<DirectoryInfo> RemoveFromList(this List<DirectoryInfo> source, List<DirectoryInfo> removeThese)
        {
            foreach (var removeItem in removeThese)
            {
                var inSource = source.FirstOrDefault(p => p.FullName == removeItem.FullName);
                if (inSource != null)
                    source.Remove(inSource);
            }
            return source;
        }

    }
}