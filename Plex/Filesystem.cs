using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pfs;
using Pfs.Plex.Api;
using Pfs.Plex.Model;

namespace Pfs.Plex
{
    public class FileSystem
    {
        private readonly char[] _separators = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar};

        private readonly ServersClient _servers;
        private readonly SectionsClient _sections;
        private readonly FileClient _file;
        private readonly Memoriser _memoriser;

        protected FileSystem(Configuration config)
        {
            var client = new BaseApiClient(config);
            this._servers = new ServersClient(client);
            this._sections = new SectionsClient(client);
            this._file = new FileClient(client);
            this._memoriser = new Memoriser(config);
        }

        public async Task<IEnumerable<Node>> ListFiles(string inputPath)
        {
            return await this._memoriser.Memorise(this._InternalListFiles, inputPath);
        }

        private async Task<IEnumerable<Node>> _InternalListFiles(string inputPath)
        {
            // todo: this code assumes that names of path components are unique, which they may not be

            var spl = inputPath.Split(_separators)
                .Skip(1)
                .Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
                .ToList();
            
            var servers = await this._servers.ListServers();
            if (spl.Count == 0)
            {
                return servers;
            }
            var server = servers.FirstOrDefault(s => s.Name == spl[0]);

            var rootSections = await this._sections.ListSections(server);
            if (spl.Count == 1)
            {
                return rootSections;
            }
            var rootSection = rootSections.FirstOrDefault(s => s.Name == spl[1]);

            var lastSection = rootSection;
            IEnumerable<FileSystemNode> sections = null;
            for (var i = 2; i <= spl.Count; i++)
            {
                sections = await this._sections.ListSectionItems(lastSection);
                if (i < spl.Count)
                {
                    lastSection = sections.FirstOrDefault(s => s.Name == spl[i]);
                }
            }
            return sections;
        }

        public async Task<Node> GetFile(string inputPath)
        {
            return await this._memoriser.Memorise(this._InternalGetFile, inputPath);
        }

        private async Task<Node> _InternalGetFile(string inputPath)
        {
            if (inputPath.Length == 1 && _separators.Contains(inputPath[0]))
            {
                return new RootNode();
            }

            var (folder, file) = Utils.GetPathInfo(inputPath);
            var files = await this.ListFiles(folder);
            return files.FirstOrDefault(f => f.Name == file);
        }

        public async Task<long> OpenFile(string inputPath, long startIndex, byte[] outputBuffer)
        {
            var file = (await this.GetFile(inputPath)) as FileSystemNode;
            if (file == null)
            {
                throw new InvalidOperationException("No such file");
            }

            return await this._file.GetFileBuffer(file, startIndex, outputBuffer);
        }
    }
}