Mounts all plex servers you have access to as local filesystems so that you can download/copy files.

See `config.example.json` for configuration options. Each of these options can either be set in `config.json` or passed as long parameters through stdin, e.g. `--mountPath /some/path`. If no plex token is set one will be retrieved on next run.