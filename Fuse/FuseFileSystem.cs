using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Fuse.NETStandard;
using Mono.Unix.Native;
using Newtonsoft.Json;
using pfs;

namespace Pfs.Fuse
{
    public class FuseFileSystem : FileSystem
    {
        private readonly Configuration _config;
        private readonly FusePlexInterface _delegate;
        private readonly Memoriser _memoriser;

        public FuseFileSystem(Configuration config)
        {
            _config = config;
            _delegate = new FusePlexInterface(config);
            _memoriser = new Memoriser(config);
        }

        protected override Errno OnReleaseHandle(string file, OpenedPathInfo info)
        {
            try
            {
                return (Errno) _memoriser.Memorise(_delegate.release, file, info.Handle.ToInt64());
            }
            catch (Exception e)
            {
                return HandleException(e);
            }
        }

        protected override Errno OnReleaseDirectory(string directory, OpenedPathInfo info)
        {
            try
            {
                return (Errno) _memoriser.Memorise(_delegate.releasedir, directory, info.Handle.ToInt64());
            }
            catch (Exception e)
            {
                return HandleException(e);
            }
        }
        
        protected override Errno OnOpenHandle(string file, OpenedPathInfo info)
        {
            try
            {
                return (Errno) _memoriser.Memorise(_delegate.open, file, (long) info.OpenFlags).Result;
            }
            catch (Exception e)
            {
                return HandleException(e);
            }
        }
        
        protected override Errno OnOpenDirectory(string directory, OpenedPathInfo info)
        {
            try
            {
                return (Errno) _memoriser.Memorise(_delegate.opendir, directory, (long) info.OpenFlags).Result;
            }
            catch (Exception e)
            {
                return HandleException(e);
            }
        }

        protected override Errno OnGetPathStatus(string path, out Stat stat)
        {
            return OnGetHandleStatus(path, null, out stat);
        }

        protected override Errno OnGetHandleStatus(string file, OpenedPathInfo info, out Stat buf)
        {
            try
            {
                var result = _memoriser.Memorise(_delegate.getattr, file).Result;
                buf = new Stat()
                {
                    st_atime = result.atime,
                    st_ctime = result.ctime,
                    st_mtime = result.mtime,
                    st_nlink = (ulong) result.nlink,
                    st_mode = (FilePermissions) result.mode,
                    st_size = result.size,
                    st_gid = (uint) result.gid,
                    st_uid = (uint) result.uid
                };
                return (Errno) FuseStatusCode.Success;
            }
            catch (Exception e)
            {
                buf = new Stat();
                return HandleException(e);
            }
        }

        protected override Errno OnReadDirectory(string directory, OpenedPathInfo info, out IEnumerable<DirectoryEntry> paths)
        {
            try
            {
                var results = _memoriser.Memorise(_delegate.readdir, directory).Result;
                paths = results.Select(name =>
                {
                    return new DirectoryEntry(name);
                });
                return (Errno) FuseStatusCode.Success;
            }
            catch (Exception e)
            {
                paths = new List<DirectoryEntry>();
                return HandleException(e);
            }
        }

        protected override Errno OnReadHandle(string file, OpenedPathInfo info, byte[] buf, long offset, out int bytesWritten)
        {
            try
            {
                bytesWritten = (int) _delegate.read(file, buf, offset).Result;
                return (Errno) FuseStatusCode.Success;
            }
            catch (Exception e)
            {
                bytesWritten = 0;
                return HandleException(e);
            }
        }

        private Errno HandleException(Exception e)
        {
            FuseException exception = null;
            if (e is FuseException fuseExceptionFirst)
            {
                exception = fuseExceptionFirst;
            }

            if (e is AggregateException aggregateException && aggregateException.GetBaseException() is FuseException fuseExceptionSecond)
            {
                exception = fuseExceptionSecond;
            }

            if (exception != null)
            {
                if (Environment.GetEnvironmentVariable("DEBUG") != null)
                {
                    Console.WriteLine(exception.Message);
                    Console.WriteLine(exception.StackTrace);
                }
                return (Errno) exception.ErrorCode;
            }

            if (Environment.GetEnvironmentVariable("DEBUG") != null)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return Errno.ENOENT;
        }

        public void Mount()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_config.MountPath))
                {
                    throw new Exception("mountPath must be specified");
                }

                MultiThreaded = true;
                MountPoint = _config.MountPath;
                ParseFuseArguments(_config.FuseOptions);
                Start();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Mounting failed due to an error: {e.Message}");
                throw;
            }
        }

        public void UnMount()
        {
            try
            {
                Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unmounting failed due to an error: {e.Message}");
                throw;
            }
        }

        public string TestInput(string path)
        {
            var err = OnGetHandleStatus(path, null, out var buf);
            if (err != 0)
            {
                return "Error = " + err;
            }

            var result = "Stat = " + JsonConvert.SerializeObject(buf);

            var isFile = (long) buf.st_mode == FuseAttributes.FILE_MODE;
            if (isFile)
            {

            }
            else
            {
                OnReadDirectory(path, null, out var paths);
                result += "Type = Folder\n";
                result += "Files = " + string.Join(", ", paths.Select(p => p.Name)) + "\n";
            }
            return result;
        }
    }
}

