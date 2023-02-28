using System;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Exception thrown when unable to connect to Vivox server.
    /// </summary>
    public class VivoxConnectionException : Exception
    {
        /// <summary>
        /// Creates an instance of VivoxConnectionException.
        /// </summary>
        /// <param name="message">The error message.</param>
        public VivoxConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates an instance of VivoxConnectionException.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public VivoxConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
