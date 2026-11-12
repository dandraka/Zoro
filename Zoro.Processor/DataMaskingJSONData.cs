using System;
using System.Data;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for nested data: JSON files.
    /// </summary>
    internal class DataMaskingJSONData : DataMasking<JContainer>
    {
        internal DataMaskingJSONData(MaskConfig config) : base(config)
        {
        }

        protected override JContainer GetData()
        {
            if (!File.Exists(config.InputFile))
            {
                throw new FileNotFoundException(config.InputFile);
            }

            string json = File.ReadAllText(config.InputFile);

            JContainer jsonObj;
            try
            {
                jsonObj = JObject.Parse(json);
            }
            catch (JsonReaderException)
            {
                jsonObj = JArray.Parse(json);
            }

            return jsonObj;
        }

        protected override void Anonymize(JContainer jsonContainer)
        {
            switch (jsonContainer.GetType().Name)
            {
                case "JArray":
                    foreach (var jsonItem in (JArray)jsonContainer)
                    {
                        foreach (JProperty c in (JToken)jsonItem)
                        {
                            AnonymizeJSONProperty(c);
                        }
                    }
                    break;
                case "JObject":
                    foreach (JProperty c in (JToken)jsonContainer)
                    {
                        AnonymizeJSONProperty(c);
                    }
                    break;
                default:
                    throw new NotImplementedException(jsonContainer.GetType().FullName);
            }
        }

        private protected override string GetFieldValue(string fieldName, DataRow row, JProperty jsonNode)
        {
            // field name is supposed to be a JsonPath
            var jsonPathResult = jsonNode.Parent.SelectToken(fieldName);
            if (jsonPathResult == null || jsonPathResult.Value<object>() == null)
            {
                jsonPathResult = jsonNode.Root.SelectToken(fieldName);
                if (jsonPathResult == null || jsonPathResult.Value<object>() == null)
                {
                    throw new FieldNotFoundException(fieldName);
                }
            }

            return (jsonPathResult as JValue).Value<string>();
        }

        private void AnonymizeJSONProperty(JProperty c)
        {
            string colName = c.Name;
            string value = Convert.ToString(c.Value);
            var fieldMask = config.FieldMasks.FirstOrDefault(x => x.FieldName == colName);
            if (fieldMask != null)
            {
                switch (c.Value.Type)
                {
                    case JTokenType.Integer:
                        string i = GetMaskedString(value, fieldMask, null, c);
                        c.Value = Convert.ToInt32(i);
                        break;
                    case JTokenType.Float:
                        string f = GetMaskedString(value, fieldMask, null, c);
                        c.Value = Convert.ToDecimal(f);
                        break;
                    default:
                        // treat non-numbers as strings
                        string s = GetMaskedString(value, fieldMask, null, c);
                        c.Value = s;
                        break;
                }
            }

            if (c.HasValues)
            {
                foreach (var jsonChild in c.Children().OfType<JContainer>())
                {
                    Anonymize((JContainer)jsonChild);
                }
            }
        }
    }
}
