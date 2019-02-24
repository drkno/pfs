const { basename, dirname, sep } = require('path');
const mem = require('mem');
const BaseClient = require('./client');
const ServersClient = require('./apis/servers');
const SectionsClient = require('./apis/sections');
const FileClient = require('./apis/file');

class FileSystem {
    constructor(config) {
        this._config = config;
        this._client = new BaseClient(config);
        this._servers = new ServersClient(this._client);
        this._sections = new SectionsClient(this._client);
        this._file = new FileClient(this._client);

        const cacheOptions = { maxAge: config.cacheAge || 3600000 };
        this.listFiles = mem(this._listFiles.bind(this), cacheOptions);
        this.getFile = mem(this._getFile.bind(this), cacheOptions);
    }

    async _listFiles(inputPath) {
        // todo: this code assumes that names of path components are unique, which they may not be

        const spl = inputPath.split(sep).filter(s => s.trim() !== '') || [];
        
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

    async _getFile(inputPath) {
        if (inputPath === '/') {
            return {
                name: 'Plex',
                createdAt: new Date(),
                lastModified: new Date(),
                type: 'folder'
            };
        }

        const files = await this.listFiles(dirname(inputPath));
        const fileName = basename(inputPath);
        return files.filter(f => f.name === fileName)[0];
    }

    async openFile(inputPath, startIndex, numberOfBytes, outputBuffer) {
        const file = await this.getFile(inputPath);
        if (!file) {
            throw new Error('No such file');
        }

        const fileBuffer = await this._file.getFileBuffer(file, startIndex, numberOfBytes);
        if (fileBuffer === null) {
            return 0;
        }

        fileBuffer.copy(outputBuffer);
        return fileBuffer.length;
    }
}

module.exports = FileSystem;
