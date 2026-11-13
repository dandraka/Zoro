# Zoro - The masked avenger

## BIG NEWS FOR VERSION 4.x !!!

Versions 4.x not only support anonymization of Office files (.docx, .xlsx and .pptx) but also now properly support Pseudonymisation: you can have a list of replacements that are applied consistenly. This way, for example, "Jim" is always replaced with "Genius", "George" is always replaced with "Hardworker", "Maria N" is always replaced with "Dancer", "Maria V" is always replaced with "Master of Pastries" and so on.

The list of replacements can be in the config itself, in a CSV or in a database. Read on for details.

## Purpose and scope

Zoro is a data masking and anonymization utility. It fetches data from a database, a JSON or a CSV file, and either creates a JSON file, a CSV file or runs SQL statements with the masked data. It can also be used with Office file types (currently .docx, .xlsx and .pptx files are supported) to anonymize their content.

The tool can be used both as a command line program and as a dotnet standard 2.1 library. To run the command line program, simply copy the ```tools``` dir from the [Nuget package](https://www.nuget.org/packages/Dandraka.Zoro). Windows and Linux versions, both 64-bit, are available.

## Usage:

**As a command line utility:**

**[Win]** Zoro.exe [mandatory: path to config file] [optional: path to input file] [optional: path to output file]

E.g. ```Zoro.exe c:\zoro\mask.xml```

or ```Zoro.exe c:\zoro\mask.xml c:\data\original.csv c:\data\anonymized.csv```

**[Linux]** ./zoro [mandatory: path to config file] [optional: path to input file] [optional: path to output file]

E.g. ```./zoro /home/jim/zoro/mask.xml```

or ```./zoro /home/jim/zoro/mask.xml /home/jim/data/original.csv /home/jim/data/anonymized.csv```

**As a library**

```
// === either read config from file ===
var configFromFile = Zoro.Processor.MaskConfig.ReadConfig("c:\temp\mask.xml");
// === or create in code ===
var config = new Zoro.Processor.MaskConfig()
{
    ConnectionString = "Server=myDbServer;Database=myDb;User Id=myUser;Password=myPassword;",
    ConnectionType = "Microsoft.Data.SqlClient",
    DataSource = DataSource.Database,
    DataDestination = DataDestination.CsvFile,
    SqlSelect = "SELECT * FROM testdata",
    OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata_db_02.csv")
};
config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Random });
config.FieldMasks.Add(new FieldMask() { FieldName = "iban", MaskType = MaskType.Asterisk });
config.FieldMasks.Add(new FieldMask() { FieldName = "country", MaskType = MaskType.None });
config.FieldMasks.Add(new FieldMask() { FieldName = "address", MaskType = MaskType.List });
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "country=CH", ReplacementList="Bahnhofstrasse 41,Hauptstrasse 8,Berggasse 4" });
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "country=GR", ReplacementList="Evangelistrias 22,Thessalias 47,Eparhiaki Odos Lefkogion 6" });
// fallback when nothing matches; MUST be the last one
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "", ReplacementList="Main Street 9,Fifth Avenue 104,Ranch rd. 1" });         

// === with your config, call the masking method ===
var masker = Zoro.Processor.DataMasking.Create(config);
masker.Mask();
```

**Migrating from 3.x to 4.x**

A small change is needed if you use Zoro as a library:

```DataMasking``` is now an abstract class, so ```new DataMasking(config)``` no longer compiles. Replace it with ```DataMasking.Create(config)```, which picks the right implementation for ```config.DataSource```. 

Nothing else is affected.

## Documentation:

### Masking types reference

Please see the [anonymization and masking types reference doc](https://github.com/dandraka/Zoro/blob/master/MaskingTypes.md).

### Developer documentation

Please see the [generated docs](https://github.com/dandraka/Zoro/blob/master/docs/Zoro.Processor.md).

### Notes on usage

- Input & Output files are optional, but if specified they take precedence over (i.e. are used instead of) the config file.
- Field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
- If using a database to write data (DataDestination=Database), all names of parameters in SqlCommand (@field for SqlServer or $field elsewhere) must have a corresponding FieldMask, even if the MaskType is None. Also, currently connection types of ```Microsoft.Data.SqlClient``` and ```System.Data.OleDb``` are supported, but if anything else (e.g. MySql, Oracle) is needed please open an issue; adding more is trivial.
- If input is a JSON of an Office file type (Docx, Xlsx, Pptx) and one or more FieldMasks are type List (FieldMask.MaskType=List), one 1 Replacement entry is allowed, which has to have an empty Selector (Selector="").
- If input is a JSON file type, FieldMasks that perform a database query (FieldMask.MaskType=Query) are not allowed. This is planned to be supported in a later version.
- If the input is an Office file type (Docx, Xlsx, Pptx), the output must be of the same type.
- The masking types ReplacementList and ReplacementQuery work like List and Query, but the masking is consistent (pseudonymisation): the same original value always gets the same replacement. If a value has no replacement left, it is masked with asterisks by default and the console program prints a warning; see ```OnListExhausted``` in the [masking types reference](https://github.com/dandraka/Zoro/blob/master/MaskingTypes.md).

## Examples:

Sample configurations for CSV, database, JSON, Word, Excel and PowerPoint files are in the [configuration samples](https://github.com/dandraka/Zoro/blob/master/ZoroConfigSamples.md).

### Note:

Although not required by the license, the author kindly asks that you share any improvements you made.
