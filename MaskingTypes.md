# Anonymization (a.k.a. masking) and Masking Types reference

This reference explains two things:
* Anonymization techniques, i.e. what can you do with your data in order to anonymize them and
* Zoro Masking Types, i.e. how you can do this with Zoro

## Anonymization techniques

Regardless of the tool being used and irrespective of the source or destination, in order to anonymize a set of data the following actions can be taken:                                                                                                                  
| Technhique                         | Description                                                                    |  Example                                                                                 |
|------------------------------------|--------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| Pseudonymisation                   | Exchanging the original data with pre-created fake data in a _reversible_ way  | Nick Peterson _always_ becomes John Miller, Eva Richards _always_ becomes Jane Brady etc. |
| Shuffling a.k.a. Permutation       | Shuffling values between records                                               | Customer.City gets a random city from the Cities table |
| Noise Addition a.k.a. Perturbation | Changing part of the value thus keeping the order of magnitude and distribution| Change last 4 digits of phone numbers, height +/- 5cm |
| Generalization a.k.a. Aggregation  | Changing values by using aggregated values (buckets) thus keeping the order of magnitude | Account balances between 0-1000 become 500, between 1001-2000 become 1500 etc. |
| Synthetic (fake) data              | Exchanging the original data with pre-created fake data in a _random_ way      | Linkoln Rd. 47 becomes Main Street 5, River View 23 becomes Wall Str. 10 etc. |
| Hiding                             | Exchanng data with fake uniform data                                           | NL35INGB0679775293 becomes ***************** |

This is how Zoro supports these techniques:

| Technique                     | Supported by Zoro (current version / roadmap)    | Notes                             |
|-------------------------------|--------------------------------------------------|-----------------------------------|
| Pseudonymisation              | Fully, current version                           | Use the ReplacementList and ReplacementQuery masking types: within a run, the same original value always gets the same replacement. Explicit mappings (in the config or in a db table) also make this repeatable across runs. Fake data can also be generated consistently with Expression, e.g. Customer Rick Bears with CustomerID=5 can become Customer-5 |
| Shuffling                     | Fully, current version                           | The usage of a db query is needed |
| Noise Addition                | Partially, current version & fully on roadmap    | String-based noise fully supported, arithmetic noise currently not supported but is on the roadmap |
| Generalization                | Partially, current version & roadmap             | String-based generalization fully supported, arithmetic generalization is out of scope but most scenarios can be achieved using a db query |
| Synthetic (fake) data         | Fully, current version                           |                                   |
| Hiding                        | Fully, current version                           |                                   |

### Note about FieldName in Office file types

Obviously, Office documents are different than CSV or JSON files. Documents contain semi-structured data as opposed to structured data that can be found in a CSV. There are no fields to choose from; it's just a stream of words.

For this reason, the FieldName in all masking types is used somewhat differently in Office documents:
- In a CSV, it's used to choose which column the program will modify.
- But in a document, it's either used to choose the text block to process or, if paired with a regular expression, ignored.

Two concrete examples (here using MaskType=Asterisk for simplicity, but Random, List, Query, ReplacementList and ReplacementQuery can be used as well, though not Expression):

- FieldName = "coffee" will go block by block (which usually means line by line) and, using the regular expression ```(.*)(coffee)(.*)```, will replace the 2nd match. 

  That replaces the word coffee with ****** (or whatever is being used as asterisk).

- But sometimes certain words are found in slightly different forms, and they all need to be replaced. A document might contain the words coffee, coffeecup, coffeehouse etc. To replace all of these, the following can be used:
  - FieldName = "coffee" (which is ignored for replacement purposes)
  - RegExMatch = ```"\b(coffee\w*)\b"```
  - RegExGroupToReplace = 1

  That replaces all words that start with coffee with ****** (as many asterisks as the word being replaced).

A few more points about Office documents:
- Text is matched per paragraph (Word and PowerPoint) or per cell (Excel), so it is found even if formatting splits it in several parts, e.g. a name that is partly bold. The replacement gets the formatting of the part where the match starts. A tab, a line break or a field ends the text being matched, so words on its two sides are never glued together.
- In PowerPoint files, the text of all slides (hidden ones too), of the layouts and masters they use and of the speaker notes is masked. Charts and SmartArt are not masked.
- Only the text of the regex group (RegExGroupToReplace) is replaced, and every match is masked on its own; the rest of the match is kept.

