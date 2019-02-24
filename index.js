const { loadConfig, saveConfig } = require('./src/config');
const getLoginDetails = require('./src/plex/auth');
const FuseFileSystem = require('./src/fuse/decorator');

let fs;
(async() => {
    let config = await loadConfig();
    if (!config.cid || !config.token) {
        const loginCfg = await getLoginDetails();
        if (config.saveLoginDetails !== 'false') {
            saveConfig(loginCfg);
        }
        config = Object.assign({}, config, loginCfg);
    }

    fs = new FuseFileSystem(config);
    await fs.mount();
})();

let terminateReceived = false;
const terminateProcess = async() => {
    if (terminateReceived) {
        process.exit(1);
    }
    terminateReceived = true;
    try {
        await fs.unmount();
        process.exit(0);
    }
    catch(e) {
        if (process.env['DEBUG']) {
            console.error(e.stack);
        }
        process.exit(1);
    }
};

const handleCriticalError = err => {
    if (terminateReceived) {
        process.exit(1);
    }
    if (process.env['DEBUG']) {
        console.error(err.stack);
    }
    else {
        console.error('Terminating due to critical error');
    }
    terminateProcess();
};

process.on('uncaughtException', handleCriticalError);
process.on('unhandledRejection', handleCriticalError);
process.on('SIGINT', terminateProcess);
process.on('exit', terminateProcess);
