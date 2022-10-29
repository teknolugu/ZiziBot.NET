using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MongoDB.Entities;
using WinTenDev.Zizi.Models.Entities.MongoDb.Internal;

namespace WinTenDev.Zizi.DbMigrations.MongoDBEntities;

[UsedImplicitly]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase")]
public class _001_Spell_Rename_FromId : IMigration
{
    public async Task UpgradeAsync()
    {
        await DB.Update<SpellEntity>()
            .Match(_ => true)
            .Modify(b => b.Rename("FromId", "UserId"))
            .ExecuteAsync();
    }
}