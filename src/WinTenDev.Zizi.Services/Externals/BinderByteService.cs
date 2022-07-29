using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;

namespace WinTenDev.Zizi.Services.Externals;

public class BinderByteService
{
    private readonly BinderByteConfig _binderByteConfig;
    private readonly ILogger<BinderByteService> _logger;
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    private readonly bool _isEnabled;
    private readonly string _apiUrl;
    private readonly string _apiToken;

    public BinderByteService(
        IOptionsSnapshot<BinderByteConfig> binderByteConfig,
        ILogger<BinderByteService> logger,
        CacheService cacheService,
        QueryService queryService
    )
    {
        _binderByteConfig = binderByteConfig.Value;
        _logger = logger;
        _cacheService = cacheService;
        _queryService = queryService;

        (_isEnabled, _apiUrl, _apiToken) = binderByteConfig.Value;
    }

    public async Task<List<BinderByteCourier>> GetSupportedCouriers()
    {
        var data = await _cacheService.GetOrSetAsync(
            "binder-byte-list-courier",
            async () => {
                var data = await GetSupportedCouriersCore();
                return data;
            }
        );

        return data;
    }

    public async Task<List<BinderByteCourier>> GetSupportedCouriersCore()
    {
        var res = await _apiUrl
            .AppendPathSegment("list_courier")
            .SetQueryParam("api_key", _apiToken)
            .AllowHttpStatus("400")
            .GetJsonAsync<List<BinderByteCourier>>();

        return res;
    }

    public BinderByteCekResi GetStoredResi(string awb)
    {
        var collection = _queryService
            .GetJsonCollection<BinderByteCekResi>()
            .AsQueryable()
            .FirstOrDefault(resi => resi.Data?.Summary?.Awb == awb);

        return collection;
    }

    public async Task<bool> CekResiStoreJson(BinderByteCekResi data)
    {
        var resi = data.Data?.Summary?.Awb;

        var insert = await _queryService
            .GetJsonCollection<BinderByteCekResi>()
            .ReplaceOneAsync(
                resi,
                data,
                true
            );

        return insert;
    }

    public async Task<string> CekResiBatchesAsync(string awb)
    {
        if (!_binderByteConfig.IsEnabled)
        {
            return "CekResi dimatikan oleh Administrator";
        }

        var couriers = await GetSupportedCouriers();

        var batchCheckResiTask = couriers
            .Select(courier => courier.Code)
            .Select(s => CekResiMergedAsync(s, awb));

        var results = await Task.WhenAll(batchCheckResiTask);

        var filtered = results.FirstOrDefault(s => !s.Contains("400"), results.FirstOrDefault());

        return filtered;
    }

    public async Task<string> CekResiMergedAsync(
        string courier,
        string awb
    )
    {
        var sb = new StringBuilder();

        var result = GetStoredResi(awb) ?? await CekResiRawAsync(courier, awb);

        if (result.Status != 200)
        {
            var message = result.Message;

            sb.AppendLine("<b>Kesalahan: </b>400")
                .Append("<b>Pesan: </b>");

            if (message.Contains("data not found", StringComparison.CurrentCultureIgnoreCase))
            {
                sb
                    .Append("Sepertinya no resi tidak ada");
            }
            else
            {
                sb.Append("Sesuatu telah terjadi silakan hubungi Administrator");
                _logger.LogError("BinderByte CekResi failed. {@V}", result);
            }

            return sb.ToTrimmedString();
        }

        var data = result.Data;
        var summary = data.Summary;
        var detail = data.Detail;
        var histories = data.History;

        sb.AppendLine("<b>📦 Ringkasan</b>")
            .Append("Kurir: ").AppendLine(summary.Courier)
            .Append("Status: ").AppendLine(summary.Status)
            .Append("Tanggal: ").AppendLine(summary.Date)
            .Append("Deskripsi: ").AppendLine(summary.Desc)
            .Append("Berat: ").AppendLine(summary.Weight)
            .AppendLine();

        sb.AppendLine("<b>📜 Detail</b>")
            .Append("Origin: ").AppendLine(detail.Origin)
            .Append("Tujuan: ").AppendLine(detail.Destination)
            .Append("Shipper: ").AppendLine(detail.Shipper)
            .Append("Penerima: ").AppendLine(detail.Receiver)
            .AppendLine();

        sb.AppendLine("<b>🕰 Riwayat</b>");

        histories
            .OrderBy(x => x.Date)
            .ForEach
            (
                (
                    history,
                    index
                ) => {
                    sb.Append(index + 1).Append(". ").AppendLine(history.Date.ToString())
                        .Append("└ ").AppendLine(history.Desc)
                        .AppendLine();
                }
            );

        var mergedResult = sb.ToTrimmedString();

        return mergedResult;
    }

    public async Task<BinderByteCekResi> CekResiRawAsync(
        string courier,
        string awb
    )
    {
        var result = await _cacheService.GetOrSetAsync(
            cacheKey: $"cek-resi-{courier}-{awb}",
            staleAfter: "1h",
            expireAfter: "1d",
            action: async () => {
                var response = await CekResiRawCoreAsync(courier, awb);

                var status = response.Data?.Summary?.Status;

                if (status?.Contains("Delivered", StringComparison.CurrentCultureIgnoreCase) ?? false)
                {
                    await CekResiStoreJson(response);
                }

                return response;
            }
        );

        return result;
    }

    public async Task<BinderByteCekResi> CekResiRawCoreAsync(
        string courier,
        string awb
    )
    {
        var res = await _apiUrl
            .AppendPathSegment("track")
            .SetQueryParam("api_key", _apiToken)
            .SetQueryParam("courier", courier)
            .SetQueryParam("awb", awb)
            .AllowHttpStatus("400")
            .GetJsonAsync<BinderByteCekResi>();

        return res;
    }
}