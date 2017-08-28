using System;
using Rille.uTorrent.Extensions.PostProcess.Model;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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

        /// <summary>
        /// If single volume archive, return one.
        /// If multi volume archive, return first file of the series.
        /// </summary>
        public List<FileInfo> GetFolderArchivesFirstFile(string path)
        {
            var retur = new List<FileInfo>();
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                //Then this is a single archive file.
                retur.Add(new FileInfo(path));
                return retur;
            }
            else
            {
                var files = dir.EnumerateFiles("*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    foreach (var supportedArchive in _config.ArchiveFirstFilePossibleFileExtensions)
                    {
                        if (file.Extension.ToLower().Substring(1) == supportedArchive)
                            retur.Add(file);
                    }
                }
                return retur;
            }
        }
    }
}