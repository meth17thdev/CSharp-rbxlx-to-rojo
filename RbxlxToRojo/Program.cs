using System.Xml.Linq;
using Newtonsoft.Json;

namespace RbxlToRojo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: RbxlxToRojo <input.rbxlx> <output_directory>");
                Console.WriteLine("Example: RbxlxToRojo mygame.rbxlx C:\\Projects\\MyRojoProject");
                return;
            }

            var inputFile = args[0];
            var outputDir = args[1];

            try
            {
                ConvertRbxlxToRojo(inputFile, outputDir);
                Console.WriteLine($"Project successfully created at {outputDir}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private static void ConvertRbxlxToRojo(string rbxlxPath, string outputDir)
        {
            if (!File.Exists(rbxlxPath))
                throw new FileNotFoundException("Input .rbxlx file not found");

            if (!rbxlxPath.EndsWith(".rbxlx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Input file must be .rbxlx");

            Directory.CreateDirectory(outputDir);
            var srcDir = Path.Combine(outputDir, "src");
            Directory.CreateDirectory(srcDir);

            XDocument doc;
            try
            {
                doc = XDocument.Load(rbxlxPath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse .rbxlx file", ex);
            }

            CreateRojoProject(rbxlxPath, outputDir, doc);
        }

        private static void CreateRojoProject(string rbxlxPath, string outputDir, XDocument doc)
        {
            var projectData = new
            {
                name = Path.GetFileNameWithoutExtension(rbxlxPath),
                tree = new Dictionary<string, object>
                {
                    ["$className"] = "DataModel",
                    ["ReplicatedStorage"] = new Dictionary<string, object>
                    {
                        ["$className"] = "ReplicatedStorage",
                        ["$path"] = "src/ReplicatedStorage"
                    },
                    ["ServerScriptService"] = new Dictionary<string, object>
                    {
                        ["$className"] = "ServerScriptService",
                        ["$path"] = "src/ServerScriptService"
                    },
                    ["Workspace"] = new Dictionary<string, object>
                    {
                        ["$className"] = "Workspace",
                        ["$path"] = "src/Workspace"
                    }
                }
            };

            File.WriteAllText(
                Path.Combine(outputDir, "default.project.json"),
                JsonConvert.SerializeObject(projectData, Formatting.Indented)
            );

            var srcDir = Path.Combine(outputDir, "src");
            ProcessItems(doc.Root, srcDir);
        }

        private static void ProcessItems(XElement parent, string currentPath)
        {
            foreach (var item in parent.Elements("Item"))
            {
                var className = item.Attribute("class")?.Value ?? "Unknown";
                var itemName = GetItemName(item);

                var itemPath = Path.Combine(currentPath, SanitizePath(itemName));
                Directory.CreateDirectory(itemPath);
                
                if (className is "LocalScript" or "Script" or "Module")
                {
                    CreateScriptFile(item, className, itemPath);
                    continue;
                }

                CreateInitFile(item, className, itemPath);
                ProcessItems(item, itemPath);
            }
        }

        private static string GetScriptExtension(string className)
        {
            Console.WriteLine(className);
            return className switch
            {
                "LocalScript" => "client",
                "Script" => "server",
                "Module" => "",
                _ => throw new ArgumentOutOfRangeException(nameof(className), className, null)
            };
        }

        private static void CreateScriptFile(XElement item, string className, string itemPath)
        {
            var propsElement = item.Element("Properties");
            if (propsElement == null) return;

            var name = propsElement.Elements().First(prop => prop.Attribute("name")?.Value == "Name").Value + "." + GetScriptExtension(className) + ".luau";
            var content = propsElement.Elements().First(prop => prop.Attribute("name")?.Value == "Source").Value.Replace("<![CDATA[", "").Replace("]]", "");

            File.WriteAllText(Path.Combine(itemPath, name), content);
        }

        private static string GetItemName(XElement item)
        {
            var nameProp = item.Elements("Properties")
                             .Elements("string")
                             .FirstOrDefault(e => e.Attribute("name")?.Value == "Name");

            return nameProp?.Value ?? item.Attribute("referent")?.Value ?? "Unnamed";
        }

        private static void CreateInitFile(XElement item, string className, string itemPath)
        {
            var properties = new Dictionary<string, object?>();
            var propsElement = item.Element("Properties");

            var name = "";

            if (propsElement != null)
            {
                foreach (var prop in propsElement.Elements())
                {
                    if (PropertyParser.TryParse(prop, out var value))
                    {
                        var propName = prop.Attribute("name")?.Value;
                        if (propName == "Name")
                        {
                            name = (string)value;
                            continue;
                        }
                        properties.Add(propName, value);
                    }
                }
            }

            var initContent = new
            {
                Name = name,
                ClassName = className,
                Properties = properties
            };

            File.WriteAllText(
                Path.Combine(itemPath, $"{name}.meta.json"),
                JsonConvert.SerializeObject(initContent, Formatting.Indented)
            );
        }

        private static string SanitizePath(string path)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                path = path.Replace(c, '_');
            }
            return path;
        }
    }
}