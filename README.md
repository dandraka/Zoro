# Zoro
### The masked avenger

Zoro is a data masking/anonymization utility. It fetches data from a database or a CSV file, and creates a CSV file with masked data.

### Usage:
Zoro.exe path_to_config_file

E.g. ```Zoro.exe c:\temp\mask.xml```

### Sample config files:
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
				<Replacement Selector="Country=Niederlande" List="Bergselaan,Schieweg,Nootdorpstraat,Nolensstraat" />
				<Replacement Selector="Country=Schweiz" List="Bahnhofstrasse,Clarahofweg,Sperrstrasse,Erlenstrasse" />
				<Replacement Selector="Country=Liechtenstein" List="Lettstrasse,Bangarten,Beckagässli,Haldenweg" />
				<Replacement Selector="Country=Deutschland" List="Bahnhofstraße,Freigaße,Hauptstraße" />
				<Replacement Selector="Country=Belgien" List="Rue d'Argent,Rue d'Assaut,Rue de l'Ecuyer,Rue du Persil" />
				<Replacement Selector="Country=Österreich" List="Miesbachgasse,Kleine Pfarrgasse,Heinestraße" />
				<Replacement Selector="Country=Frankreich" List="Rue Nationale,Boulevard Vauban,Rue des Stations,Boulevard de la Liberté" />
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
