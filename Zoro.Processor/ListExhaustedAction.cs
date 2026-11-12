namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// What happens when a <c>ReplacementList</c> or <c>ReplacementQuery</c> mask has no replacement left for a value.
    /// </summary>
    public enum ListExhaustedAction
    {
        /// <summary>
        /// The value is masked with asterisks (see <c>FieldMask.Asterisk</c>). This is the default,
        /// so that it is visible in the output that the configuration needs more replacements.
        /// </summary>
        Asterisk = 0,

        /// <summary>
        /// A <c>ReplacementExhaustedException</c> is thrown.
        /// </summary>
        Error = 1,

        /// <summary>
        /// A replacement that is already used is picked randomly, so two values might share a replacement.
        /// </summary>
        Reuse = 2
    }
}
