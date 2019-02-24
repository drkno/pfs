const { parse } = require('path');

class ExtendedPromise extends Promise {
    static any(promises) {
        return Promise.all(promises.map(p => p.then(
            val => Promise.reject(val),
            err => Promise.resolve(err)
        )))
        .then(
            errors => Promise.reject(errors),
            val => Promise.resolve(val)
        );
    };
}

const cleanAndDedupe = items => {
    const fileNameCache = {};
    for (let item of items) {
        const parsed = parse(item.name.replace(/\/|\\/g, '_'));
        let addition = '';
        let i = 0;
        while (fileNameCache[parsed.name + addition + parsed.ext]) {
            if (i++ === 0) {
                addition = ` - (${item.id})`;
            }
            else {
                addition = ` - ${i}`;
            }
        }
        const filename = parsed.name + addition + parsed.ext;
        fileNameCache[filename] = true;
        item.name = filename;
    }
    return items;
};

module.exports = {
    ExtendedPromise,
    cleanAndDedupe
};
