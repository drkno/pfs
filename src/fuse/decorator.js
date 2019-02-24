const fuse = require('fuse-bindings');
const { promisify } = require('util');
const FuseFilesystem = require('./fuse');

const mount = promisify(fuse.mount.bind(fuse));
const unmount = promisify(fuse.unmount.bind(fuse));

/**
 * Turn all fuse callback based methods into async method calls on a class.
 */
class FuseAsyncDecorator {
    constructor(config) {
        this._config = config;
        this._delegate = new FuseFilesystem(config);

        const ops = [
            'init',
            'access',
            'statfs',
            'getattr',
            'fgetattr',
            'flush',
            'fsync',
            'fsyncdir',
            'readdir',
            'truncate',
            'ftruncate',
            'readlink',
            'chown',
            'chmod',
            'mknod',
            'setxattr',
            'listxattr',
            'removexattr',
            'open',
            'opendir',
            'read',
            'write',
            'release',
            'releasedir',
            'create',
            'utimens',
            'unlink',
            'rename',
            'link',
            'symlink',
            'mkdir',
            'rmdir',
            'destroy'
        ];
        
        for (let arg of ops) {
            if (this._delegate[arg]) {
                this[arg] = (...args) => {
                    this._shadow(arg, args);
                };
            }
        }
    }

    async _shadow(funcName, args) {
        const callback = args.splice(args.length - 1, 1)[0];
        try {
            const results = await this._delegate[funcName].apply(this._delegate, args);
            callback.apply(this, results);
        } catch (e) {
            console.error(e);
            callback(fuse.EIO);
        }
    }

    async mount() {
        try {
            const opts = {
                options: [],
                displayFolder: false,
                force: false
            };
            if (!this._config.mountPath) {
                throw new Error('mountPath must be specified');
            }
            await mount(this._config.mountPath, opts, this);
        }
        catch(e) {
            console.error(`Mounting failed due to an error: ${e.message}`);
            throw e;
        }
    }

    async unmount() {
        try {
            if (!this._config.mountPath) {
                // it was not possible to mount without this variable set
                return;
            }
            await unmount(this._config.mountPath);
        }
        catch(e) {
            console.error(`Unmounting failed due to an error: ${e.message}`);
            throw e;
        }
    }
}

module.exports = FuseAsyncDecorator;
