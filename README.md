# Zoro - The masked avenger

Zoro is a data masking and anonymization utility. It fetches data from a database, a JSON or a CSV file, and either creates a JSON file, a CSV file or runs SQL statements with the masked data.

It can be used as a command line program or as a dotnet standard 2.1 library. To run the command line program, simply copy the ```tools``` dir from the [Nuget package](https://www.nuget.org/packages/Dandraka.Zoro). Windows and Linux versions, both 64-bit, are available.

## Usage:

**As a command line utility:**

[Win] zoro.exe path_to_config_file
E.g. ```zoro.exe c:\temp\mask.xml```

[Linux] ./zoro path_to_config_file
E.g. ```zoro /home/jim/data/mask.xml```

**As a library**

```
// === either read config from file ===
var configFromFile = Zoro.Processor.MaskConfig.ReadConfig("c:\temp\mask.xml");
// === or create in code ===
var config = new Zoro.Processor.MaskConfig()
{
    ConnectionString = "Server=myDbServer;Database=myDb;User Id=myUser;Password=myPassword;",
    ConnectionType = "System.Data.SqlClient",
    DataSource = DataSource.Database,
    DataDestination = DataDestination.CsvFile,
    SqlSelect = "SELECT * FROM testdata",
    OutputFile = Path.Combine(utility.TestInstanceDir, "maskeddata_db_02.csv")
};
config.FieldMasks.Add(new FieldMask() { FieldName = "name", MaskType = MaskType.Similar });
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
var masker = new Zoro.Processor.DataMasking(config);
masker.Mask();
```

**Sample config file:**

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

The above config file can process for example the following CSV:

```
ID;Name;BankAccount
1;KM-elektronik;CH9880808007645910141
2;Cretaneshop;GR4701102050000020547061026
3;VELOPLUSs.r.l;IT36K0890150920000000550061
4;natuurhulpcentrum.be;BE79235040722733
```

and the result will be something like the following:

```
ID;Name;BankAccount
1;RJ-egitrjitiz;*********************
2;Lqebuhuzfic;***************************
3;NKWNQWWBg.g.q;***************************
4;botaahjazlvojknub.qi;****************
```

**A more complete sample of a config file is the following:**

```
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>ID</FieldName>
      <MaskType>None</MaskType>
    </FieldMask>  
    <FieldMask>
      <FieldName>CountryISOCode</FieldName>
      <MaskType>None</MaskType>
    </FieldMask>      
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
    <FieldMask>
      <FieldName>City</FieldName>
      <MaskType>Query</MaskType>
      <QueryReplacement SelectorField="CountryISOCode" GroupField="countrycode" ValueField="cityname" Query="SELECT cityname, countrycode FROM cities" />
    </FieldMask>  
  </FieldMasks>
  <DataSource>Database</DataSource>
  <DataDestination>Database</DataDestination>
  <ConnectionString>Server=DBSRV1;Database=appdb;Trusted_Connection=yes;</ConnectionString>
  <ConnectionType>System.Data.SqlClient</ConnectionType>
  <SqlSelect>SELECT * FROM customers</SqlSelect>
  <SqlCommand>INSERT INTO customers_anonymous (ID, MainPhone, Street) VALUES ($ID, $MainPhone, $Street)</SqlCommand>
</MaskConfig>
```

If using a database to write data (DataDestination=Database), the number of parameters in SqlCommand ($field) must match the number of FieldMasks.

### Developer documentation

Please see the [generated docs](https://github.com/dandraka/Zoro/blob/master/docs/Zoro.Processor.md).

### Note:

Although not required by the license, the author kindly asks that you share any improvements you made.
