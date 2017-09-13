using System;
using System.Threading;

namespace Rille.uTorrent.Extensions.PostProcess.Hidden
{
    class Program
    {
        static Mutex mutex = new Mutex(false, "https://github.com/rille111/Rille.uTorrent.Extensions");

        /// <summary>
        /// If there is no console output it's because the project type is set to Windows Application.
        /// Change to console if you want, for example debug, or need more feedback when running.
        /// </summary>
        static void Main(string[] args)
        {
            // Wait 5 seconds if contended – in case another instance
            // of the program is in the process of shutting down.
            if (!mutex.WaitOne(TimeSpan.Zero, false))
            {
                return;
            }
            try
            {
                var runner = new Runner();
                var exitCode = runner.Run(args);
                runner.ExitApp(exitCode);
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

    }
}
