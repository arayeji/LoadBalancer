using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace LoadBalancer
{
    public class Config
    {
        public List<LoadBalancerBase> ServerGroups = new List<LoadBalancerBase>();

        public void Load()
        {
            try
            {
                if (File.Exists("Config.cfg"))
                {
                    var jsonSettings = new JsonSerializerSettings();
                    jsonSettings.Converters.Add(new IPConverter());
                    jsonSettings.Converters.Add(new IPEndPointConverter());
                    jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.JavaScriptDateTimeConverter());
                    jsonSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    jsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All;
                    ServerGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText("Config.cfg"), jsonSettings).ServerGroups;
                }
                else
                {
                    File.WriteAllText("Config.cfg", Newtonsoft.Json.JsonConvert.SerializeObject(this));
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void Reload(string FileName)
        {
            if (File.Exists(FileName))
            {
                var jsonSettings = new JsonSerializerSettings();
                jsonSettings.Converters.Add(new IPConverter());
                jsonSettings.Converters.Add(new IPEndPointConverter());
                jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.JavaScriptDateTimeConverter());
                jsonSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                jsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
                ServerGroups = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(FileName), jsonSettings).ServerGroups;
                Console.WriteLine("Config Reloaded!");
            }
        }

        public void Save(string FileName)
        {
            var jsonSettings = new JsonSerializerSettings();
            jsonSettings.Converters.Add(new IPConverter());
            jsonSettings.Converters.Add(new IPEndPointConverter());
            jsonSettings.Converters.Add(new Newtonsoft.Json.Converters.JavaScriptDateTimeConverter());
            jsonSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            jsonSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
            File.WriteAllText(FileName, JsonConvert.SerializeObject((this), Formatting.Indented, jsonSettings));
        }

        public class IPConverter : JsonConverter<IPAddress>
        {
            public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer)
            {
                writer.WriteValue(value.ToString());
            }

            public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;
                return IPAddress.Parse(s);
            }
        }
        public class IPEndPointConverter : JsonConverter<IPEndPoint>
        {
            public override void WriteJson(JsonWriter writer, IPEndPoint value, JsonSerializer serializer)
            {
                IPEndPoint ep = (IPEndPoint)value;
                JObject jo = new JObject();
                jo.Add("Address", JToken.FromObject(ep.Address, serializer));
                jo.Add("Port", ep.Port);
                jo.WriteTo(writer);
            }

            public override IPEndPoint ReadJson(JsonReader reader, Type objectType, IPEndPoint existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                JObject jo = JObject.Load(reader);
                IPAddress address = jo["Address"].ToObject<IPAddress>(serializer);
                int port = (int)jo["Port"];
                return new IPEndPoint(address, port);
            }
        }

    }
}
