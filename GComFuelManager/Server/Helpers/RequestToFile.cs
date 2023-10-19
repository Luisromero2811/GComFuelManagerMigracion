using Newtonsoft.Json;
//using ServiceReference2; //QA
using ServiceReference7; //Producción
using System.Diagnostics;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;

namespace GComFuelManager.Server.Helpers
{
    public class RequestToFile
    {
        public void GenerateFile(string value, string name, string foldername)
        {
            if (!Directory.Exists("Files"))
                Directory.CreateDirectory("Files");

            var path = string.IsNullOrEmpty(foldername) ? "default" : foldername;

            var pathFile = Directory.Exists($"Files/{path}");

            if (!pathFile)
                Directory.CreateDirectory($"Files/{path}");

            JsonDocument jsonDoc = JsonDocument.Parse(value);

            using (FileStream file = File.Create($"Files/{path}/{name}"))
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(file, new JsonWriterOptions { Indented = true }))
                {
                    jsonDoc.WriteTo(writer);
                }
            }
        }

        public void GenerateFileXML(string name, string foldername, WsSaveBillOfLadingRequest type)
        {
            if (!Directory.Exists("Files"))
                Directory.CreateDirectory("Files");

            var path = string.IsNullOrEmpty(foldername) ? "default" : foldername;

            var pathFile = Directory.Exists($"Files/{path}");

            if (!pathFile)
                Directory.CreateDirectory($"Files/{path}");

            XmlSerializer serializer = new XmlSerializer(typeof(WsSaveBillOfLadingRequest));

            using (StreamWriter file = new StreamWriter($"Files/{path}/{name}"))
            {
                serializer.Serialize(file,type);
            }
        }

        public void GenerateFileXMLResponse(string name, string foldername, WsBillOfLadingResponse type)
        {
            if (!Directory.Exists("Files"))
                Directory.CreateDirectory("Files");

            var path = string.IsNullOrEmpty(foldername) ? "default" : foldername;

            var pathFile = Directory.Exists($"Files/{path}");

            if (!pathFile)
                Directory.CreateDirectory($"Files/{path}");

            XmlSerializer serializer = new XmlSerializer(typeof(WsBillOfLadingResponse));

            using (StreamWriter file = new StreamWriter($"Files/{path}/{name}"))
            {
                serializer.Serialize(file, type);
            }
        }
    }
}
