using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Dandraka.Zoro.Processor
{
    internal class JsonFileWriter : IDataWriter<JContainer>
    {
        private readonly MaskConfig config;

        internal JsonFileWriter(MaskConfig config)
        {
            this.config = config;
        }

        public void Write(JContainer json)
        {
            File.WriteAllText(config.OutputFile, json.ToString(), Encoding.UTF8);
        }
    }
}
