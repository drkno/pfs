using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using pfs;
using Pfs.Plex.Apis;

namespace Pfs.Plex
{
    public class FileSystem
    {
        private Configuration _config;
        private BaseClient _client;
        private ServersClient _servers;
        private SectionsClient _sections;
        private FileClient _file;
        private Memoriser _memoriser;

        public FileSystem(Configuration config)
        {
            this._config = config;
            this._client = new BaseClient(config);
            this._servers = new ServersClient(this._client);
            this._sections = new SectionsClient(this._client);
            this._file = new FileClient(this._client);
            this._memoriser = new Memoriser(config);
        }

        public async Task<IEnumerable<BaseNode>> ListFiles(string inputPath)
        {
            return await this._memoriser.Memorise(this._InternalListFiles, inputPath);
        }

        private async Task<IEnumerable<BaseNode>> _InternalListFiles(string inputPath)
        {
            // todo: this code assumes that names of path components are unique, which they may not be

            var spl = inputPath.Split(Path.PathSeparator)
                .Skip(1)
                .Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
                .ToList();
            
            var servers = await this._servers.ListServers();
            if (spl.Count == 0)
            {
                return servers;
            }
            var server = servers.Where(s => s.Name == spl[0]).FirstOrDefault();

            var rootSections = await this._sections.ListSections(server);
            if (spl.Count == 1)
            {
                return rootSections;
            }
            var rootSection = rootSections.Where(s => s.Name == spl[1]).FirstOrDefault();

            var lastSection = rootSection;
            IEnumerable<Node> sections = null;
            for (var i = 2; i <= spl.Count; i++)
            {
                sections = await this._sections.ListSectionItems(lastSection);
                if (i < spl.Count)
                {
                    lastSection = sections.Where(s => s.Name == spl[i]).FirstOrDefault();
                }
            }
            return sections;
        }

        public async Task<BaseNode> GetFile(string inputPath)
        {
            return await this._memoriser.Memorise(this._InternalGetFile, inputPath);
        }

        private async Task<BaseNode> _InternalGetFile(string inputPath)
        {
            if (inputPath == "/")
            {
                return new Node()
                {
                    Name = "Plex",
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    Type = FileType.Folder
                };
            }

            var files = await this.ListFiles(Path.GetDirectoryName(inputPath));
            var fileName = Path.GetFileName(inputPath);
            return files.Where(f => f.Name == fileName).FirstOrDefault();
        }

        // async openFile(string inputPath, long startIndex, long numberOfBytes, outputBuffer)
        // {
        //     const file = await this.getFile(inputPath);
        //     if (!file) {
        //         throw new Error('No such file');
        //     }

        //     const fileBuffer = await this._file.getFileBuffer(file, startIndex, numberOfBytes);
        //     if (fileBuffer === null) {
        //         return 0;
        //     }

        //     fileBuffer.copy(outputBuffer);
        //     return fileBuffer.length;
        // }
    }
}
