using System;

namespace Pfs.Fuse
{
    public class FuseException : Exception
    {
        public FuseStatusCode ErrorCode { get; }
        public FuseException(FuseStatusCode errorCode)
        {
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return "Error = " + ErrorCode;
        }
    }
}