using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MongoDB.Entities;
using MySqlConnector;
using RepoDb;
using Serilog;

namespace WinTenDev.Zizi.Services.Internals;

public class SpellService
{
    private readonly IMapper _mapper;
    private readonly QueryService _queryService;
    private readonly CacheService _cacheService;

    public SpellService(
        IMapper mapper,
        QueryService queryService,
        CacheService cacheService
    )
    {
        _mapper = mapper;
        _queryService = queryService;
        _cacheService = cacheService;
    }

    public MySqlConnection DbConnection => _queryService.CreateMysqlConnectionCore();

    public async Task<bool> SaveSpell(SpellDto data)
    {
        // if (await DbConnection.ExistsAsync<Spell>(
        // spell =>
        // spell.Typo == data.Typo
        // ))
        // {
        // return false;
        // }

        // var insert = await DbConnection.InsertAsync(data);
        // Log.Debug("Insert Spell Result: {Spell}", insert);

        var insert = await DB.Find<SpellEntity>()
            .Match(entity => entity.Typo == data.Typo)
            .ExecuteAnyAsync();

        if (insert) return false;

        var spell = _mapper.Map<SpellEntity>(data);

        await spell.InsertAsync();

        return true;
    }

    public async Task<List<SpellEntity>> GetSpellAll(bool evictBefore = false)
    {
        var spells = await _cacheService.GetOrSetAsync(
            cacheKey: "spelling",
            evictBefore: evictBefore,
            action: async () => {
                // var spells = await DbConnection.QueryAllAsync<Spell>();
                // return spells;

                var spells = await DB.Find<SpellEntity>().ExecuteAsync();

                return spells;
            }
        );

        return spells.ToList();
    }
}