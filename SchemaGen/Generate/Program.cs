using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generate
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                var schema = JsonSchema4.FromFileAsync(@"3DTileSchema\tileset.schema.json");
                var settings = new CSharpGeneratorSettings();
                settings.Namespace = "Unity3DTiles.Schema";
                settings.ClassStyle = CSharpClassStyle.Poco;
                settings.ArrayType = typeof(Array).ToString();
                settings.GenerateDataAnnotations = false;
                var generator = new CSharpGenerator(schema.Result, settings);
                var file = generator.GenerateFile();
                File.WriteAllText(@"..\..\..\..\Assets\Unity3DTiles\Schema\TilesetSchema.cs", file);
            }
            
        }
    }
}
