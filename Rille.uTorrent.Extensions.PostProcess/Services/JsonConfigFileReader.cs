using System.IO;
using Newtonsoft.Json.Linq;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    public class JsonConfigFileReader
    {
        private readonly string _jsonFilePath;

        private JObject _parsedFile;

        public JObject ParsedConfigJObject
        {
            get
            {
                if (_parsedFile == null)
                    _parsedFile = JObject.Parse(File.ReadAllText(_jsonFilePath));
                return _parsedFile;
            }
        }


        public JsonConfigFileReader(string jsonFilePath = "./config.json")
        {
            _jsonFilePath = jsonFilePath;
        }

        /// <summary>
        /// Scans jsonfile that is expected to be in the following example: {"SomeKey": "SomeValue", "SomeOtherKey":"SomeOtherValue"}
        /// And returns the value. Exception if not found.
        /// </summary>
        public T GetValue<T>(string key)
        {
            // Make sure config.json exists, is set to 'Content' and 'Copy Always'.
            var value = ParsedConfigJObject[key];
            return value.ToObject<T>();
        }

    }
}
