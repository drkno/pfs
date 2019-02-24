const fuse = require('fuse-bindings');
const FileSystem = require('../plex/filesystem');

class FuseFilesystem extends FileSystem {
    constructor(config) {
        super(config);
        this._config = config;
    }
    
    async open(path, flags) {
        const files = await this.listFiles(path);
        return [0, 42];
    }

    async opendir(path, flags) {
        const files = await this.listFiles(path);
        return [0, 42];
    }

    async read() {

    }

    async readdir(path) {
        const files = await this.listFiles(path);
        return [0, files.map(f => f.name)];
    }
}

module.exports = FuseFilesystem;