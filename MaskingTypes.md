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
| Pseudonymisation              | Partially, current version & roadmap             | Keeping reference tables is out of scope but fake data can be consistently generated, e.g. Customer Rick Bears with CustomerID=5 can become Customer-5 |
| Shuffling                     | Fully, current version                           | The usage of a db query is needed |
| Noise Addition                | Partially, current version & fully on roadmap    | String-based noise fully supported, arithmetic noise currently not supported but is on the roadmap |
| Generalization                | Partially, current version & roadmap             | String-based generalization fully supported, arithmetic generalization is out of scope but most scenarios can be achieved using a db query |
| Synthetic (fake) data         | Fully, current version                           |                                   |
| Hiding                        | Fully, current version                           |                                   |

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

### MaskType=Similar

Each character of the original data is substituted by a random character taking the following into account:

* If the original character is a space or a symbol, it is preserved unchanged. E.g. @ stays @.
* If the original character is a letter or number, the random character is also a letter or number respectively. E.g. f might become g, 5 might become 2.
* If the original character is a letter, the random character prerves the case (upper/lower) and vowel/consonant of the original. E.g. a might become i, K might become D.

With the usage of a regular expression, it is possible to change all or only part of the original data.

#### Mandatory, optional and ignored fields

* MaskType = Similar
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
      <MaskType>Similar</MaskType>
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
      <MaskType>Similar</MaskType>
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
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced.
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

#### Mandatory, optional and ignored fields

* MaskType = Expression
* Asterisk: Ignored
* Expression: Mandatory. An expression consisting of fixed text and fields/JsonPaths enclosed in double curly brackets.
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Ignored
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced.
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
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced.
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

#### Mandatory, optional and ignored fields

* MaskType = Query
* Asterisk: Ignored
* Expression: Ignored
* FieldName: Mandatory. The name of the field being sought. Note that field names are case-insensitive for CSV files & DB queries, but case-sensitive for JSON files.
* ListOfPossibleReplacements: Ignored
* QueryReplacement: Mandatory. Must contain all of the following attributes:
  * SelectorField: The name of the field _from the original data_ which will be used to match the reference records.
  * GroupField: The name of the field from the reference query (see Query below) which needs to match the values from the SelectorField.
  * ValueField: The name of the field from the reference query (see Query below)which will be used as value after a random record (from the records where SelectorField=GroupField) is picked.
  * Query: The SQL query which will be executed to fetch the reference records.
* RegExGroupToReplace: Optional. A number that specifies which regex group will be replaced.
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
  <!-- Currently System.Data.SqlClient and System.Data.OleDb are supported, but if needed, adding more is trivial -->
  <ConnectionType>System.Data.SqlClient</ConnectionType>
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
