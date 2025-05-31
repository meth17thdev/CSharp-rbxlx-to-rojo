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

    public static bool TryParse(XElement prop, out object? value)
    {
        var propName = prop.Attribute("name")?.Value;
        var propValue = prop.Value;
        try
        {
            value = prop.Name.LocalName switch
            {
                "string" => prop.Value,
                "bool" => prop.Value == "true",
                "int" => int.Parse(prop.Value),
                "int64" => long.Parse(prop.Value),
                "float" => prop.ParseAsFloat(),
                "double" => double.Parse(prop.Value.Replace('.', ',')),
                "BinaryString" => prop.Value,
                "SharedString" => prop.Value,
                "Content" => new { Url = prop.Element("url")?.Value },
                "Ref" => new { Reference = prop.Value },
                "UniqueId" => prop.Value,
                "CoordinateFrame" => ParseCoordinateFrame(prop),
                "Vector2" => new
                {
                    X = prop.ParseElementAsFloat("X"), 
                    Y = prop.ParseElementAsFloat("Y")
                },
                "Vector3" => new
                {
                    X = prop.ParseElementAsFloat("X"),
                    Y = prop.ParseElementAsFloat("Y"),
                    Z = prop.ParseElementAsFloat("Z")
                },
                "Color3" => new
                {
                    R = prop.ParseElementAsFloat("R"),
                    G = prop.ParseElementAsFloat("G"),
                    B = prop.ParseElementAsFloat("B")
                },
                "Color3uint8" => ParseColor3Uint8(prop),
                "UDim" => new
                {
                    S = prop.ParseElementAsFloat("S"),
                    O = prop.ParseElementAsFloat("O")
                },
                "UDim2" => new
                {
                    XS = prop.ParseElementAsFloat("XS"),
                    XO = prop.ParseElementAsFloat("XO"),
                    YS = prop.ParseElementAsFloat("YS"),
                    YO = prop.ParseElementAsFloat("YO")
                },
                "NumberRange" => ParseNumberRange(prop),
                "PhysicalProperties" => ParsePhysicalProperties(prop),
                "Rect2D" => new
                {
                    Min = new 
                    { 
                        X = prop.ParseElementAsFloat("MinX"),
                        Y = prop.ParseElementAsFloat("MinY")
                    },
                    Max = new
                    {
                        X = prop.ParseElementAsFloat("MaxX"),
                        Y = prop.ParseElementAsFloat("MaxY")
                    }
                },
                "token" => ParseEnum(prop),
                "SecurityCapabilities" => prop.Value,
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

    private static object ParseCoordinateFrame(XElement prop)
    {
        return new
        {
            X = prop.ParseElementAsFloat("X"),
            Y = prop.ParseElementAsFloat("Y"),
            Z = prop.ParseElementAsFloat("Z"),
            R00 = prop.ParseElementAsFloat("R00"),
            R01 = prop.ParseElementAsFloat("R01"),
            R02 = prop.ParseElementAsFloat("R02"),
            R10 = prop.ParseElementAsFloat("R10"),
            R11 = prop.ParseElementAsFloat("R11"),
            R12 = prop.ParseElementAsFloat("R12"),
            R20 = prop.ParseElementAsFloat("R20"),
            R21 = prop.ParseElementAsFloat("R21"),
            R22 = prop.ParseElementAsFloat("R22")
        };
    }

    private static object ParseColor3Uint8(XElement prop)
    {
        uint colorValue = uint.Parse(prop.Value);
        return new
        {
            R = (colorValue >> 16) & 0xFF,
            G = (colorValue >> 8) & 0xFF,
            B = colorValue & 0xFF
        };
    }

    private static object ParseNumberRange(XElement prop)
    {
        var parts = prop.Value.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        var selected = parts.Select(part => part.Replace('.', ',')).ToArray();
        return new
        {
            Min = float.Parse(selected[0]),
            Max = float.Parse(selected[1])
        };
    }

    private static object ParsePhysicalProperties(XElement prop)
    {
        var customPhysics = prop.Element("CustomPhysics")?.Value == "true";
        if (!customPhysics)
            return new { CustomPhysics = false };

        return new
        {
            CustomPhysics = true,
            Density = prop.ParseElementAsFloat("Density"),
            Friction = prop.ParseElementAsFloat("Friction"),
            Elasticity = prop.ParseElementAsFloat("Elasticity"),
            FrictionWeight = prop.ParseElementAsFloat("FrictionWeight"),
            ElasticityWeight = prop.ParseElementAsFloat("ElasticityWeight")
        };
    }

    private static object ParseFont(XElement prop)
    {
        return new
        {
            Family = prop.Element("Family")?.Element("url")?.Value,
            Weight = prop.ParseElementAsFloat("Weight"),
            Style = prop.Attribute("Style")?.Value,
            CachedFaceId = prop.Element("CachedFaceId")?.Element("url")?.Value
        };
    }

    private static object? ParseEnum(XElement prop)
    {
        //var propName = prop.Attribute("name")?.Value;
        return "0";
    }
}