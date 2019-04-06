using System;
using System.Threading.Tasks;
using Pfs.Fuse;
using Pfs.Plex;

namespace Pfs
{
    public static class Program
    {
        private static bool _terminateReceived;
        private static FuseFileSystem _fs;

        private static void HandleFatalException(Exception ex)
        {
            if (Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.WriteLine(ex?.Message);
                Console.WriteLine(ex?.StackTrace);
                Console.ReadKey(true);
            }
            if (_terminateReceived)
            {
                Environment.Exit(1);
            }
            _terminateReceived = true;
            try
            {
                if (!IsWindows())
                {
                    _fs?.UnMount();
                }
                Environment.Exit(1);
            }
            catch(Exception e)
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.ReadKey(true);
                }
                Environment.Exit(1);
            }
        }

        private static bool IsWindows()
        {
            var env = Environment.OSVersion.Platform;
            return env == PlatformID.Win32NT ||
                   env == PlatformID.Win32S ||
                   env == PlatformID.Win32Windows ||
                   env == PlatformID.WinCE ||
                   env == PlatformID.Xbox;
        }

        private static void WindowsMain()
        {
            string line;
            while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
            {
                Console.WriteLine(_fs.TestInput(line));
            }
        }

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleFatalException(e.ExceptionObject as Exception);

            var config = Configuration.LoadConfig();
            if (string.IsNullOrWhiteSpace(config.Cid) || string.IsNullOrWhiteSpace(config.Token))
            {
                var (cid, token) = await PlexOAuth.GetLoginDetails();
                config.Cid = cid;
                config.Token = token;
                if (config.SaveLoginDetails)
                {
                    Configuration.SaveConfig(config);
                }
            }

            try
            {
                _fs = new FuseFileSystem(config);
                if (IsWindows())
                {
                    WindowsMain();
                }
                else
                {
                    _fs.Mount();
                }
            }
            catch (Exception e)
            {
                HandleFatalException(e);
            }
        }
    }
}
