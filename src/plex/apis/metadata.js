class MetadataClient {
    constructor(client) {
        this._client = client;
    }

    async listSections(server) {
        const sections = await this._client.jsonFetch(server.url, '/library/sections', {
            'X-Plex-Token': server.token
        });
        return sections.MediaContainer.Directory.map(d => ({
            server,
            name: d.title,
            createdAt: new Date(d.createdAt * 1000),
            lastModified: new Date(d.updatedAt * 1000),
            sectionId: d.key,
            next: `/library/sections/${d.key}/all`
        }));
    }

    async listSectionItems(section) {
        const sectionItems = await this._client.jsonFetch(section.server.url, section.next, {
            'X-Plex-Token': section.server.token
        });
        sectionItems.MediaContainer.map

        sectionItems.MediaContainer.Metadata.flatmap(f => f.Media)
    }
}

module.exports = MetadataClient;
