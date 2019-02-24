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

module.exports = ExtendedPromise;
