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
