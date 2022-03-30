using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;

namespace WinTenDev.Zizi.Services.Externals;

public class DeepAiService
{
    private readonly CommonConfig _commonConfig;

    // private readonly DeepAI_API _deepAiApi;

    public DeepAiService()
    {

    }

    public DeepAiService(IOptionsSnapshot<CommonConfig> commonConfig)
    {
        _commonConfig = commonConfig.Value;
    }

    // public string NsfwDetector(string imagePath)
    // {
    // var resp = _deepAiApi.callStandardApi("nsfw-detector", new
    // {
    // image = imagePath
    // });

    // var json = _deepAiApi.objectAsJsonString(resp);

    // return json;
    // }

    public async Task<DeepAiResult> NsfwDetectCoreAsync(string imagePath)
    {
        var baseUrl = "https://api.deepai.org/api/nsfw-detector";
        var token = _commonConfig.DeepAiToken;

        var flurlResponse = await baseUrl
            .WithHeader("api-key", token)
            .PostMultipartAsync(
                content => {
                    content.AddFile("image", imagePath);
                    // content.AddString("image", imagePath);
                    // content.AddUrlEncoded("image", imagePath);
                }
            );

        var deepAiResult = await flurlResponse.GetJsonAsync<DeepAiResult>();
        Log.Debug("NSFW Result: {@Result}", deepAiResult);

        return deepAiResult;
    }
}
