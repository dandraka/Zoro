using System;
using System.IO;
using System.Linq;

namespace Dandraka.Zoro.Processor
{
    internal static class Validator
    {
        internal static void PerformSanityChecks(MaskConfig config)
        {
            CheckReplacementCsv(config);

            switch (config.DataSource)
            {
                case DataSource.DocXFile:
                case DataSource.XlsXFile:
                case DataSource.PptXFile:
                    if (config.DataSource == DataSource.DocXFile && config.DataDestination != DataDestination.DocXFile) { throw new NotSupportedException("For docx source, only docx destination is supported."); }
                    if (config.DataSource == DataSource.XlsXFile && config.DataDestination != DataDestination.XlsXFile) { throw new NotSupportedException("For xlsx source, only xlsx destination is supported."); }
                    if (config.DataSource == DataSource.PptXFile && config.DataDestination != DataDestination.PptXFile) { throw new NotSupportedException("For pptx source, only pptx destination is supported."); }
                    foreach (var m in config.FieldMasks)
                    {
                        if (m.MaskType == MaskType.Expression) { throw new NotSupportedException("For Office file types, the Expression mask type is not supported."); }
                        // selector and group are ignored
                        if (m.QueryReplacement != null)
                        {
                            if (!string.IsNullOrWhiteSpace(m.QueryReplacement.SelectorField)) { m.QueryReplacement.SelectorField = string.Empty; }
                            if (!string.IsNullOrWhiteSpace(m.QueryReplacement.GroupDbField)) { m.QueryReplacement.GroupDbField = string.Empty; }
                        }
                        if (m.ListOfPossibleReplacements != null && m.ListOfPossibleReplacements.Count > 0)
                        {
                            foreach (var r in m.ListOfPossibleReplacements)
                            {
                                r.Selector = string.Empty;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }        

        private static void CheckReplacementCsv(MaskConfig config)
        {
            foreach (var mask in config.FieldMasks)
            {
                var csvItems = (mask.ListOfPossibleReplacements ?? new System.Collections.Generic.List<Replacement>())
                    .OfType<ReplacementCSV>().ToList();
                if (csvItems.Count == 0)
                {
                    continue;
                }

                string name = $"The ReplacementCSV of field '{mask.FieldName}'";
                if (mask.MaskType != MaskType.ReplacementList)
                {
                    throw new InvalidDataException($"{name} is only supported with the ReplacementList mask type.");
                }
                if (mask.ListOfPossibleReplacements.Count != csvItems.Count)
                {
                    throw new InvalidDataException($"{name} cannot be used together with Replacement items.");
                }
                if (csvItems.Count > 1)
                {
                    throw new InvalidDataException($"{name} can only be used once.");
                }

                var csv = csvItems[0];
                if (string.IsNullOrWhiteSpace(csv.CSVFileName)) { throw new InvalidDataException($"{name} must have a CSVFileName."); }
                if (string.IsNullOrWhiteSpace(csv.FromFieldName)) { throw new InvalidDataException($"{name} must have a FromFieldName."); }
                if (string.IsNullOrWhiteSpace(csv.ToFieldName)) { throw new InvalidDataException($"{name} must have a ToFieldName."); }
                if (!string.IsNullOrEmpty(csv.Selector) || !string.IsNullOrEmpty(csv.ReplacementList) || csv.Mappings.Count > 0)
                {
                    throw new InvalidDataException($"{name} cannot have a Selector, a List or Map items.");
                }
            }
        }
    }
}