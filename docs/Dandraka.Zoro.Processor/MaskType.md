# MaskType enumeration

Sets the type of masking done to a field.

```csharp
public enum MaskType
```

## Values

| name | value | description |
| --- | --- | --- |
| None | `0` | The field contents are not masked. This is the default. |
| Similar | `1` | The field contents are substituted with different content, e.g. a different name or post code. |
| Asterisk | `2` | The field contents are substituted with a character, e.g. asterisks or spaces. |
| List | `3` | The field contents are substituted with a randomly picked item of a given list. |
| Query | `4` | The field contents are substituted with a randomly picked item from the result of q query. The query must return only one field. |

## See Also

* namespace [Dandraka.Zoro.Processor](../Zoro.Processor.md)

<!-- DO NOT EDIT: generated by xmldocmd for Zoro.Processor.dll -->