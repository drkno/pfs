const { readFile, writeFile } = require('fs');
const { promisify } = require('util');

const readJson = async(file) => {
    try {
        const data = await promisify(readFile)(file, 'utf8');
        return JSON.parse(data.replace(/^\uFEFF/, ''));
    }
    catch(e) {
        return void(0);
    }
};

const readArgs = () => {
    const result = {};
    let key;
    let args = [];
    for (let i = 2; i <= process.argv.length; i++) {
        const cur = process.argv[i] || '-';
        if (cur.startsWith('-')) {
            if (key) {
                result[key] = args;
                args = [];
            }
            key = cur.replace(/^-+/g, '');
            if (result[key]) {
                throw new Error(`Duplicate argument '${key}' provided`);
            }
        }
        else if (!key) {
            throw new Error(`Value '${cur}' provided without an argument`);
        }
        else {
            args.push(cur);
        }
    }

    for (let k in result) {
        const val = result[k];
        for (let i = 0; i < val.length; i++) {
            if ((!isNaN(val[i]) && val[i] !== '') || val[i] === 'true' || val[i] === 'false') {
                val[i] = JSON.parse(val[i]);
            }
        }
        if (val.length === 0) {
            result[k] = true;
        }
        else if (val.length === 1) {
            result[k] = val[0];
        }
    }
    return result;
};

const saveJson = async(content, file) => {
    return await promisify(writeFile)(file, JSON.stringify(content, null, 4), 'utf8');
};

module.exports = {
    loadConfig: async(configFile = 'config.json') => {
        const cfgCli = readArgs();
        const cfgFile = await readJson(cfgCli.configFile || configFile);
        return Object.assign({}, cfgFile, cfgCli);
    },
    saveConfig: (content, configFile = 'config.json') => saveJson(content, configFile)
};
