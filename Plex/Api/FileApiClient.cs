using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pfs.Plex.Model;

namespace Pfs.Plex.Api
{
    public class FileClient
    {
        private BaseApiClient _client;
        public FileClient(BaseApiClient client)
        {
            this._client = client;
        }

        public async Task<long> GetFileBuffer(FileSystemNode file, long startIndex, byte[] outputBuffer)
        {
            if (startIndex >= file.Size)
            {
                // nothing to do, we have read 0 bytes
                return 0;
            }

            var endIndex = Math.Min(startIndex + outputBuffer.Length - 1, file.Size - 1);

            return await this._client.BufferFetch(file.Server.Url, file.Next, new Dictionary<string, string>{
                { "X-Plex-Token", file.Server.Token },
                { "download", "1" }
            }, startIndex, endIndex, outputBuffer);
        }
    }
}
