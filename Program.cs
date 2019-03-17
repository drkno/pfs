using System;
using System.Threading.Tasks;
using Pfs.Fuse;
using Pfs.Plex;

namespace Pfs
{
    public static class Program
    {
        private static bool terminateReceived;
        private static FuseFileSystem fs;

        private static void HandleFatalException(Exception ex)
        {
            if (Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.WriteLine(ex?.Message);
                Console.WriteLine(ex?.StackTrace);
            }
            if (terminateReceived)
            {
                Environment.Exit(1);
            }
            terminateReceived = true;
            try
            {
                if (fs != null)
                {
                    fs.Unmount();
                }
                Environment.Exit(1);
            }
            catch(Exception e)
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                Environment.Exit(1);
            }
        }

        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleFatalException(e.ExceptionObject as Exception);

            var config = Configuration.LoadConfig();
            if (string.IsNullOrWhiteSpace(config.Cid) || string.IsNullOrWhiteSpace(config.Token))
            {
                (string cid, string token) = await PlexOAuth.GetLoginDetails();
                config.Cid = cid;
                config.Token = token;
                if (config.SaveLoginDetails)
                {
                    Configuration.SaveConfig(config);
                }
            }

            try
            {
                fs = new FuseFileSystem(config);
                fs.Mount();
            }
            catch (Exception e)
            {
                HandleFatalException(e);
            }
        }
    }
}
