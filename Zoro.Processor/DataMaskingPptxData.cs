using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Masking for PowerPoint presentations (.pptx).
    /// Masks the slides (hidden ones too), the layouts and masters they use, and the speaker notes.
    /// Charts and SmartArt are not masked.
    /// </summary>
    internal class DataMaskingPptxData : DataMaskingOfficeData<PresentationDocument>
    {
        internal DataMaskingPptxData(MaskConfig config) : base(config)
        {
        }

        protected override PresentationDocument GetData()
        {
            CopyInputToOutput();
            return PresentationDocument.Open(config.OutputFile, true);
        }

        protected override void Anonymize(PresentationDocument doc)
        {
            PrepareTextMasks();

            // layouts and masters are shared by many slides, so Distinct() masks them once
            var parts = doc.PresentationPart.SlideParts
                .SelectMany(s => new OpenXmlPart[] { s, s.SlideLayoutPart, s.SlideLayoutPart?.SlideMasterPart, s.NotesSlidePart })
                .Where(p => p != null)
                .Distinct();

            foreach (var part in parts)
            {
                foreach (var paragraph in part.RootElement.Descendants<A.Paragraph>())
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
        }

        /// <summary>
        /// Splits a paragraph in parts of runs that directly follow each other, because PowerPoint often splits
        /// a sentence in many runs (spell check, language, formatting). A line break or a field (e.g. the slide number)
        /// ends a part, so that words on its two sides are not glued together.
        /// </summary>
        private static IEnumerable<List<A.Text>> GetTextSegments(A.Paragraph paragraph)
        {
            var segment = new List<A.Text>();

            foreach (var child in paragraph.ChildElements)
            {
                if (child is A.Run run)
                {
                    if (run.Text != null)
                    {
                        segment.Add(run.Text);
                    }
                }
                else if ((child is A.Break || child is A.Field) && segment.Count > 0)
                {
                    yield return segment;
                    segment = new List<A.Text>();
                }
            }

            if (segment.Count > 0)
            {
                yield return segment;
            }
        }
    }
}
