using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Xml.Linq;

namespace RbxlToRojo;

public static class PropertyParser
{
    /*private static float ParseAttributeAsFloat(this XElement prop, string name)
    {
        var value = prop.Attribute(name)?.Value;
        if (value == null)
        {
            Console.WriteLine($"not found attribute {name} at {prop.Attribute("name").Value}");
            return 0;
        }
        return float.Parse(value.Contains('.') ? value.Replace('.', ',') : value);
    }*/
    
    private static float ParseElementAsFloat(this XElement prop, string name)
    {
        var value = prop.Element(name)?.Value;
        if (value == null)
        {
            Console.WriteLine($"not found element {name} at {prop.Attribute("name").Value}");
            return 0;
        }
        return float.Parse(value.Contains('.') ? value.Replace('.', ',') : value);
    }

    private static float ParseAsFloat(this XElement prop)
    {
        var value = prop.Value;
        return float.Parse(value.Contains('.') ? value.Replace('.', ',') : value);
    }

    private static double ParseAsDouble(this XElement prop)
    {
        var value = prop.Value;
        return double.Parse(value.Contains('.') ? value.Replace('.', ',') : value);
    }

    public static bool TryParse(XElement prop, out object? value)
    {
        var propName = prop.Attribute("name")?.Value;
        var propValue = prop.Value;
        try
        {
            value = prop.Name.LocalName switch
            {
                "string" => propValue,
                "bool" => bool.Parse(propValue),
                "int" => int.Parse(propValue),
                "int64" => long.Parse(propValue),
                "float" => prop.ParseAsFloat(),
                "float64" => prop.ParseAsDouble(),
                "BinaryString" => ParseBinaryString(prop),
                "SharedString" => propValue,
                "Content" => prop.Element("url")?.Value,
                "UniqueId" => new
                {
                    UniqueId = propValue
                },
                "CoordinateFrame" => ParseCoordinateFrame(prop),
                "Vector2" => ParseVector2(prop),
                "Vector3" => ParseVector3(prop),
                "Color3" => ParseColor3(prop),
                "Color3uint8" => ParseColor3Uint8(prop),
                "UDim" => ParseUDim(prop),
                "UDim2" => ParseUDim2(prop),
                "NumberRange" => ParseNumberRange(prop),
                "PhysicalProperties" => ParsePhysicalProperties(prop),
                "Rect" => ParseRect(prop),
                "token" => prop.Value,
                "ColorSequence" => ParseColorSequence(prop),
                "NumberSequence" => ParseNumberSequence(prop),
                "Font" => ParseFont(prop),
                _ => null
            };

            return value != null;
        }
        catch (Exception e)
        {
            Console.WriteLine($"prop name: {propName}, prop type: {prop.Name.LocalName}, prop value: {propValue}");
            throw;
        }
    }

    private static object ParseBinaryString(XElement prop)
    {
        var name = prop.Attribute("name")?.Value;

        if (name == "Tags") return ParseTags(prop);
        
        return new
        {
            BinaryString = prop.Value
        };
    }

    private static object ParseCoordinateFrame(XElement prop)
    {
        var X = prop.ParseElementAsFloat("X");
        var Y = prop.ParseElementAsFloat("Y");
        var Z = prop.ParseElementAsFloat("Z");
        var R00 = prop.ParseElementAsFloat("R00");
        var R01 = prop.ParseElementAsFloat("R01");
        var R02 = prop.ParseElementAsFloat("R02");
        var R10 = prop.ParseElementAsFloat("R10");
        var R11 = prop.ParseElementAsFloat("R11");
        var R12 = prop.ParseElementAsFloat("R12");
        var R20 = prop.ParseElementAsFloat("R20");
        var R21 = prop.ParseElementAsFloat("R21");
        var R22 = prop.ParseElementAsFloat("R22");
        
        return new 
        {
            CFrame = new
            {
                position = (float[])[X, Y, Z],
                orientation = (float[][])[(float[])[R00, R01, R02], (float[])[R10, R11, R12], (float[])[R20, R21, R22]]
            }
        };
    }

    private static object ParseVector2(XElement prop) => new
    {
        Vector2 = (float[])
            [prop.ParseElementAsFloat("X"), prop.ParseElementAsFloat("Y")]
    };

    private static object ParseVector3(XElement prop) => new
    {
        Vector3 = (float[])
            [prop.ParseElementAsFloat("X"), prop.ParseElementAsFloat("Y"), prop.ParseElementAsFloat("Z")]
    };

    private static object ParseColor3(XElement prop) => new
    {
        Color3 = (float[])
            [prop.ParseElementAsFloat("R"), prop.ParseElementAsFloat("G"), prop.ParseElementAsFloat("B")]
    };

    private static object ParseColor3Uint8(XElement prop)
    {
        var colorValue = uint.Parse(prop.Value);
        return new
        {
            Color3uint8 = (float[])
                [(colorValue >> 16) & 0xFF, (colorValue >> 8) & 0xFF, colorValue & 0xFF]
        };
    }

