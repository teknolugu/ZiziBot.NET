using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Entities;

namespace WinTenDev.Zizi.Utils.Providers;

public static class MongoDBEntitiesUtil
{
    public static async Task<T> ExSaveAsync<T>(
        this T entity,
        Expression<Func<T, bool>> expression,
        CancellationToken cancellation = default
    ) where T : IEntity, IModifiedOn, ICreatedOn
    {
        await DB.Find<T>()
            .Match(expression)
            .ExecuteAnyAsync(cancellation)
            .ContinueWith(async task => {
                if (task.Result)
                {
                    await DB.Update<T>()
                        .Match(expression)
                        .ModifyExcept(x => new { x.ID, x.CreatedOn }, entity)
                        .ExecuteAsync(cancellation);
                }
                else
                {
                    await entity.InsertAsync(cancellation: cancellation);
                }
            }, cancellation);

        return entity;
    }
}