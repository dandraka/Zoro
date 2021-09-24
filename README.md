## 🎉 IMPORTANT NOTE 🥳
The project has been converted to Dotnet Core 5.0.
* Branch `master` now fully works, both for CSV and MS SQL data. This is the code that is maintained and will be published as a Nuget package.
* The branch `dotnet-461` is the legacy version, which must be built with .Net Framework 4.6.1. It works, but it's not maintained.

***

# Zoro - The masked avenger

Zoro is a data masking/anonymization utility. It fetches data from a database or a CSV file, and creates a CSV file with masked data.

It can be used as a command line program (zoro.exe) or as a dotnet standard library.

## Usage:

**As a command line utility:**

Zoro.exe path_to_config_file

E.g. ```Zoro.exe c:\temp\mask.xml```

**As a library**

```
// === either read config from file ===
var configFromFile = Zoro.Processor.MaskConfig.ReadConfig("c:\temp\mask.xml");
// === or create in code ===
var config = new Zoro.Processor.MaskConfig()
{
    ConnectionString = "Server=myDbServer;Database=myDb;User Id=myUser;Password=myPassword;",
    DataSource = DataSource.Database,
    SqlSelect = "SELECT * FROM testdata",
    OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata_db_02.csv"),
    FieldMasks = new List<FieldMask>()
};
config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Similar );
config.FieldMasks.Add(new FieldMask() { FieldName = "iban", MaskType = MaskType.Asterisk });
config.FieldMasks.Add(new FieldMask() { FieldName = "country", MaskType = MaskType.None });
config.FieldMasks.Add(new FieldMask() { FieldName = "address", MaskType = MaskType.List });
config.FieldMasks[3].ListOfPossibleReplacements = new List<Replacement>();
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "country=CH", ReplacementList="Bahnhofstrasse 41,Hauptstrasse 8,Berggasse 4" });
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "country=GR", ReplacementList="Evangelistrias 22,Thessalias 47,Eparhiaki Odos Lefkogion 6" });
// fallback when nothing matches; MUST be the last one
config.FieldMasks[3].ListOfPossibleReplacements.Add(new Replacement() 
    { Selector = "", ReplacementList="Main Street 9,Fifth Avenue 104,Ranch rd. 1" });         

// === with your config, call the masking method ===
var masker = new Zoro.Processor.DataMasking(config);
masker.Mask();
```

Sample config files:
```
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>Name</FieldName>
      <MaskType>Similar</MaskType>
    </FieldMask>
    <FieldMask>
      <FieldName>BankAccount</FieldName>
      <MaskType>Asterisk</MaskType>
    </FieldMask>
  </FieldMasks>
  <InputFile>C:\temp\Zorotests\data.csv</InputFile>
  <OutputFile>C:\temp\Zorotests\maskeddata.csv</OutputFile>
  <Delimiter>;</Delimiter>
</MaskConfig>
```

```
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>MainPhone</FieldName>
      <MaskType>Similar</MaskType>
      <RegExMatch>^(\+\d\d)?(.*)$</RegExMatch>
      <RegExGroupToReplace>2</RegExGroupToReplace>
    </FieldMask>
    <FieldMask>
      <FieldName>Street</FieldName>
      <MaskType>List</MaskType>
        <ListOfPossibleReplacements>
          <Replacement Selector="Country=Netherlands" List="Bergselaan,Schieweg,Nootdorpstraat,Nolensstraat" />
          <Replacement Selector="Country=Switzerland" List="Bahnhofstrasse,Clarahofweg,Sperrstrasse,Erlenstrasse" />
          <Replacement Selector="Country=Liechtenstein" List="Lettstrasse,Bangarten,Beckagässli,Haldenweg" />
          <Replacement Selector="Country=Germany" List="Bahnhofstraße,Freigaße,Hauptstraße" />
          <Replacement Selector="Country=Belgium" List="Rue d'Argent,Rue d'Assaut,Rue de l'Ecuyer,Rue du Persil" />
          <Replacement Selector="Country=Austria" List="Miesbachgasse,Kleine Pfarrgasse,Heinestraße" />
          <Replacement Selector="Country=France" List="Rue Nationale,Boulevard Vauban,Rue des Stations,Boulevard de la Liberté" />
          <!--- fallback when nothing matches; MUST be the last one --->
          <Replacement Selector="" List="Bedford Gardens,Sheffield Terrace,Kensington Palace Gardens" />
        </ListOfPossibleReplacements>
    </FieldMask>
  </FieldMasks>
  <InputFile></InputFile>
  <OutputFile>C:\temp\Zorotests\maskeddata.csv</OutputFile>
  <Delimiter>;</Delimiter>
  <DataSource>Database</DataSource>
  <ConnectionString>Server=DBSRV1;Database=appdb;Trusted_Connection=yes;</ConnectionString>
  <SqlSelect>SELECT * FROM customers</SqlSelect>
</MaskConfig>
```

### Note:

Although not required by the license, the author kindly asks that you share any improvements you made.
