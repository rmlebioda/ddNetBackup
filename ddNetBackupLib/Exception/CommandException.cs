using System;
using System.Runtime.Serialization;

namespace ddNetBackupLib.Exception
{
    [Serializable]
    public class CommandException : System.Exception
    {
        public CommandException()
        {
        }

        protected CommandException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CommandException(string? message) : base(message)
        {
        }

        public CommandException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}