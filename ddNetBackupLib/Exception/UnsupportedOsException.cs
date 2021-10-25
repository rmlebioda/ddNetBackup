using System;
using System.Runtime.Serialization;

namespace ddNetBackupLib.Exception
{
    [Serializable]
    public class UnsupportedOsException : System.Exception
    {
        public UnsupportedOsException(PlatformID platformId) : base($"Unsupported OS platform: {platformId}")
        {
        }
        
        public UnsupportedOsException()
        {
        }

        protected UnsupportedOsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public UnsupportedOsException(string? message) : base(message)
        {
        }

        public UnsupportedOsException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}