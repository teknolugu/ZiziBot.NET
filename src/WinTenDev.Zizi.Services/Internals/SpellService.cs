using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using RepoDb;
using Serilog;
using WinTenDev.Zizi.Models.Tables;

namespace WinTenDev.Zizi.Services.Internals;

public class SpellService
{
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public SpellService(
        QueryService queryService,
        CacheService cacheService
    )
    {
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public MySqlConnection DbConnection =>
        _queryService.CreateMysqlConnectionCore();

    public async Task<bool> SaveSpell(Spell data)
    {
        if (await DbConnection.ExistsAsync<Spell>(
                spell =>
                    spell.Typo == data.Typo
            ))
        {
            return false;
        }

        var insert = await DbConnection.InsertAsync(data);
        Log.Debug("Insert Spell Result: {Spell}", insert);

        return true;
    }

    public async Task<List<Spell>> GetSpellAll()
    {
        var spells = await _cacheService.GetOrSetAsync(
            cacheKey: "spelling",
            action: () => {
                var spells = DbConnection.QueryAllAsync<Spell>();
                return spells;
            }
        );

        return spells.ToList();
    }
}
