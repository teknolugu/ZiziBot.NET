using System.Threading.Tasks;
using FluentAssertions;
using WinTenDev.Zizi.Utils.IO;
using WinTenDev.Zizi.Utils.Text;
using Xunit;

namespace WinTenDev.Zizi.Tests.Units.Text
{
    public class JsonUtilTests
    {
        [Fact]
        public void ConvertJsonToYamlTest()
        {
            var helloWorld = new HelloWorld()
            {
                Hello = "World"
            };

            var json = helloWorld.ToJson();

            var yaml = json.JsonToYaml();
        }

        [Fact]
        public async Task WriteToFileTest()
        {
            var helloWorld = new HelloWorld()
            {
                Hello = "World"
            };

            var json = helloWorld.ToJson();

            var jsonFile = await json.WriteToFileAsync("write-test.json");
            var fileExist = jsonFile.IsFileExist();

            fileExist.Should().BeTrue();
        }
    }

    internal class HelloWorld
    {
        public string Hello { get; set; }
    }
}