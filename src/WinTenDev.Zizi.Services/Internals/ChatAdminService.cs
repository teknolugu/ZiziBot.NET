using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using RepoDb;
using WinTenDev.Zizi.Models.Tables;

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

        public async Task<int> SaveAll(IEnumerable<ChatAdmin> entities)
        {
            await using var connection = CreateConnection();

            var chatAdmins = await connection.QueryAsync<ChatAdmin>(
                admin =>
                    admin.ChatId == entities.First().ChatId
            );

            var deletedRows = await connection.DeleteAllAsync(chatAdmins);
            var insertRows = await connection.InsertAllAsync(entities);
            var diffRows = insertRows - deletedRows;

            return diffRows;
        }
    }
}
