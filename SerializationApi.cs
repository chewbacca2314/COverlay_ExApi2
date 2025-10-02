using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace cOverlay
{
    public class SerializationApi
    {
        public static void Serialize<T>(T settings, string key)
        {
            string directoryPath = Path.GetDirectoryName(key);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(key))
            {
                File.Create(key).Close();
            }

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);

            for (int i = 0; i < 15; i++)
            {
                try
                {
                    File.WriteAllText(key, json);
                    return;
                }
                catch (Exception)
                {
                }
            }
        }

        public static T Deserialize<T>(string key)
        {
            if (File.Exists(key))
            {
                var text = File.ReadAllText(key);
                var obj = JsonConvert.DeserializeObject<T>(text);

                if (obj == null)
                {
                    return default(T);
                }

                return obj;
            }

            return default(T);
        }
    }
}