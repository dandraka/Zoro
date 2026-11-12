
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
        None = 0,

        /// <summary>
        /// The field contents are substituted with random content. 
        /// Letters are substituted with a random letter of the same case, vowels with vowels and consonants with consonants. 
        /// Numbers are substituted with a random number. 
        /// Symbols, auch as - + % etc are left as is.
        /// From v.3.1 we assign a synonym: "Random" is the new "Similar" (because it is more intuitive).
        /// Similar is kept for backwards compatibility.
        /// </summary>
        Random = 1,

        /// <summary>
        /// Same as Random, kept for backwards compatibility.
        /// </summary>        
        Similar = 1,

        /// <summary>
        /// The field contents are substituted with a character, e.g. asterisks or spaces.
        /// </summary>
        Asterisk = 2,

        /// <summary>
        /// The field contents are substituted with a combination of a constant string and values from other fields.
        /// The FieldMask.Expression field is mandatory and must be filled with a constant string and
        /// field names enclosed in double curly brackets.
        /// For example "Customer-{{CustomerID}}" (without the quotes).
        /// When the data source is Json, a JsonPath is expected in the place of field name. The JsonPath
        /// will be applied on the root of the Json.
        /// For example "Customer-{{$.CustomerID}}" (without the quotes).
        /// Note: for Office input types (docx, xlsx, pptx), this masking type is not supported.
        /// </summary>
        Expression = 3,        

        /// <summary>
        /// The field contents are substituted with a randomly picked item of a given list.
        /// </summary>
        List = 4,

        /// <summary>
        /// The field contents are substituted with a randomly picked item from the result of q query.
        /// The query must return only one field.
        /// </summary>
        Query = 5,

        /// <summary>
        /// Like <c>List</c>, but the masking is consistent (pseudonymisation): the same original value
        /// always gets the same replacement, and different values get different replacements.
        /// Replacements are either given explicitly (<c>Replacement.Mappings</c>) or taken randomly
        /// from the items of the list that are not used yet. Matching ignores case and surrounding spaces.
        /// When there are no replacements left, <c>FieldMask.OnListExhausted</c> decides what happens.
        /// </summary>
        ReplacementList = 6,

        /// <summary>
        /// Like <c>Query</c>, but the masking is consistent (pseudonymisation), see <c>ReplacementList</c>.
        /// If <c>QueryReplacement.KeyField</c> is filled, the query is a mapping table of original value to
        /// replacement; otherwise the query returns the pool of replacements to pick from.
        /// </summary>
        ReplacementQuery = 7
    }
}