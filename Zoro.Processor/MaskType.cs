
namespace Zoro.Processor
{
    /// <summary>
    /// Sets the type of masking done to a field.
    /// </summary>
    public enum MaskType
    {
        /// <summary>
        /// The field contents are not masked. This is the default.
        /// </summary>
        None,

        /// <summary>
        /// The field contents are substituted with different content, e.g. a different name or post code.
        /// </summary>
        Similar,

        /// <summary>
        /// The field contents are substituted with a character, e.g. asterisks or spaces.
        /// </summary>
        Asterisk,

        /// <summary>
        /// The field contents are substituted with a randomly picked item of a given list.
        /// </summary>
        List
    }
}