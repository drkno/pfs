using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Pfs.Plex;

namespace Pfs.Fuse
{
    public class FusePlexInterface : FileSystem
    {
        private Configuration _config;
        // private IDictionary<string, long> _pathToFdMap;
        // private IDictionary<long, string> _fdToPathMap;
        private Random _random;

        public FusePlexInterface(Configuration config) : base(config)
        {
            this._config = config;
            // this._pathToFdMap = new Dictionary<string, long>();
            // this._fdToPathMap = new Dictionary<long, string>();
            this._random = new Random();
        }

        private string _normalisePath(string path)
        {
            return Path.GetFullPath(path).Replace("\\", "").Trim();
        }

        // private long _getFd(string path)
        // {
        //     path = this._normalisePath(path);
        //     if (this._pathToFdMap.ContainsKey(path)) {
        //         return this._pathToFdMap[path];
        //     }
        //     long fd;
        //     do {
        //         fd = this._random.Next(int.MinValue, int.MaxValue);
        //     }
        //     while(this._fdToPathMap.ContainsKey(fd));
        //     this._pathToFdMap[path] = fd;
        //     this._fdToPathMap[fd] = path;
        //     return fd;
        // }

        public FuseStatusCode release(string path, long fd)
        {
            // var p = this._fdToPathMap[fd];
            // this._fdToPathMap.Remove(fd);
            // this._pathToFdMap.Remove(p);
            return FuseStatusCode.Success;
        }

        public FuseStatusCode releasedir(string path, long fd)
        {
            return this.release(path, fd);
        }

        public async Task<FuseAttributes> getattr(string path)
        {
            var file = await this.GetFile(this._normalisePath(path));
            if (file == null) {
                throw new FuseException(FuseStatusCode.ENOENT);
            }
            return new FuseAttributes(file, this._config);
        }

        public async Task<FuseAttributes> fgetattr(string path, long fd)
        {
            // var p = this._fdToPathMap[fd] ?? this._normalisePath(path);
            var p = this._normalisePath(path);
            return await this.getattr(p);
        }

        private async Task<long> _BaseOpen(string path, long flags, FileType type) 
        {
            path = this._normalisePath(path);
            if ((flags & 3) != 0) {
                // this fs is read only, reject
                throw new FuseException(FuseStatusCode.EACCES);
            }
            var file = await this.GetFile(path);
            if (file == null) {
                throw new FuseException(FuseStatusCode.ENOENT);
            }
            if (type == FileType.Folder && file.Type == FileType.File) {
                throw new FuseException(FuseStatusCode.ENOTDIR);
            }
            else if (type == FileType.File && file.Type == FileType.Folder) {
                throw new FuseException(FuseStatusCode.EISDIR);
            }
            return (long) FuseStatusCode.Success;
        }

        public async Task<long> open(string path, long flags) {
            return await this._BaseOpen(path, flags, FileType.File);
        }

        public async Task<long> opendir(string path, long flags) {
            return await this._BaseOpen(path, flags, FileType.Folder);
        }

        public async Task<string[]> readdir(string path) {
            var files = await this.ListFiles(this._normalisePath(path));
            return files.Select(f => f.Name).ToArray();
        }

        // async read(path, fd, buffer, length, position) {
        //     try {
        //         const p = this._fdMap[fd] || this._normalisePath(path);
        //         return await this.openFile(p, position, length, buffer);
        //     }
        //     catch(e) {
        //         if (e.message === 'No such file') {
        //             return ENOENT;
        //         }
        //         throw e;
        //     }
        // }
    }
}
