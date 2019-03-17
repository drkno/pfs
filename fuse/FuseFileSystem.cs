using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Fuse;
using Mono.Fuse.NETStandard;
using Mono.Unix.Native;
using pfs;

namespace Pfs.Fuse
{
    public class FuseFileSystem : FileSystem
    {
        private Configuration _config;
        private FusePlexInterface _delegate;
        private Memoriser _memoriser;

        public FuseFileSystem(Configuration config)
        {
            this._config = config;
            this._delegate = new FusePlexInterface(config);
            this._memoriser = new Memoriser(config);
        }

        protected override Errno OnReleaseHandle(string file, OpenedPathInfo info)
        {
            try
            {
                return (Errno) this._memoriser.Memorise(this._delegate.release, file, info.Handle.ToInt64());
            }
            catch (FuseException e)
            {
                return (Errno) e.ErrorCode;
            }
        }

        protected override Errno OnReleaseDirectory(string directory, OpenedPathInfo info)
        {
            try
            {
                return (Errno) this._memoriser.Memorise(this._delegate.releasedir, directory, info.Handle.ToInt64());
            }
            catch (FuseException e)
            {
                return (Errno) e.ErrorCode;
            }
        }
        
        protected override Errno OnOpenHandle(string file, OpenedPathInfo info)
        {
            try
            {
                return (Errno) this._memoriser.Memorise(this._delegate.open, file, (long) info.OpenFlags).Result;
            }
            catch (FuseException e)
            {
                return (Errno) e.ErrorCode;
            }
        }
        
        protected override Errno OnOpenDirectory(string directory, OpenedPathInfo info)
        {
            try
            {
                return (Errno) this._memoriser.Memorise(this._delegate.opendir, directory, (long) info.OpenFlags).Result;
            }
            catch (FuseException e)
            {
                return (Errno) e.ErrorCode;
            }
        }

        protected override Errno OnGetPathStatus(string path, out Stat stat) {
            return OnGetHandleStatus(path, null, out stat);
        }

        protected override Errno OnGetHandleStatus(string file, OpenedPathInfo info, out Stat buf)
        {
            try
            {
                var result = this._memoriser.Memorise(this._delegate.getattr, file).Result;
                buf = new Stat() {
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
            catch (FuseException e)
            {
                buf = new Stat();
                return (Errno) e.ErrorCode;
            }
        }

        protected override Errno OnReadDirectory(string directory, OpenedPathInfo info, out IEnumerable<DirectoryEntry> paths)
        {
            try
            {
                paths = this._memoriser.Memorise(this._delegate.readdir, directory).Result.Select(name => new DirectoryEntry(name));
                return (Errno) FuseStatusCode.Success;
            }
            catch (FuseException e)
            {
                paths = new List<DirectoryEntry>();
                return (Errno) e.ErrorCode;
            }
        }

        // protected override OnReadHandle(string file, OpenedPathInfo info, byte[] buf, long offset, out int bytesWritten);

        public void Mount()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this._config.MountPath))
                {
                    throw new Exception("mountPath must be specified");
                }

                this.MultiThreaded = true;
                this.MountPoint = this._config.MountPath;
                this.ParseFuseArguments(this._config.FuseOptions);
                this.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Mounting failed due to an error: {e.Message}");
                throw e;
            }
        }

        public void Unmount()
        {
            try
            {
                this.Stop();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Unmounting failed due to an error: {e.Message}");
                throw e;
            }
        }
    }
}

