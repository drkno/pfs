const { parse } = require('path');

class SectionsClient {
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
        if (!section.server || !section.server.url || !section.next) {
            throw new Error('No such file or folder');
        }

        const sectionItems = await this._client.jsonFetch(section.server.url, section.next, {
            'X-Plex-Token': section.server.token
        });
        return sectionItems.MediaContainer.Metadata.flatMap(d => {
            if (d.Media) {
                const fileNameCache = {};
                return d.Media.flatMap(m => m.Part).map(p => {
                    const parsed = parse(p.file);
                    let addition = '';
                    let i = 1;
                    while (fileNameCache[parsed.name + addition + parsed.ext]) {
                        addition = ` - ${i}`;
                    }
                    const filename = parsed.name + addition + parsed.ext;
                    fileNameCache[filename] = true;

                    return {
                        server: section.server,
                        name: filename,
                        createdAt: new Date(d.addedAt * 1000),
                        lastModified: new Date(d.updatedAt * 1000),
                        partId: p.id,
                        next: p.key,
                        type: 'file',
                        size: p.size
                    }
                });
            }
            else {
                return [{
                    server: section.server,
                    name: d.title,
                    createdAt: new Date(d.addedAt * 1000),
                    lastModified: new Date(d.updatedAt * 1000),
                    sectionId: d.ratingKey,
                    next: d.key,
                    type: 'folder'
                }];
            }
        });
    }
}

module.exports = SectionsClient;
