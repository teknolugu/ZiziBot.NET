using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WinTenDev.Zizi.Services.Externals;

public class OptiicDevService
{
    private readonly ILogger<OptiicDevService> _logger;
    private readonly CacheService _cacheService;
    private readonly OptiicDevConfig _optiicDevConfig;

    public OptiicDevService(
        ILogger<OptiicDevService> logger,
        IOptionsSnapshot<OptiicDevConfig> optiicDevConfig,
        CacheService cacheService
    )
    {
        _logger = logger;
        _cacheService = cacheService;
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

        var json = await _cacheService.GetOrSetAsync(
            cacheKey: $"ocr-{filePath}",
            expireAfter: "30d",
            staleAfter: "1d",
            action: async () => {
                var response = await new FlurlClient("https://api.optiic.dev")
                    .Request("process")
                    .PostMultipartAsync(
                        content => content
                            .AddString("apiKey", randomApiKey)
                            .AddFile("image", filePath)
                    );

                var json = await response.GetJsonAsync<OptiicDevOcr>();
                return json;
            }
        );

        return json;
    }
}