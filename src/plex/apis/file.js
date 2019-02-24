class FileClient {
    constructor(client) {
        this._client = client;
    }

    async getFileBuffer(file, startIndex, numberOfBytes) {
        if (startIndex >= file.size) {
            // nothing to do, we have read 0 bytes
            return null;
        }

        const endIndex = Math.min(startIndex + numberOfBytes - 1, file.size - 1);

        return await this._client.bufferFetch(file.server.url, file.next, {
            'X-Plex-Token': file.server.token,
            'download': 1
        }, startIndex, endIndex);
    }
}

module.exports = FileClient;
