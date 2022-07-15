using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using MoreLinq;
using Serilog;
using Telegram.Bot.Types;

namespace WinTenDev.Zizi.Services.Externals;

public class AnimalsService
{
    private const string CatSourceAwsRandomCat = "https://aws.random.cat/meow";
    private const string CatSourceTheCatApi = "https://api.thecatapi.com/v1/images/search";

    public async Task<List<IAlbumInputMedia>> GetRandomCatsCatApi(int catNumber)
    {
        var listAlbum = new List<IAlbumInputMedia>();

        var listCats = await CatSourceTheCatApi
            .SetQueryParam("limit", catNumber)
            .GetJsonAsync<List<TheCatApiCat>>();

        listCats.ForEach
        (
            (
                cat,
                index
            ) => {
                var urlFile = cat.Url.AbsoluteUri;
                var fileName = Path.GetFileName(urlFile);

                Log.Debug("Adding Kochenk. Url: {Url}", urlFile);

                listAlbum.Add
                (
                    new InputMediaPhoto
                    (
                        new InputMedia(urlFile)
                        {
                            FileName = fileName
                        }
                    )
                    {
                        Caption = $"Kochenk {index}"
                    }
                );

            }
        );

        return listAlbum;

    }

    public async Task<List<IAlbumInputMedia>> GetRandomCatsAwsCat(int catNumber)
    {
        var listAlbum = new List<IAlbumInputMedia>();

        for (var i = 1; i <= catNumber; i++)
        {
            Log.Information(
                "Loading cat {I} of {CatNum} from {CatSource}",
                i,
                catNumber,
                CatSourceAwsRandomCat
            );

            var url = await CatSourceAwsRandomCat.GetJsonAsync<AwsRandomCatMeow>();
            var urlFile = url.File.AbsoluteUri;

            Log.Debug("Adding kochenk {UrlFile}", urlFile);

            var fileName = Path.GetFileName(urlFile);

            listAlbum.Add
            (
                new InputMediaPhoto
                (
                    new InputMedia(urlFile)
                    {
                        FileName = fileName
                    }
                )
                {
                    Caption = $"Kochenk {i}"
                }
            );
        }

        return listAlbum;
    }
}