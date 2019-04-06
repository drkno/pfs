using Mono.Unix.Native;

namespace Pfs.Fuse
{
    public enum FuseStatusCode
    {
        Success = 0,
        ENOENT = Errno.ENOENT,
        ENOTDIR = Errno.ENOTDIR,
        EISDIR = Errno.EISDIR,
        EACCES = Errno.EACCES
    }
}
