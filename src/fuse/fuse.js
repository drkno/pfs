const { EACCES, ENOENT, ENOTDIR, EISDIR } = require('fuse-bindings');
const { normalize } = require('path');
const FileSystem = require('../plex/filesystem');

const DIRECTORY_MODE = 16676;
const FILE_MODE = 33133;

class FuseFilesystem extends FileSystem {
    constructor(config) {
        super(config);
        this._config = config;
        this._fdMap = {};
    }

    _normalisePath(path) {
        return normalize(path).replace(/\\/g, '').trim();
    }

    _getFd(path) {
        path = this._normalisePath(path);
        if (this._fdMap[path]) {
            return this._fdMap[path];
        }
        let fd;
        do {
            fd = Math.round(Math.random() * Number.MAX_SAFE_INTEGER);
        }
        while(this._fdMap[fd]);
        this._fdMap[path] = fd;
        this._fdMap[fd] = path;
        return fd;
    }

    async release(_, fd) {
        const p = this._fdMap[fd];
        delete this._fdMap[p];
        delete this._fdMap[fd];
        return 0;
    }

    async releasedir(path, fd) {
        return await this.release(this._normalisePath(path), fd);
    }

    async getattr(path) {
        const file = await this.getFile(this._normalisePath(path));
        if (!file) {
            return ENOENT;
        }
        return [0, {
            mtime: file.lastModified,
            atime: file.createdAt,
            ctime: file.lastModified,
            nlink: 1,
            size: file.size || 0,
            mode: file.type === 'folder' ? DIRECTORY_MODE : FILE_MODE,
            uid: this._config.uid || process.getuid() || 0,
            gid: this._config.gid || process.getgid() || 0
        }];
    }

    async fgetattr(path, fd) {
        const p = this._fdMap[fd] || this._normalisePath(path);
        return await this.getattr(p);
    }

    async _baseOpen(path, flags, type) {
        path = this._normalisePath(path);
        if (flags & 3 !== 0) {
            // this fs is read only, reject
            return EACCES;
        }
        const file = this.getFile(path);
        if (!file) {
            return ENOENT;
        }
        if (type === 'folder' && file.type === 'file') {
            return ENOTDIR;
        }
        else if (type === 'file' && file.type === 'folder') {
            return EISDIR;
        }
        return [0, this._getFd(path)];
    }

    async open(path, flags) {
        return await this._baseOpen(path, flags, 'file');
    }

    async opendir(path, flags) {
        return await this._baseOpen(path, flags, 'folder');
    }

    async readdir(path) {
        const files = await this.listFiles(this._normalisePath(path));
        return [0, files.map(f => f.name)];
    }

    async read(path, fd, buffer, length, position) {
        try {
            const p = this._fdMap[fd] || this._normalisePath(path);
            return await this.openFile(p, position, length, buffer);
        }
        catch(e) {
            if (e.message === 'No such file') {
                return ENOENT;
            }
            throw e;
        }
    }
}

module.exports = FuseFilesystem;