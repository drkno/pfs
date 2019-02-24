const ExtendedPromise = require('../../promise.js');

class ServersClient {
    constructor(client) {
        this._client = client;
    }

    async _findServer(uris) {
        try {
            return await ExtendedPromise.any(uris.map(u => this._client.headFetch(u)));
        } catch(e) {
            return void(0);
        }
    }

    async listServers() {
        const servers = await this._client.xmlFetch('https://plex.tv', '/api/resources', {
            includeHttps: 1,
            includeRelay: 1
        });

        const filtered = (await ExtendedPromise.all(servers.MediaContainer.Device
            .filter(d => d['$'].presence == '1')
            .map(async(d) => ({
                name: d['$'].name,
                createdAt: new Date(d['$'].createdAt * 1000),
                lastModified: new Date(d['$'].lastSeenAt * 1000),
                token: d['$'].accessToken,
                url: await this._findServer(d.Connection.map(u => u['$'].uri)),
                type: 'folder'
            }))))
            .filter(d => !!d.url);
        
        return filtered;
    }
}

module.exports = ServersClient;
