using System;

/// <summary>
/// Exception raised when a field is not found.
/// </summary>
public class FieldNotFoundException : Exception
{
    /// <summary>
    /// Creates a FieldNotFoundException.
    /// </summary>
    public FieldNotFoundException()
    {
    }

    /// <summary>
    /// Creates a FieldNotFoundException specifying the field name.
    /// </summary>
    public FieldNotFoundException(string field)
        : base($"Field {field} was not found.")
    {
    }

    /// <summary>
    /// Creates a FieldNotFoundException specifying the field name and an inner exception.
    /// </summary>
    public FieldNotFoundException(string field, Exception inner)
        : base($"Field {field} was not found.", inner)
    {
    }
}