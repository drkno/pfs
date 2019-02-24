const opn = require('opn');
const fetch = require('node-fetch');

const sleep = async(timeout) => new Promise(resolve => setTimeout(resolve, timeout));

const PlexHeaders = {
    'Accept': 'application/json',
    'X-Plex-Product': 'PlexFS',
    'X-Plex-Version': 'PlexFS',
    'X-Plex-Client-Identifier': 'PlexFSv1'
};

class PlexOAuth {
    async _signIn() {
        const {pin, code} = await this._getPlexOAuthPin();
        const url = `https://app.plex.tv/auth/#!?clientID=${PlexHeaders['X-Plex-Client-Identifier']}&code=${code}`;
        try {
            await opn(url);
        }
        catch(e) {}
        console.log(`Please authenticate in your web browser. If your web browser did not open, please go to ${url}`);
        
        let token;
        while(true) {
            const response = await fetch(`https://plex.tv/api/v2/pins/${pin}`, {
                headers: PlexHeaders
            });

            const jsonData = await response.json();
            if (jsonData.authToken) {
                token = jsonData.authToken;
                break;
            }
            await sleep(1000);
        }
        return token;
    }

    async _getPlexOAuthPin() {
        const response = await fetch('https://plex.tv/api/v2/pins?strong=true', {
            method: 'POST',
            headers: PlexHeaders
        });
        const jsonData = await response.json();
        return {
            pin: jsonData.id,
            code: jsonData.code
        };
    }

    async performLogin() {
        return await this._signIn();
    }
}

module.exports = async() => {
    console.log('Login Required.');
    const oauthProvider = new PlexOAuth();
    const token = await oauthProvider.performLogin();
    console.log('Login Complete. Plex token received.');
    return {
        cid: PlexHeaders['X-Plex-Client-Identifier'],
        token
    };
};