## Masking types reference

For every instance of a [Field Mask](https://github.com/dandraka/Zoro/blob/master/docs/Dandraka.Zoro.Processor/FieldMask.md) the following [masking types](https://github.com/dandraka/Zoro/blob/master/docs/Dandraka.Zoro.Processor/MaskType.md) are available:

### MaskType=None

The field contents are not masked. This is the default. This can also be achieved by omitting the field from configuration.

Example
```
  <FieldMasks>
    <FieldMask>
      <FieldName>ID</FieldName>
      <MaskType>None</MaskType>
    </FieldMask> 
  </FieldMasks>    
```

#### Mandatory, optional and ignored fields

* MaskType = None
* Asterisk: Ignored
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Ignored
* RegExGroupToReplace: Ignored
* RegExMatch: Ignored

Sample input and output

| ID (original) | ID (masked) |
|--------------:|------------:|
| 1             | 1           |

### MaskType=Random

Note: Old mask type "Similar" was renamed to "Random", as it is more intuitive. Similar is kept for backwards compatibility, so existing configs do not need to change.

Each character of the original data is substituted by a random character taking the following into account:

* If the original character is a space or a symbol, it is preserved unchanged. E.g. @ stays @.
* If the original character is a letter or number, the random character is also a letter or number respectively. E.g. f might become g, 5 might become 2.
* If the original character is a letter, the random character prerves the case (upper/lower) and vowel/consonant of the original. E.g. a might become i, K might become D.

With the usage of a regular expression, it is possible to change all or only part of the original data.

#### Mandatory, optional and ignored fields

* MaskType = Random
* Asterisk: Ignored
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example

```
    <FieldMask>
      <FieldName>ProductDescription</FieldName>
      <MaskType>Random</MaskType>
    </FieldMask>  
```

Sample input and output

| ProductDescription (original) | ProductDescription (masked) |
|:------------------------------|:----------------------------|
| Corn Flour 200 gr.            | Gawq Spaix 348 lk.          |

#### Example with regular expression

```
    <FieldMask>
      <FieldName>MainPhone</FieldName>
      <MaskType>Random</MaskType>
      <RegExMatch>^(\+\d\d\d\d\d)?(.*)$</RegExMatch>
      <RegExGroupToReplace>2</RegExGroupToReplace>
    </FieldMask>  
```

Sample input and output

| MainPhone (original) | MainPhone (masked) |
|:---------------------|:-------------------|
| +41785412798         | +41785099753       |

### MaskType=Asterisk

The field contents are substituted with a constant character. If the asterisk character is not specified, it defaults to an asterisk (*).

With the usage of a regular expression, it is possible to change all or only part of the original data.

#### Mandatory, optional and ignored fields

* MaskType = Asterisk
* Asterisk: Optional. A character that will replace the original data. Defaults to an asterisk (*).
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example
```
  <FieldMasks>
    <FieldMask>
      <FieldName>IBAN</FieldName>
      <MaskType>Asterisk</MaskType>
      <Asterisk>0</Asterisk>
      <RegExMatch>(....)(.*)</RegExMatch>
      <RegExGroupToReplace>2</RegExGroupToReplace>      
    </FieldMask> 
  </FieldMasks>    
```

Sample input and output

| IBAN (original)      | IBAN (masked)        |
|:---------------------|:---------------------|
| CH930076201162385297 | CH930000000000000000 |

### MaskType=Expression

The field contents are substituted with a string that may contain one or more existing fields (in case the source is Csv or DB query) or JsonPath (in case the source is Json). The fields/JsonPaths must be enclosed in double curly brackets, e.g. {{id}} or {{$.name}}.

In the case of Json and in order to facilitate complex scenarios, when a node with a matching name is found anywhere in the Json tree, Zoro first applies the JsonPath expression to the node's _parent_. If nothing is found, it applies it to the Json _root_. If nothing is found in both cases, a [FieldNotFoundException](https://github.com/dandraka/Zoro/blob/master/docs/Dandraka.Zoro.Processor/FieldNotFoundException.md) is thrown.

With the usage of a regular expression, it is possible to change all or only part of the original data.

Please note that for Office input types (docx, xlsx, pptx), this masking type is not supported as it doesn't make sense; there's no "field" in a document to get values from. Using an Expression masking type with an Office input type throws an error.

#### Mandatory, optional and ignored fields

* MaskType = Expression
* Asterisk: Ignored
* Expression: Mandatory. An expression consisting of fixed text and fields/JsonPaths enclosed in double curly brackets.
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example for CSV or DB query
```
  <FieldMasks>
    <FieldMask>
      <FieldName>ProductDescription</FieldName>
      <MaskType>Expression</MaskType>
      <Expression>product-{{id}} ({{manufacturer}})</Expression>
      <RegExMatch>(.*?) (.*)</RegExMatch>
      <RegExGroupToReplace>2</RegExGroupToReplace>      
    </FieldMask> 
  </FieldMasks>    
```

Sample input and output

| ID | Manufacturer | ProductDescription (original) | ProductDescription (masked) |
|----|--------------|-------------------------------|-----------------------------|
| 5  | Olympos      | Feta in salt water 500 gr.    | Feta product-5 (Olympos)    |

#### Example for JSON
```
  <FieldMasks>
    <FieldMask>
      <FieldName>name</FieldName>
      <MaskType>Expression</MaskType>
      <Expression>Customer {{$.id}}</Expression>
    </FieldMask>   
    <FieldMask>
      <FieldName>spouse</FieldName>
      <MaskType>Expression</MaskType>
      <Expression>Spouse of {{$.employees[0].id}}</Expression>
    </FieldMask> 
  </FieldMasks>    
```

Sample input and output

Original JSON
```
{
    "employees": [
        {
            "id": "1",
            "name": "Aleksander Singh",
            "spouse": "Ingrid Díaz"
        },
        {
            "id": "2",
            "name": "Alicja Bakshi",
            "spouse": "Ellinore Alvarez"
        }
    ]
}   
```

Masked JSON
```
{
    "employees": [
        {
            "id": "1",
            "name": "Customer 1",
            "spouse": "Spouse of Customer 1"
        },
        {
            "id": "2",
            "name": "Customer 2",
            "spouse": "Spouse of Customer 1"
        }
    ]
}
```

### MaskType=List

The field contents are substituted with a randomly picked item of one or more given lists. A selector can be used to pick the correct list (for example, streets that martch the country). If a match is not found, Zoro uses the fallback list which has an empty selector and must be the last one.

The lists must consist of items that are separated using a comma.

In the case of Json, only one list with an empty selector (a.k.a. fallback) is allowed.

#### Mandatory, optional and ignored fields

* MaskType = List
* Asterisk: Ignored
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: A list of ```<Replacement>``` items. Each item must contain:
  * a Selector attribute, which can be either empty (fallback) or contain a field name from the data, the equality sign (=) and a constant value. E.g. ```Selector="Country=Greece"```.
  * and a List attribute, which is a comma-separated list of strings. E.g. ```List="Feta,Olives,Kasseri"```.
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example for CSV or DB query
```
  <FieldMasks>
    <FieldMask>
      <FieldName>ProductDescription</FieldName>
      <MaskType>List</MaskType>
      <ListOfPossibleReplacements>
        <Replacement Selector="Country=Greece" List="Feta,Olives,Kasseri" />
        <Replacement Selector="Country=Switzerland" List="Sbrinz,Emmentaler,Gruyère" />
        <Replacement Selector="Country=Portugal" List="Pastéis de nata,Bacalhau,Ginjinha" /> 
        <!--- fallback when nothing matches; MUST be the last one --->
        <Replacement Selector="" List="Philly Cheesesteak,Hamburger,Hot Dog" />                
      </ListOfPossibleReplacements>      
    </FieldMask> 
  </FieldMasks>   
```

Sample input and output

| Country      | ProductDescription (original) | ProductDescription (masked) |
|--------------|-------------------------------|-----------------------------|
| Greece       | Extra virgin Olive oil 500 ml.| Kasseri                     |
| Sweden       | IKEA meatballs 1 kg. (frozen) | Philly Cheesesteak          |
| Portugal     | Port wine 750 ml.             | Bacalhau                    |
| Switzerland  | Fondue Kit Migros             | Gruyère                     |

### MaskType=Query

The field contents are substituted with a randomly picked item of one or more given lists which are fetched using a database query. A selector can be used to pick the correct list (for example, streets that martch the country). If a match is not found, a DataNotFound exception is raised (no fallback possible).

Please note that for Office input types (docx, xlsx, pptx), the selector is ignored as it doesn't make sense; there's no "field" in a document to group and select values with.

#### Mandatory, optional and ignored fields

* MaskType = Query
* Asterisk: Ignored
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Mandatory. Must contain all of the following attributes:
  * SelectorField: The name of the field _from the original data_ which will be used to match the reference records. Ignored for Office input types.
  * GroupField: The name of the field from the reference query (see Query below) which needs to match the values from the SelectorField. If GroupField is empty, all data in the ValueDbField retrieved from the Query will be used as a single group; the SelectorField will be ignored. Ignored for Office input types (i.e. same as if set to empty).
  * ValueField: The name of the field from the reference query (see Query below)which will be used as value after a random record (from the records where SelectorField=GroupField) is picked.
  * Query: The SQL query which will be executed to fetch the reference records.
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

### Example for a DB query
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

Sample input and output

| CityName	| CityCountryName |
|-----------|-----------------| 
| Basel	    | CH              |
| Chur	    | CH              |
| Wollerau  | CH              |
| Berlin	  | DE              |
| Stuttgard | DE              |
| Münich	  | DE              |
| Gianniou	| GR              |
| Asomatos	| GR              |
| Lefkogeia	| GR              |

_Cities reference table_

| CustomerCountry | CustomerFullname     | CustomerCity (original) | CustomerCity (masked) |
|-----------------|----------------------|-------------------------|-----------------------|
| CH              | MegaCorp Switzerland | Zürich                  | Wollerau              |
| DE              | InnoChem Germany     | Köln                    | Stuttgard             |
| GR              | Fage S.A.            | Athens                  | Asomatos              |

### MaskType=ReplacementList

Like List, but the masking is _consistent_: the same original value always gets the same replacement, and different original values get different replacements. This is what is needed for pseudonymisation, e.g. Athens is always replaced by CityA and London by CityB.

Original values are matched ignoring case and leading/trailing spaces, i.e. Athens, athens and ` ATHENS ` are the same value.

The replacement of a value comes from one of the following:

* **A mapping** you define, using ```<Map From="Athens" To="CityA" />``` items inside a ```<Replacement>```.
* **A pool** of items, which is the ```List``` attribute of the ```<Replacement>``` (comma-separated, as in the List mask type). Each new value gets a randomly picked item that has not been used yet, neither by another value nor by a ```<Map>```.
* **Both.** Mappings are applied first; all other values get items from the pool.

A long list of ```<Map>``` items is not practical in a configuration file, and it often changes independently of the rest of the configuration. For this reason the mappings can be read from a csv file, using a single ```<ReplacementCSV>``` item instead of the ```<Replacement>``` items (see below). Its file is read when the masking starts.

Consistency is kept for the duration of a run, separately for every field mask and for every ```<Replacement>``` (i.e. for every selector). Pool items are picked randomly, so they differ from run to run; if the same pseudonyms are needed in every run, use mappings.

If there are no replacements left for a value (more distinct values than pool items, or a value without a mapping and no pool), the ```OnListExhausted``` field decides what happens:

* ```Asterisk``` (default): The value is masked with the Asterisk character (see Asterisk, default *), so it is visible in the output that the configuration needs more items or mappings. The console program prints a warning, and the library offers ```DataMasking.ValuesWithoutReplacement```, which is the number of distinct values this happened to.
* ```Error```: A [ReplacementExhaustedException](https://github.com/dandraka/Zoro/blob/master/docs/Dandraka.Zoro.Processor/ReplacementExhaustedException.md) is thrown.
* ```Reuse```: An already used replacement is picked randomly. This keeps the masking consistent, but two different values may then share a replacement.

When a regular expression is used, the matched text of the regex group is the original value, and every match is replaced on its own. Only the group is replaced; the rest of the match is kept.

Please note that for Office input types (docx, xlsx, pptx) and Json the selector is ignored or not supported, as with the List mask type: use one ```<Replacement>``` with an empty selector.

In Office input types, if no RegExMatch is given and there are ```<Map>``` items, Zoro looks for all the mapped values in the document (whole words or phrases, ignoring case, longest first) and FieldName is just a label. This way one field mask can pseudonymise any number of names. Without maps, FieldName is the text that is looked for, as in the other masking types.

#### Mandatory, optional and ignored fields

* MaskType = ReplacementList
* Asterisk: Optional. The character used when ```OnListExhausted``` is Asterisk. Defaults to an asterisk (*).
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: A list of ```<Replacement>``` items. Each item can contain:
  * a Selector attribute, as in the List mask type.
  * a List attribute, which is the pool, a comma-separated list of strings.
  * ```<Map>``` items, each having a From attribute (the original value) and a To attribute (the replacement). The same From value (ignoring case and spaces) cannot be used twice in the same ```<Replacement>```.
  * Instead of all of the above, one ```<ReplacementCSV>``` item, which takes the mappings from a csv file. It must be the only item of the list (using it together with ```<Replacement>``` items is an error) and has the following attributes:
    * CSVFileName: Mandatory. The csv file, which must exist. A relative path is relative to the current directory, like InputFile.
    * FromFieldName: Mandatory. The name of the column (in the first row of the csv file) that holds the original values.
    * ToFieldName: Mandatory. The name of the column that holds the replacements.
    * Delimiter: Optional. The delimiter of the csv file. Defaults to a comma. A tab can be written as ```&#9;```.

    Values in the csv file can be enclosed in double quotes, e.g. when they contain the delimiter. Rows with an empty original value are ignored. The same original value (ignoring case and spaces) cannot appear twice. Column names are matched ignoring case. A missing column or an empty file is an error.
* OnListExhausted: Optional. Asterisk (default), Error or Reuse.
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1. 0 means the whole match.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example: random items from a pool
```
  <FieldMasks>
    <FieldMask>
      <FieldName>City</FieldName>
      <MaskType>ReplacementList</MaskType>
      <ListOfPossibleReplacements>
        <Replacement Selector="" List="CityA,CityB,CityC,CityD" />
      </ListOfPossibleReplacements>
    </FieldMask> 
  </FieldMasks>   
```

Sample input and output

| City (original) | City (masked) |
|-----------------|---------------|
| Athens          | CityC         |
| London          | CityA         |
| athens          | CityC         |
| Paris           | CityD         |
| Rome            | CityB         |
| Berlin          | ******        |

_Berlin is the 5th different city but there are only 4 items, so the default OnListExhausted=Asterisk applies._

#### Example: your own mappings, with a pool for the rest and a different list per country
```
  <FieldMasks>
    <FieldMask>
      <FieldName>City</FieldName>
      <MaskType>ReplacementList</MaskType>
      <OnListExhausted>Error</OnListExhausted>
      <ListOfPossibleReplacements>
        <Replacement Selector="Country=GR" List="Polis1,Polis2,Polis3">
          <Map From="Athens" To="Capital" />
        </Replacement>
        <!--- fallback when nothing matches; MUST be the last one --->
        <Replacement Selector="" List="Town1,Town2,Town3" />
      </ListOfPossibleReplacements>
    </FieldMask> 
  </FieldMasks>   
```

Sample input and output

| Country | City (original) | City (masked) |
|---------|-----------------|---------------|
| GR      | Athens          | Capital       |
| GR      | Patra           | Polis2        |
| UK      | London          | Town3         |
| UK      | Athens          | Town1         |
| GR      | Athens          | Capital       |

#### Example: names in an Office document
```
  <FieldMasks>
    <FieldMask>
      <FieldName>people</FieldName>
      <MaskType>ReplacementList</MaskType>
      <RegExMatch>\b(Nick Peterson|Eva Richards)\b</RegExMatch>
      <RegExGroupToReplace>1</RegExGroupToReplace>
      <ListOfPossibleReplacements>
        <Replacement Selector="">
          <Map From="Nick Peterson" To="John Miller" />
          <Map From="Eva Richards" To="Jane Brady" />
        </Replacement>
      </ListOfPossibleReplacements>
    </FieldMask> 
  </FieldMasks>   
```

Every occurrence of Nick Peterson in the document becomes John Miller and every occurrence of Eva Richards becomes Jane Brady, even if they are in the same sentence.

#### Example: mappings from a csv file
```
  <FieldMasks>
    <FieldMask>
      <FieldName>all</FieldName>
      <MaskType>ReplacementList</MaskType>
      <ListOfPossibleReplacements>
        <ReplacementCSV CSVFileName="names.csv" FromFieldName="original" ToFieldName="pseudonym" />
      </ListOfPossibleReplacements>
    </FieldMask> 
  </FieldMasks>   
```

_names.csv_
```
original,pseudonym
Nick Peterson,John Miller
Eva Richards,Jane Brady
```

This gives the same result as the ```<Map>``` items of the previous example. As there, a RegExMatch can be used or omitted; if it is omitted and the source is an Office file, all the values of the csv file are looked for in the document.

### MaskType=ReplacementQuery

Like ReplacementList, but the pool or the mappings are fetched using a database query, as the Query mask type does. Everything said about ReplacementList (consistency, matching ignoring case and spaces, ```OnListExhausted```, regular expressions) applies. The order of the rows returned by the query does not matter.

There are two ways to use it:

* **A pool.** The query returns the replacements in ValueField. Each new value gets a randomly picked item that has not been used yet. This is the ReplacementList pool, fetched from a database.
* **A mapping table.** If KeyField is filled, each row of the query is a pair: the original value is in KeyField and its replacement is in ValueField. Rows with an empty key are ignored, and the same key (ignoring case and spaces) cannot appear twice in the same group. Values that are not in the table follow ```OnListExhausted```.

In both cases, GroupField and SelectorField can be used to have different lists or mappings per group, e.g. per country, exactly as in the Query mask type. For Office input types the selector and group are ignored.

For Office input types, a mapping table (KeyField filled) used without a RegExMatch makes Zoro look for all the keys of the table in the document, as described in ReplacementList.

#### Mandatory, optional and ignored fields

* MaskType = ReplacementQuery
* Asterisk: Optional. The character used when ```OnListExhausted``` is Asterisk. Defaults to an asterisk (*).
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* OnListExhausted: Optional. Asterisk (default), Error or Reuse.
* QueryReplacement: Mandatory. Must contain the following attributes:
  * SelectorField: As in the Query mask type. Ignored for Office input types.
  * GroupField: As in the Query mask type. Ignored for Office input types.
  * KeyField: Optional. If filled, the query is a mapping table and this is the field of the query that holds the original value. If empty, the query is a pool.
  * ValueField: The name of the field from the query that holds the replacements.
  * Query: The SQL query which will be executed to fetch the reference records.
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced. The number is 1-based, not 0-based, i.e. the first group is 1. 0 means the whole match.
* RegExMatch: Optional. A regular expression with one or more groups e.g. (.*). If this omitted, the whole field is replaced.

#### Example: a pool per country
```
    <FieldMask>
      <FieldName>CustomerCity</FieldName>
      <MaskType>ReplacementQuery</MaskType>
      <QueryReplacement 
        SelectorField="CustomerCountry" 
        GroupField="cityCountryName" 
        ValueField="cityName" 
        Query="SELECT cityName, cityCountryName FROM cities" />
    </FieldMask>  
```

With the cities reference table of the Query mask type, every customer city gets a city of its country, and the same city is always replaced by the same one.

#### Example: a mapping table with a different mapping per country
```
    <FieldMask>
      <FieldName>CustomerFullname</FieldName>
      <MaskType>ReplacementQuery</MaskType>
      <OnListExhausted>Asterisk</OnListExhausted>
      <Asterisk>#</Asterisk>
      <QueryReplacement 
        SelectorField="CustomerCountry" 
        GroupField="country" 
        KeyField="originalName" 
        ValueField="pseudonym" 
        Query="SELECT originalName, pseudonym, country FROM name_mapping" />
    </FieldMask>  
```

| originalName | pseudonym | country |
|--------------|-----------|---------|
| Nick         | John      | GR      |
| Nick         | Giorgos   | CH      |
| Eva          | Maria     | GR      |

_name_mapping table_

Sample input and output

| CustomerCountry | CustomerFullname (original) | CustomerFullname (masked) |
|-----------------|-----------------------------|---------------------------|
| GR              | Nick                        | John                      |
| CH              | Nick                        | Giorgos                   |
| GR              | eva                         | Maria                     |
| GR              | Zed                         | ###                       |
