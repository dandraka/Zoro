using System;

namespace Dandraka.Zoro.Processor
{
    /// <summary>
    /// Template for all masking implementations: read, anonymize, write.
    /// </summary>
    /// <typeparam name="T">The in-memory representation of the data, e.g. a DataTable.</typeparam>
    internal abstract class DataMasking<T> : DataMasking
    {
        protected DataMasking(MaskConfig config) : base(config)
        {
        }

        protected abstract T GetData();

        protected abstract void Anonymize(T data);

        public override void Mask()
        {
            Validator.PerformSanityChecks(this.config);

            // created first so that an unsupported source/destination pair fails before any data is read
            var writer = CreateWriter();

            T data = GetData();
            Anonymize(data);
            writer.Write(data);
        }

        private IDataWriter<T> CreateWriter()
        {
            object writer = config.DataDestination switch
            {
                DataDestination.CsvFile => (object)new CsvFileWriter(config),
                DataDestination.JsonFile => new JsonFileWriter(config),
                DataDestination.Database => new DbWriter(config),
                DataDestination.DocXFile => new DocxFileWriter(config),
                DataDestination.XlsXFile => new XlsxFileWriter(config),
                _ => throw new NotSupportedException(),
            };

            if (writer is IDataWriter<T> typedWriter)
            {
                return typedWriter;
            }

            throw new NotSupportedException($"Data source {config.DataSource} cannot be written to destination {config.DataDestination}.");
        }
    }
}
