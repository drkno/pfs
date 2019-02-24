const { ExtendedPromise, cleanAndDedupe } = require('../utils');

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

        if (!servers.MediaContainer.Device) {
            if (servers.MediaContainer.size != '0' && process.env['DEBUG']) {
                console.error(`User should have access to ${sectionItems.MediaContainer.size} servers, but none were returned.`);
            }
            console.error('You do not have access to any plex servers!');
            return [];
        }

        const filtered = (await ExtendedPromise.all(servers.MediaContainer.Device
            .filter(d => d['$'].presence == '1')
            .map(async(d) => ({
                name: d['$'].name,
                createdAt: new Date(d['$'].createdAt * 1000),
                lastModified: new Date(d['$'].lastSeenAt * 1000),
                token: d['$'].accessToken,
                url: await this._findServer(d.Connection.map(u => u['$'].uri)),
                type: 'folder',
                id: Math.round(Math.random() * Number.MAX_SAFE_INTEGER)
            }))))
            .filter(d => !!d.url);
        
        return cleanAndDedupe(filtered);
    }
}

module.exports = ServersClient;
