# Zoro - The masked avenger

Zoro is a data masking and anonymization utility. It fetches data from a database, a JSON or a CSV file, and either creates a JSON file, a CSV file or runs SQL statements with the masked data.

It can be used as a command line program or as a dotnet standard 2.1 library. To run the command line program, simply copy the ```tools``` dir from the [Nuget package](https://www.nuget.org/packages/Dandraka.Zoro). Windows and Linux versions, both 64-bit, are available.

## Usage:

**As a command line utility:**

[Win] zoro.exe path_to_config_file
E.g. ```zoro.exe c:\temp\mask.xml```

[Linux] ./zoro path_to_config_file
E.g. ```./zoro /home/jim/data/mask.xml```

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

## Documentation:

### Masking types reference

Please see the [anonymization and masking types reference doc](https://github.com/dandraka/Zoro/blob/master/MaskingTypes.md).

### Developer documentation

Please see the [generated docs](https://github.com/dandraka/Zoro/blob/master/docs/Zoro.Processor.md).

### Notes on usage

- If using a database to write data (DataDestination=Database), all names of parameters in SqlCommand (@field for SqlServer or $field elsewhere) must have a corresponding FieldMask, even if the MaskType is None. Also, currently connection types of ```System.Data.SqlClient``` and ```System.Data.OleDb``` are supported, but if anything else (e.g. MySql, Oracle) is needed please open an issue; adding more is trivial.
- If input is a JSON file (DataSource=JsonFile) and one or more FieldMasks are type List (FieldMask.MaskType=List), one 1 Replacement entry is allowed, which has to have an empty Selector (Selector="").
- If input is a JSON file (DataSource=JsonFile), FieldMasks that perform a database query (FieldMask.MaskType=Query) are not allowed. This is planned to be supported in a later version.

## Examples:

**Sample config file for CSV source and destination**

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

**Sample config file for DB source and destination using lists and queries**

```
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>ID</FieldName>
      <MaskType>None</MaskType>
    </FieldMask>   
    <FieldMask>
      <FieldName>CustomerFullname</FieldName>
      <MaskType>None</MaskType>
    </FieldMask>   
    <FieldMask>
      <FieldName>CustomerCountry</FieldName>
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
          <Replacement Selector="CustomerCountry=NL" List="Bergselaan,Schieweg,Nootdorpstraat,Nolensstraat" />
          <Replacement Selector="CustomerCountry=CH" List="Bahnhofstrasse,Clarahofweg,Sperrstrasse,Erlenstrasse" />
          <Replacement Selector="CustomerCountry=LI" List="Lettstrasse,Bangarten,Beckagässli,Haldenweg" />
          <Replacement Selector="CustomerCountry=DE" List="Bahnhofstraße,Freigaße,Hauptstraße" />
          <Replacement Selector="CustomerCountry=BE" List="Rue d'Argent,Rue d'Assaut,Rue de l'Ecuyer,Rue du Persil" />
          <Replacement Selector="CustomerCountry=FR" List="Rue Nationale,Boulevard Vauban,Rue des Stations,Boulevard de la Liberté" />
          <!--- fallback when nothing matches; MUST be the last one --->
          <Replacement Selector="" List="Bedford Gardens,Sheffield Terrace,Kensington Palace Gardens" />
        </ListOfPossibleReplacements>
    </FieldMask>          
    <FieldMask>
      <FieldName>CustomerCity</FieldName>
      <MaskType>Query</MaskType>
      <QueryReplacement 
        SelectorField="CustomerCountry" 
        GroupField="cityCountryName" 
        ValueField="cityName" 
        Query="SELECT cityName, cityCountryName FROM cities" />
    </FieldMask>  
  </FieldMasks>
  <DataSource>Database</DataSource>
  <DataDestination>Database</DataDestination>
  <ConnectionString>Server=DBSRV1;Database=appdb;Trusted_Connection=yes;</ConnectionString>
  <!-- Currently System.Data.SqlClient and System.Data.OleDb are supported, but if needed, adding more is trivial -->
  <ConnectionType>System.Data.SqlClient</ConnectionType>
  <SqlSelect>SELECT ID, CustomerFullname, CustomerCity, CustomerCountry FROM customers</SqlSelect>
  <!-- Note that the parameter character is @ for Sql Server, $ elsewhere -->
  <SqlCommand>INSERT INTO customers_anonymous (ID, CustomerFullname, CustomerCity, CustomerCountry) VALUES (@ID, @CustomerFullname, @CustomerCity, @CustomerCountry)</SqlCommand>
</MaskConfig> 
```

**Sample config file for JSON source and destination using an Expression and a List**

```
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>name</FieldName>
      <MaskType>Expression</MaskType>
      <Expression>Customer {{$.id}}</Expression>
    </FieldMask>
    <FieldMask>
      <FieldName>salary</FieldName>
      <MaskType>Similar</MaskType>
    </FieldMask>  
    <FieldMask>
      <FieldName>spouse</FieldName>
      <MaskType>List</MaskType>
		<ListOfPossibleReplacements>
			<Replacement Selector="" List="Eleni Koufaki,Athina Lefkogianaki,Mihaela Papadomanolaki" />
		</ListOfPossibleReplacements>
	</FieldMask>	  
  </FieldMasks>
  <InputFile>%TestInstanceDir%\data2.json</InputFile>
  <OutputFile>%TestInstanceDir%\maskedata2.json</OutputFile>
  <DataSource>JsonFile</DataSource>
  <DataDestination>JsonFile</DataDestination>
</MaskConfig>
```

The above config file can process for example the following JSON:

```
{
    "employees": [
        {
            "id": "1",
            "name": "Aleksander Singh",
            "salary": 105000,
            "spouse": "Ingrid Díaz"
        },
        {
            "id": "2",
            "name": "Alicja Bakshi",
            "salary": 142500,
            "spouse": "Ellinore Alvarez"
        }
    ]
}
```

and the result will be something like the following:

```
{
    "employees": [
        {
            "id": "1",
            "name": "Customer-1",
            "salary": 902473,
            "spouse": "Mihaela Papadomanolaki"
        },
        {
            "id": "2",
            "name": "Customer-2",
            "salary": 046795,
            "spouse": "Eleni Koufaki"
        }
    ]
}
```

### Note:

Although not required by the license, the author kindly asks that you share any improvements you made.
