using System.Xml.Linq;

namespace WinTenDev.Zizi.Utils.Text;

public static class XmlUtil
{
    public static XElement GetOrCreateElement(
        this XContainer container,
        string name
    )
    {
        var element = container.Element(name);
        if (element != null) return element;
        element = new XElement(name);
        container.Add(element);
        return element;
    }
}