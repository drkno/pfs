const { basename, dirname, normalize, sep } = require('path');
const BaseClient = require('./client');
const ServersClient = require('./apis/servers');
const SectionsClient = require('./apis/sections');
const MetadataClient = require('./apis/metadata');

class FileSystem {
    constructor(config) {
        this._config = config;
        this._client = new BaseClient(config);
        this._servers = new ServersClient(this._client);
        this._sections = new SectionsClient(this._client);
        this._metadata = new MetadataClient(this._client);
    }

    async listFiles(inputPath) {
        // todo: this code assumes that names of path components are unique, which they may not be

        const normPath = normalize(inputPath);
        const spl = normPath.split(sep).filter(s => s.trim() !== '') || [];
        
        const servers = await this._servers.listServers();
        if (spl.length === 0) {
            return servers;
        }
        const server = servers.filter(s => s.name === spl[0])[0];

        const rootSections = await this._sections.listSections(server);
        if (spl.length === 1) {
            return rootSections;
        }
        const rootSection = rootSections.filter(s => s.name === spl[1])[0];

        let lastSection = rootSection;
        let sections;
        for (let i = 2; i <= spl.length; i++) {
            sections = await this._sections.listSectionItems(lastSection);
            if (i < spl.length) {
                lastSection = sections.filter(s => s.name === spl[i])[0];
            }
        }
        return sections;
    }

    async openFile(inputPath) {
        const files = await this.listFiles(dirname(inputPath));
        const fileName = basename(inputPath);

        const metadata = await this._metadata.getMetadata();
    }
}

module.exports = FileSystem;
