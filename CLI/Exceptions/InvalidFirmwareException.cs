using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace boschfwtool.Exceptions
{
    class InvalidFirmwareException : Exception
    {
        public InvalidFirmwareException(string message) : base(message)
        {
        }

        public InvalidFirmwareException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidFirmwareException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
