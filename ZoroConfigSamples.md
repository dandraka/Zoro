# Zoro configuration samples

Sample configuration files for the different data sources. For the meaning of every setting, see the [masking types reference](https://github.com/dandraka/Zoro/blob/master/MaskingTypes.md); for how to run Zoro, see the [README](https://github.com/dandraka/Zoro/blob/master/README.md).

- [CSV source and destination](#csv-source-and-destination)
- [DB source and destination, using lists and queries](#db-source-and-destination-using-lists-and-queries)
- [JSON source and destination, using an Expression and a List](#json-source-and-destination-using-an-expression-and-a-list)
- [Word document (.docx), using a List to replace IBANs](#word-document-docx-using-a-list-to-replace-ibans)
- [Excel workbook (.xlsx), pseudonymising customers from a csv file](#excel-workbook-xlsx-pseudonymising-customers-from-a-csv-file)
- [PowerPoint presentation (.pptx), customer review deck](#powerpoint-presentation-pptx-customer-review-deck)

## CSV source and destination

```xml
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>Name</FieldName>
      <MaskType>Random</MaskType>
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

## DB source and destination, using lists and queries

```xml
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
      <MaskType>Random</MaskType>
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
          <!-- fallback when nothing matches; MUST be the last one -->
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
  <!-- Currently Microsoft.Data.SqlClient and System.Data.OleDb are supported, but if needed, adding more is trivial -->
  <ConnectionType>Microsoft.Data.SqlClient</ConnectionType>
  <SqlSelect>SELECT ID, CustomerFullname, CustomerCity, CustomerCountry FROM customers</SqlSelect>
  <!-- Note that the parameter character is @ for Sql Server, $ elsewhere -->
  <SqlCommand>INSERT INTO customers_anonymous (ID, CustomerFullname, CustomerCity, CustomerCountry) VALUES (@ID, @CustomerFullname, @CustomerCity, @CustomerCountry)</SqlCommand>
</MaskConfig>
```

## JSON source and destination, using an Expression and a List

```xml
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
      <MaskType>Random</MaskType>
    </FieldMask>
    <FieldMask>
      <FieldName>spouse</FieldName>
      <MaskType>List</MaskType>
      <ListOfPossibleReplacements>
        <Replacement Selector="" List="Eleni Koufaki,Athina Lefkogianaki,Mihaela Papadomanolaki" />
      </ListOfPossibleReplacements>
    </FieldMask>
  </FieldMasks>
  <InputFile>C:\temp\Zorotests\data2.json</InputFile>
  <OutputFile>C:\temp\Zorotests\maskedata2.json</OutputFile>
  <DataSource>JsonFile</DataSource>
  <DataDestination>JsonFile</DataDestination>
</MaskConfig>
```

The above config file can process for example the following JSON:

```json
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

```json
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

## Word document (.docx), using a List to replace IBANs

```xml
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <FieldMask>
      <FieldName>IBAN</FieldName>
      <MaskType>List</MaskType>
      <RegExMatch>\b([A-Za-z]{2}\w{16})\b</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
      <ListOfPossibleReplacements>
        <Replacement Selector="" List="NL1111111111111111,GR2222222222222222,CH3333333333333333" />
      </ListOfPossibleReplacements>
    </FieldMask>
  </FieldMasks>
  <InputFile>C:\temp\Zorotests\report.docx</InputFile>
  <OutputFile>C:\temp\Zorotests\report_masked.docx</OutputFile>
  <DataSource>DocXFile</DataSource>
  <DataDestination>DocXFile</DataDestination>
</MaskConfig>
```

That will replace all words starting with 2 letters and followed by 16 letters or numbers (which is a simplified version of what an IBAN looks like) with one replacement from the list.

## Excel workbook (.xlsx), pseudonymising customers from a csv file

Scenario: the finance department wants to share its open-invoices workbook with an external consultant. Customer names must be replaced with pseudonyms that are the same everywhere in the workbook (so that totals per customer can still be traced), IBANs must be replaced with sample IBANs, and e-mail addresses must not identify anyone.

```xml
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <!-- Customer names. The csv file (columns "original" and "pseudonym") is maintained by finance and
         has one row per customer. FieldName is just a label: all the names of the csv file are searched. -->
    <FieldMask>
      <FieldName>customers</FieldName>
      <MaskType>ReplacementList</MaskType>
      <ListOfPossibleReplacements>
        <ReplacementCSV CSVFileName="C:\finance\customer_pseudonyms.csv" FromFieldName="original" ToFieldName="pseudonym" />
      </ListOfPossibleReplacements>
    </FieldMask>
    <!-- IBANs. Every different IBAN gets its own sample IBAN, the same one wherever it appears, so the list
         must have at least as many items as there are different IBANs. If it does not, stop instead of
         producing a file with some IBANs replaced by asterisks. -->
    <FieldMask>
      <FieldName>IBAN</FieldName>
      <MaskType>ReplacementList</MaskType>
      <RegExMatch>\b([A-Z]{2}\d{2}\w{10,30})\b</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
      <OnListExhausted>Error</OnListExhausted>
      <ListOfPossibleReplacements>
        <Replacement Selector="" List="DE89370400440532013000,GB82WEST12345698765432,FR1420041010050500013M02606,NL91ABNA0417164300,CH9300762011623852957" />
      </ListOfPossibleReplacements>
    </FieldMask>
    <!-- E-mail addresses: only the part before the @ is replaced, so the domain stays readable. -->
    <FieldMask>
      <FieldName>email</FieldName>
      <MaskType>Random</MaskType>
      <RegExMatch>\b([\w.+-]+)@[\w-]+(\.[\w-]+)+\b</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
    </FieldMask>
  </FieldMasks>
  <InputFile>C:\finance\Q3_open_invoices.xlsx</InputFile>
  <OutputFile>C:\finance\Q3_open_invoices_masked.xlsx</OutputFile>
  <DataSource>XlsXFile</DataSource>
  <DataDestination>XlsXFile</DataDestination>
</MaskConfig>
```

Notes:
- Only the text of normal text cells is masked; numbers, formulas, sheet names and comments are not.
- The output file must not exist yet.
- If the pseudonyms are kept in a database instead of a csv file, use the ReplacementQuery mask type, see the [masking types reference](https://github.com/dandraka/Zoro/blob/master/MaskingTypes.md).

## PowerPoint presentation (.pptx), customer review deck

Scenario: a quarterly customer review deck has to be reused as a training example. It names two customers and their contacts, mentions an internal project name, shows amounts and lists e-mail addresses.

```xml
<?xml version="1.0"?>
<MaskConfig xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <FieldMasks>
    <!-- Customers, contacts and the project name. A few names, so they are in the config itself.
         Matching ignores case, and the longest name wins ("Helvetia Logistics AG" before "Helvetia").
         FieldName is just a label: all the names of the Map items are searched. -->
    <FieldMask>
      <FieldName>names</FieldName>
      <MaskType>ReplacementList</MaskType>
      <ListOfPossibleReplacements>
        <Replacement Selector="">
          <Map From="Helvetia Logistics AG" To="Customer A" />
          <Map From="Helvetia" To="Customer A" />
          <Map From="Hellenic Marine Supplies" To="Customer B" />
          <Map From="Anna Keller" To="Contact 1" />
          <Map From="Nikos Papadakis" To="Contact 2" />
          <Map From="Project Falcon" To="Project Alpha" />
        </Replacement>
      </ListOfPossibleReplacements>
    </FieldMask>
    <!-- Amounts: the digits after a currency code are replaced, the currency and the separators are kept. -->
    <FieldMask>
      <FieldName>amounts</FieldName>
      <MaskType>Random</MaskType>
      <RegExMatch>\b(?:CHF|EUR|USD) ?(\d[\d.,']*)</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
    </FieldMask>
    <!-- E-mail addresses: only the part before the @ is replaced. -->
    <FieldMask>
      <FieldName>email</FieldName>
      <MaskType>Random</MaskType>
      <RegExMatch>\b([\w.+-]+)@[\w-]+(\.[\w-]+)+\b</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
    </FieldMask>
  </FieldMasks>
  <InputFile>C:\sales\Q3_customer_review.pptx</InputFile>
  <OutputFile>C:\sales\Q3_customer_review_masked.pptx</OutputFile>
  <DataSource>PptXFile</DataSource>
  <DataDestination>PptXFile</DataDestination>
</MaskConfig>
```

Notes:
- All slides are masked, including hidden ones, as well as the layouts and masters they use (e.g. a customer name in the footer) and the speaker notes.
- Charts and SmartArt are not masked.
- The output file must not exist yet.
