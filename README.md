# Zoro
### The masked avenger

Zoro is a data masking/anonymization utility. It processes CSV files.

### Usage:
Zoro.exe path_to_config_file

E.g. ```Zoro.exe c:\temp\mask.xml```

### Sample config file:
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

### Note:

Although not required by the license, the author kindly asks that you share any improvements you made.
