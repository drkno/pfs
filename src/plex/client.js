const fetch = require('node-fetch');
const { parseString } = require('xml2js');
const mem = require('mem');

class BaseClient {
    constructor(config) {
        this.token = config.token;
        this.cid = config.cid;
        const cacheOptions = { maxAge: config.cacheAge || 3600000 };
        this.jsonFetch = mem(this._jsonFetch.bind(this), cacheOptions);
        this.xmlFetch = mem(this._xmlFetch.bind(this), cacheOptions);
        this.headFetch = mem(this._headFetch.bind(this), cacheOptions);
    }

    _buildPlexUrl(server, path, params = {}) {
        params = Object.assign({}, {
            'X-Plex-Token': this.token,
            'X-Plex-Client-Identifier': this.cid
        }, params);
        return server +
            path +
            (Object.keys(params).length > 0 ? '?' : '') +
            (Object.keys(params).map(k => `${k}=${params[k]}`).join('&'));
    }

    async _jsonFetch(...args) {
        return await (await fetch(this._buildPlexUrl(...args), {
            headers: {
                'Accept': 'application/json'
            }
        })).json();
    }

    _toXml(xml) {
        return new Promise((resolve, reject) => {
            parseString(xml, (err, res) => err ? reject(err) : resolve(res));
        });
    }

    async _xmlFetch(...args) {
        return await this._toXml(await (await fetch(this._buildPlexUrl(...args), {
            headers: {
                'Accept': 'application/xml'
            }
        })).text());
    }

    async _headFetch(uri) {
        await fetch(uri, {method: 'HEAD'});
        return uri;
    }
}

module.exports = BaseClient;
