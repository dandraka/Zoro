using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for Word documents (.docx).
    /// </summary>
    internal class DataMaskingDocxData : DataMaskingOfficeData<WordprocessingDocument>
    {
        internal DataMaskingDocxData(MaskConfig config) : base(config)
        {
        }

        protected override WordprocessingDocument GetData()
        {
            CopyInputToOutput();
            return WordprocessingDocument.Open(config.OutputFile, true);
        }

        protected override void Anonymize(WordprocessingDocument doc)
        {
            PrepareTextMasks();

            var body = doc.MainDocumentPart.Document.Body;

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                foreach (var segment in GetTextSegments(paragraph))
                {
                    var original = segment.Select(t => t.Text ?? string.Empty).ToList();
                    var masked = MaskRunTexts(original);
                    for (int i = 0; i < segment.Count; i++)
                    {
                        if (masked[i] != original[i])
                        {
                            segment[i].Text = masked[i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Splits the text of a paragraph in parts of text elements that directly follow each other,
        /// because Word often splits a sentence in many runs. A tab, a line break, a field or the like
        /// ends a part, so that words on its two sides are not glued together.
        /// </summary>
        private static IEnumerable<List<Text>> GetTextSegments(Paragraph paragraph)
        {
            var segment = new List<Text>();

            // runs of nested paragraphs, e.g. in text boxes, are handled with their own paragraph
            foreach (var run in paragraph.Descendants<Run>().Where(r => r.Ancestors<Paragraph>().First() == paragraph))
            {
                foreach (var child in run.ChildElements)
                {
                    if (child is Text text)
                    {
                        segment.Add(text);
                    }
                    else if (!(child is RunProperties) && !(child is LastRenderedPageBreak) && segment.Count > 0)
                    {
                        yield return segment;
                        segment = new List<Text>();
                    }
                }
            }

            if (segment.Count > 0)
            {
                yield return segment;
            }
        }
    }
}
