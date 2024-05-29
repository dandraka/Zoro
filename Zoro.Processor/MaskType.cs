
namespace Dandraka.Zoro.Processor
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
        /// The field contents are substituted with a combination of a constant string and values from other fields.
        /// The FieldMask.Expression field is mandatory and must be filled with a constant string and
        /// field names enclosed in double curly brackets.
        /// For example "Customer-{{CustomerID}}" (without the quotes).
        /// When the data source is Json, a JsonPath is expected in the place of field name. The JsonPath
        /// will be applied on the root of the Json.
        /// For example "Customer-{{$.CustomerID}}" (without the quotes).
        /// </summary>
        Expression,        

        /// <summary>
        /// The field contents are substituted with a randomly picked item of a given list.
        /// </summary>
        List,

        /// <summary>
        /// The field contents are substituted with a randomly picked item from the result of q query.
        /// The query must return only one field.
        /// </summary>
        Query
    }
}