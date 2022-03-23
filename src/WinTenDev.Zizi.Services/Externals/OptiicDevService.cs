using System;
using System.Threading.Tasks;
using CacheTower;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Externals;

public class OptiicDevService
{
    private readonly ILogger<OptiicDevService> _logger;
    private readonly CacheStack _cacheStack;
    private readonly OptiicDevConfig _optiicDevConfig;

    public OptiicDevService(
        ILogger<OptiicDevService> logger,
        IOptionsSnapshot<OptiicDevConfig> optiicDevConfig,
        CacheStack cacheStack
    )
    {
        _logger = logger;
        _cacheStack = cacheStack;
        _optiicDevConfig = optiicDevConfig.Value;
    }

    public async Task<OptiicDevOcr> ScanImageText(string filePath)
    {
        _logger.LogInformation("Starting scan OCR. File: {FilePath}", filePath);

        var apiKeys = _optiicDevConfig.ApiKeys;
        if (apiKeys == null)
        {
            _logger.LogError("OptiicDev OCR API keys not found");

            return new OptiicDevOcr()
            {
                Text = "Sepertinya OCR belum siap. Silakan hubungi pengembang.",
            };
        }

        var randomApiKey = apiKeys.RandomElement();

        var json = await _cacheStack.GetOrSetAsync<OptiicDevOcr>(
            cacheKey: $"ocr-{filePath}",
            getter: async (_) => {
                var response = await new FlurlClient("https://api.optiic.dev")
                    .Request("process")
                    .PostMultipartAsync(
                        content => content
                            .AddString("apiKey", randomApiKey)
                            .AddFile("image", filePath)
                    );

                var json = await response.GetJsonAsync<OptiicDevOcr>();
                return json;
            },
            settings: new CacheSettings(TimeSpan.FromDays(30))
        );

        return json;
    }
}