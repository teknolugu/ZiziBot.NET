using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheTower;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Options;
using MoreLinq;
using Serilog;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Models.Types.BinderByte;
using WinTenDev.Zizi.Models.Types.Pigoora;
using WinTenDev.Zizi.Services.Internals;
using WinTenDev.Zizi.Utils;

namespace WinTenDev.Zizi.Services.Externals;

public class CekResiService
{
    private readonly BinderByteConfig _binderByteConfig;
    private readonly CacheStack _cacheStack;
    private readonly QueryService _queryService;
    private const string PigooraCekResiUrl = "https://api.cekresi.pigoora.com/cekResi";
    private const string CommonUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0";

    public CekResiService(
        IOptionsSnapshot<BinderByteConfig> binderByteConfig,
        CacheStack cacheStack,
        QueryService queryService
    )
    {
        _binderByteConfig = binderByteConfig.Value;
        _cacheStack = cacheStack;
        _queryService = queryService;
    }

    private static List<string> GetCouriers()
    {
        return new List<string> { "sicepat", "jne", "jnt", "ninja", "pos" };
    }

    public async Task<PigooraCekResi> CekResi(string resi)
    {
        return new PigooraCekResi();
    }

    public async Task<string> CekResiCore(string resi)
    {
        Log.Information("Checking Resi {0}", resi);
        var couriers = GetCouriers();

        Log.Debug("Resi Check finish");
        return "filtered";
    }

    public async Task PigooraCekResi(
        string courier,
        string awb
    )
    {
        var pigooraConfig = "_appConfig.PigooraConfig";
        var cekResiUrl = "pigooraConfig.CekResiUrl";
        var cekResiToken = "pigooraConfig.CekResiToken";

        var cekResi = await PigooraCekResiUrl
            .SetQueryParam("key", cekResiToken)
            .SetQueryParam("resi", awb)
            .SetQueryParam("kurir", courier)
            .SetQueryParams()
            .WithHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:85.0) Gecko/20100101 Firefox/85.0")
            .WithHeader("Host", "api.cekresi.pigoora.com")
            .WithHeader("Origin", "https://cekresi.pigoora.com")
            .WithHeader("Referer", "https://cekresi.pigoora.com/")
            .GetJsonAsync<PigooraCekResi>();
    }

    #region BinderByte CekResi

    public BinderByteCekResi BinderByteGetStoredResi(string awb)
    {
        var collection = _queryService
            .GetJsonCollection<BinderByteCekResi>()
            .AsQueryable()
            .FirstOrDefault(resi => resi.Data?.Summary?.Awb == awb);

        return collection;
    }

    public async Task<bool> BinderByteCekResiStoreJson(BinderByteCekResi data)
    {
        var resi = data.Data?.Summary?.Awb;

        var insert = await _queryService
            .GetJsonCollection<BinderByteCekResi>()
            .ReplaceOneAsync(resi, data, true);

        return insert;
    }

    public async Task<string> BinderByteCekResiBatchesAsync(string awb)
    {
        if (!_binderByteConfig.IsEnabled)
        {
            return "CekResi dimatikan oleh Administrator";
        }

        var batchCheckResiTask = GetCouriers()
            .Select(s => BinderByteCekResiMergedAsync(s, awb));

        var results = await Task.WhenAll(batchCheckResiTask);

        var filtered = results.FirstOrDefault(s => !s.Contains("400"), results.FirstOrDefault());

        return filtered;
    }

    public async Task<string> BinderByteCekResiMergedAsync(
        string courier,
        string awb
    )
    {
        var sb = new StringBuilder();

        var result = BinderByteGetStoredResi(awb) ?? await BinderByteCekResiRawAsync(courier, awb);

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
                Log.Error("BinderByte CekResi failed. {@V}", result);
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

    public async Task<BinderByteCekResi> BinderByteCekResiRawAsync(
        string courier,
        string awb
    )
    {
        var result = await _cacheStack.GetOrSetAsync<BinderByteCekResi>
        (
            $"cek-resi-{courier}-{awb}", async (_) => {
                var response = await BinderByteCekResiRawCoreAsync(courier, awb);

                var status = response.Data?.Summary?.Status;

                if (status?.Contains("Delivered", StringComparison.CurrentCultureIgnoreCase) ?? false)
                {
                    await BinderByteCekResiStoreJson(response);
                }

                return response;
            },
            new CacheSettings(TimeSpan.FromDays(1), TimeSpan.FromDays(1))
        );

        return result;
    }

    public async Task<BinderByteCekResi> BinderByteCekResiRawCoreAsync(
        string courier,
        string awb
    )
    {
        var res = await _binderByteConfig.ApiUrl
            .AppendPathSegment("track")
            .SetQueryParam("api_key", _binderByteConfig.ApiToken)
            .SetQueryParam("courier", courier)
            .SetQueryParam("awb", awb)
            .AllowHttpStatus("400")
            .GetJsonAsync<BinderByteCekResi>();

        return res;
    }

    #endregion
}