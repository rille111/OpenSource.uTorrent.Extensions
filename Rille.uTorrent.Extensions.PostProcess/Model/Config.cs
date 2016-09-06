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
}