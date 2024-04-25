# DataDestination enumeration

Specifies where the data should be sent to.

```csharp
public enum DataDestination
```

## Values

| name | value | description |
| --- | --- | --- |
| CsvFile | `0` | The data will be written to a csv file. |
| Database | `1` | The data will be INSERTed in a db using an SQL query. |
| JsonFile | `2` | The data will be written to a JSON file. |

## See Also

* namespace [Dandraka.Zoro.Processor](../Zoro.Processor.md)

<!-- DO NOT EDIT: generated by xmldocmd for Zoro.Processor.dll -->