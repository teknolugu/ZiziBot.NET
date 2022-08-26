using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Entities;
using MySqlConnector;

namespace WinTenDev.Zizi.Services.Internals
{
    public class ChatAdminService
    {
        private readonly QueryService _queryService;

        public ChatAdminService(QueryService queryService)
        {
            _queryService = queryService;
        }

        public MySqlConnection CreateConnection()
        {
            return _queryService.CreateMysqlConnectionCore();
        }

        public async Task<long> SaveAll(IEnumerable<GroupAdmin> entities)
        {
            // await using var connection = CreateConnection();
            //
            // var chatAdmins = await connection.QueryAsync<ChatAdmin>(
            //     admin =>
            //         admin.ChatId == entities.First().ChatId
            // );
            //
            // var deletedRows = await connection.DeleteAllAsync(chatAdmins);
            // var insertRows = await connection.InsertAllAsync(entities);

            var deleted = await DB.DeleteAsync<GroupAdmin>(admin => admin.ChatId == entities.First().ChatId);
            var insert = await entities.InsertAsync();

            var deletedRows = deleted.DeletedCount;
            var insertRows = insert.InsertedCount;

            var diffRows = insertRows - deletedRows;

            return diffRows;
        }
    }
}