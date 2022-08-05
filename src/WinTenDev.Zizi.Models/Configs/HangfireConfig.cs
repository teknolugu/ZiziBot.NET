using WinTenDev.Zizi.Models.Enums;

namespace WinTenDev.Zizi.Models.Configs;

public class HangfireConfig
{
    public string BaseUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string[] Queues { get; set; } = new[] { "default" };
    public int WorkerMultiplier { get; set; } = 1;

    public HangfireDataStore DataStore { get; set; }
    public string SqliteConnection { get; set; }
    public string LiteDbConnection { get; set; }
    public string RedisConnection { get; set; }
    public string MysqlConnection { get; set; }
    public string MongoDbConnection { get; set; }
}