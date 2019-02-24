const { cleanAndDedupe } = require('../utils');

class SectionsClient {
    constructor(client) {
        this._client = client;
    }

    async listSections(server) {
        const sections = await this._client.jsonFetch(server.url, '/library/sections', {
            'X-Plex-Token': server.token
        });

        if (!sections.MediaContainer.Directory) {
            if (process.env['DEBUG'] && sections.MediaContainer.size != '0') {
                console.error(`User should have access to ${sections.MediaContainer.size} sections, but none were returned.`);
            }
            return [];
        }

        return cleanAndDedupe(sections.MediaContainer.Directory.map(d => ({
            server,
            name: d.title,
            createdAt: new Date(d.createdAt * 1000),
            lastModified: new Date(d.updatedAt * 1000),
            id: d.key,
            type: 'folder',
            next: `/library/sections/${d.key}/all`
        })));
    }

    async listSectionItems(section) {
        if (!section.server || !section.server.url || !section.next) {
            throw new Error('No such file or folder');
        }

        const sectionItems = await this._client.jsonFetch(section.server.url, section.next, {
            'X-Plex-Token': section.server.token
        });

        if (!sectionItems.MediaContainer.Metadata) {
            if (process.env['DEBUG'] && sectionItems.MediaContainer.size != '0') {
                console.error(`No items were returned from plex when there should have been ${sectionItems.MediaContainer.size} items.`);
            }
            return [];
        }

        return cleanAndDedupe(sectionItems.MediaContainer.Metadata.flatMap(d => {
            if (d.Media) {
                return d.Media.flatMap(m => m.Part).map(p => {
                    return {
                        server: section.server,
                        name: p.file,
                        createdAt: new Date(d.addedAt * 1000),
                        lastModified: new Date(d.updatedAt * 1000),
                        id: p.id,
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
                    id: d.ratingKey,
                    next: d.key,
                    type: 'folder'
                }];
            }
        }));
    }
}

module.exports = SectionsClient;
