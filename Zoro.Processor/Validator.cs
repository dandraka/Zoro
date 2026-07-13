using System;

namespace Dandraka.Zoro.Processor
{
    internal static class Validator
    {
        internal static void PerformSanityChecks(MaskConfig config)
        {
            switch (config.DataSource)
            {
                case DataSource.DocXFile:
                case DataSource.XlsXFile:
                    if (config.DataSource == DataSource.DocXFile && config.DataDestination != DataDestination.DocXFile) { throw new NotSupportedException("For docx source, only docx destination is supported."); }
                    if (config.DataSource == DataSource.XlsXFile && config.DataDestination != DataDestination.XlsXFile) { throw new NotSupportedException("For xlsx source, only xlsx destination is supported."); }
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
    }
}