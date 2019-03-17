using System;
using System.Collections.Generic;
using System.Linq;

namespace Pfs.Plex.Apis
{
    public class FileClient
    {
        private BaseClient _client;
        public FileClient(BaseClient client)
        {
            this._client = client;
        }

        // public async Task<byte[]> GetFileBuffer(Node file, long startIndex, long numberOfBytes)
        // {
        //     if (startIndex >= file.Size) {
        //         // nothing to do, we have read 0 bytes
        //         return null;
        //     }

        //     var endIndex = Math.Min(startIndex + numberOfBytes - 1, file.Size - 1);

        //     return await this._client.BufferFetch(file.Server.Url, file.Next, {
        //         { "X-Plex-Token", file.Server.Token },
        //         { "download", "1" }
        //     }, startIndex, endIndex);
        // }
    }
}
