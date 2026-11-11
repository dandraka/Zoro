using System;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Exception raised when a value has no replacement left and <c>FieldMask.OnListExhausted</c> is Error.
    /// </summary>
    public class ReplacementExhaustedException : Exception
    {
        /// <summary>
        /// Creates a ReplacementExhaustedException.
        /// </summary>
        public ReplacementExhaustedException()
            : base(DefaultMessage)
        {
        }

        /// <summary>
        /// Creates a ReplacementExhaustedException with a message.
        /// </summary>
        public ReplacementExhaustedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates a ReplacementExhaustedException with a message and an inner exception.
        /// </summary>
        public ReplacementExhaustedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        // The original value is left out on purpose, it is sensitive data.
        private const string DefaultMessage = "A value has no replacement left. Add more items or mappings, or set OnListExhausted to Asterisk or Reuse.";
    }
}
