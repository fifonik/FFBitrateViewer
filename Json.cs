using System.IO;
using Newtonsoft.Json;


namespace FFBitrateViewer
{
    public class Json
    {
        public static T? FileRead<T>(string fs)
        {
            // todo@ read from stream (as in IsValid)
            string? text = File.ReadAllText(fs);
            return JsonConvert.DeserializeObject<T>(text); // todo@ add options
        }


        public static void FileWrite<T>(string fs, T obj)
        {
            File.WriteAllText(fs, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }


        public static bool IsValid<T>(string fs)
        {
            try
            {
                using StreamReader file = File.OpenText(fs);
                var serializer = new JsonSerializer();
                var data = (T?)serializer.Deserialize(file, typeof(T?));
                return data != null;
            } catch
            {
                // just catch any error
            }
            return false;
        }
    }
}