using System;
using FluentValidation;

namespace Rille.uTorrent.Extensions.PostProcess.Model
{
    public class Config
    {
        public string UnpackedFolder { get; set; } = "Z:\\";

        /// <summary>
        /// ie: c:\program\7zip\7z.exe
        /// </summary>
        public string ZipExeFullpath { get; set; }

        /// <summary>
        /// http://www.dotnetperls.com/7-zip-examples
        /// e   = extract (preserve paths)
        /// -y   = silent
        /// -ao  = overwrite files
        /// </summary>
        public string ZipSwitches { get; set; } = "x -y -ao";

        public string[] ArchiveFirstFilePatterns { get; set; } = { "*.rar", "*.001", "*.zip" };

        /// <summary>
        /// ie: http://localhost:8080
        /// </summary>
        public string ApiUrl { get; set; } = "http://7.150.174.30:8080/";

        public string ApiLogin { get; set; } = "admin";

        public string ApiPassword { get; set; } = "Nachos114";
    }

    public class ConfigValidator : AbstractValidator<Config>
    {
        public ConfigValidator()
        {
            RuleFor(p => p.ApiLogin).NotEmpty();
            RuleFor(p => p.ApiPassword).NotEmpty();
            RuleFor(p => p.ApiUrl).NotEmpty();
            RuleFor(p => p.ArchiveFirstFilePatterns).NotEmpty();
            RuleFor(p => p.UnpackedFolder).NotEmpty();
            RuleFor(p => p.ZipExeFullpath).NotEmpty();
            RuleFor(p => p.ZipSwitches).NotEmpty();

            RuleFor(p => p.ApiUrl)
                .Must(BeValidUri)
                .WithMessage("Invalid format. Example: http://localhost:111/some/api");

            RuleFor(p => p.ZipExeFullpath)
                .Must(FileExists)
                .WithMessage("Didnt exist at the configured path.");

            RuleFor(p => p.UnpackedFolder)
                .Must(FolderExists)
                .WithMessage("Didnt exist at the configured path.");
        }

        private bool FolderExists(string arg)
        {
            return System.IO.Directory.Exists(arg);
        }

        private bool FileExists(string arg)
        {
            return System.IO.File.Exists(arg);
        }

        private bool BeValidUri(string arg)
        {
            Uri outUri;
            return System.Uri.TryCreate(arg, UriKind.Absolute, out outUri);
        }
    }
}