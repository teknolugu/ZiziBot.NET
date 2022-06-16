using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodingSeb.Localization.Loaders;
using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinTenDev.Zizi.Models.Factories;

public class AdvancedJsonLocalizationFileLoaderFactory : ILocalizationFileLoader
{
    public string LabelPathSeparator { get; set; } = ".";

    public string LabelPathRootPrefix { get; set; } = string.Empty;

    public string LabelPathSuffix { get; set; } = string.Empty;

    public bool CanLoadFile(string fileName) => fileName.TrimEnd().EndsWith(".loc.json", StringComparison.OrdinalIgnoreCase);

    public void LoadFile(
        string fileName,
        LocalizationLoader loader
    )
    {
        using (StreamReader reader = File.OpenText(fileName))
            ((JObject) JToken.ReadFrom((JsonReader) new JsonTextReader((TextReader) reader))).Properties().ToList<JProperty>().ForEach(
                (Action<JProperty>) (property => this.ParseSubElement(
                    property,
                    new Stack<string>(),
                    loader,
                    fileName
                ))
            );
    }

    private void ParseSubElement(
        JProperty property,
        Stack<string> textId,
        LocalizationLoader loader,
        string fileName
    )
    {
        var valueType = property.Value.Type;

        switch (valueType)
        {
            case JTokenType.Object:
                textId.Push(property.Name);
                ((JObject) property.Value).Properties().ToList<JProperty>().ForEach(
                    (Action<JProperty>) (subProperty => this.ParseSubElement(
                        subProperty,
                        textId,
                        loader,
                        fileName
                    ))
                );
                textId.Pop();
                break;

            case JTokenType.Array:
            case JTokenType.String:
                var value = property.Value.ToString();
                if (valueType == JTokenType.Array)
                {
                    value = property.Value.ToObject<string[]>().ToDelimitedString("");
                }

                loader.AddTranslation(
                    textId: this.LabelPathRootPrefix + string.Join(this.LabelPathSeparator, textId.Reverse<string>()) + this.LabelPathSuffix,
                    languageId: property.Name,
                    value: value,
                    source: fileName
                );
                break;
            default:
                throw new FormatException("Invalid format in Json language file for property [" + property.Name + "]");
        }
    }
}