    private static object ParseUDim(XElement prop) => new
    {
        UDim = (float[])
            [prop.ParseElementAsFloat("S"), prop.ParseElementAsFloat("O")]
    };

    private static object ParseUDim2(XElement prop) => new
    {
        UDim2 = (float[][])
            [[prop.ParseElementAsFloat("XS"), prop.ParseElementAsFloat("XO")], [prop.ParseElementAsFloat("YS"), prop.ParseElementAsFloat("YO")]]
    };

    private static object ParseNumberRange(XElement prop)
    {
        var parts = prop.Value.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var selected = parts.Select(part => part.Replace('.', ',')).ToArray();
        return new
        {
            NumberRange = (float[]) [float.Parse(selected[0]), float.Parse(selected[1])]
        };
    }

    private static object ParsePhysicalProperties(XElement prop)
    {
        var customPhysics = prop.Element("CustomPhysics")?.Value == "true";
        if (!customPhysics)
            return new { PhysicalProperties = "Default" };

        return new
        {
            CustomPhysics = true,
            density = prop.ParseElementAsFloat("Density"),
            friction = prop.ParseElementAsFloat("Friction"),
            elasticity = prop.ParseElementAsFloat("Elasticity"),
            frictionWeight = prop.ParseElementAsFloat("FrictionWeight"),
            elasticityWeight = prop.ParseElementAsFloat("ElasticityWeight")
        };
    }

    private static object ParseRect(XElement prop) => new
    {
        Rect = (float[][])
        [
            [prop.ParseElementAsFloat("MinX"), prop.ParseElementAsFloat("MinY")],
            [prop.ParseElementAsFloat("MaxX"), prop.ParseElementAsFloat("MaxY")]
        ]
    };

    private static object ParseColorSequence(XElement prop)
    {
        var keypoints = new List<Dictionary<string, object>>();

        foreach (var keypointElement in prop.Elements("Keypoint"))
        {
            var time = ParseElementAsFloat(keypointElement, "Time");
            var colorElement = keypointElement.Element("Value");
        
            var color = new[]
            {
                float.Parse(colorElement.Element("R")?.Value ?? "0", CultureInfo.InvariantCulture),
                float.Parse(colorElement.Element("G")?.Value ?? "0", CultureInfo.InvariantCulture),
                float.Parse(colorElement.Element("B")?.Value ?? "0", CultureInfo.InvariantCulture)
            };
        
            keypoints.Add(new Dictionary<string, object>
            {
                ["time"] = time,
                ["color"] = color
            });
        }

        return new Dictionary<string, object>
        {
            ["ColorSequence"] = new Dictionary<string, object>
            {
                ["keypoints"] = keypoints
            }
        };
    }

    private static object ParseNumberSequence(XElement prop)
    {
        var keypoints = new List<Dictionary<string, object>>();

        foreach (var keypointElement in prop.Elements("Keypoint"))
        {
            keypoints.Add(new Dictionary<string, object>
            {
                ["time"] = ParseElementAsFloat(keypointElement, "Time"),
                ["value"] = ParseElementAsFloat(keypointElement, "Value"),
                ["envelope"] = ParseElementAsFloat(keypointElement, "Envelope")
            });
        }

        return new
        {
            NumberSequence = new Dictionary<string, object>
            {
                ["keypoints"] = keypoints
            }
        };
    }

    private static object ParseFont(XElement prop)
    {
        var weightNum = prop.ParseElementAsFloat("Weight");
        
        return new
        {
            family = prop.Element("Family")?.Element("url")?.Value,
            weight = GetWeight(weightNum),
            style = prop.Element("Style")?.Value,
            //cachedFaceId = prop.Element("CachedFaceId")?.Element("url")?.Value
        };
    }

    private static object ParseTags(XElement prop)
    {
        var value = prop.Value;
        
        if (string.IsNullOrEmpty(value))
        {
            return new
            {
                Tags = Array.Empty<string>()
            };
        }

        var tagBytes = Convert.FromBase64String(value);
        var tags = Encoding.UTF8.GetString(tagBytes).Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(t => !string.IsNullOrWhiteSpace(t));
            
        return new
        {
            Tags = (string[])tags
        };
    }

    private static string GetWeight(float weight)
    {
        return weight switch
        {
            100 => "Thin",
            200 => "ExtraLight",
            300 => "Light",
            350 => "SemiLight",
            400 => "Regular", // Default
            500 => "Medium",
            600 => "SemiBold",
            700 => "Bold",
            800 => "ExtraBold",
            900 => "Heavy",
            950 => "ExtraHeavy",
            _ => throw new ArgumentOutOfRangeException(nameof(weight), weight, null)
        };
    }

    private static object? ParseEnum(XElement prop)
    {
        //var propName = prop.Attribute("name")?.Value;
        return "0";
    }
}