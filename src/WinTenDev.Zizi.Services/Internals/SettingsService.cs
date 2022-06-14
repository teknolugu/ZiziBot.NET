using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Serilog;
using SqlKata.Execution;
using WinTenDev.Zizi.Models.Tables;
using WinTenDev.Zizi.Models.Types;
using WinTenDev.Zizi.Utils;
using WinTenDev.Zizi.Utils.Telegram;
using WinTenDev.Zizi.Utils.Text;

namespace WinTenDev.Zizi.Services.Internals;

public class SettingsService
{
    private const string BaseTable = "group_settings";
    private const string CacheKey = "setting";
    private readonly CacheService _cacheService;
    private readonly QueryService _queryService;

    public SettingsService(
        CacheService cacheService,
        QueryService queryService
    )
    {
        _cacheService = cacheService;
        _queryService = queryService;
    }

    public async Task<bool> IsSettingExist(long chatId)
    {
        var data = await GetSettingsByGroupCore(chatId);
        var isExist = data != null;

        Log.Debug(
            "Group setting for ChatID '{ChatId}' IsExist? {IsExist}",
            chatId,
            isExist
        );
        return isExist;
    }

    public string GetCacheKey(long chatId)
    {
        return CacheKey + "-" + chatId.ReduceChatId();
    }

    public async Task<ChatSetting> GetSettingsByGroupCore(long chatId)
    {
        var where = new Dictionary<string, object>
        {
            { "chat_id", chatId }
        };

        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .Where(where)
            .FirstOrDefaultAsync<ChatSetting>();

        return data;
    }

    public async Task<ChatSetting> GetSettingsByGroup(
        long chatId,
        bool evictBefore = false
    )
    {
        Log.Information("Get settings chat {ChatId}", chatId);
        var cacheKey = GetCacheKey(chatId);

        var settings = await _cacheService.GetOrSetAsync(
            cacheKey: cacheKey,
            evictBefore: evictBefore,
            action: async () => {
                var data = await _queryService
                    .CreateMySqlFactory()
                    .FromTable(BaseTable)
                    .Where("chat_id", chatId)
                    .FirstOrDefaultAsync<ChatSetting>();

                return data ?? new ChatSetting();
            }
        );

        return settings;
    }

    public Task<IEnumerable<ChatSetting>> GetAllSettings()
    {
        var chatGroups = _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            // .WhereNot("chat_type", "Private")
            // .WhereNot("chat_type", "0")
            .GetAsync<ChatSetting>();

        return chatGroups;
    }

    public async Task UpdateCacheAsync(long chatId)
    {
        Log.Debug("Updating cache for {ChatId}", chatId);
        var cacheKey = GetCacheKey(chatId);

        var data = await GetSettingsByGroupCore(chatId);

        if (data == null)
        {
            Log.Warning("This may first time chat for this ChatId: {ChatId}", chatId);
            return;
        }

        await _cacheService.SetAsync(cacheKey, data);
    }

    public async Task<int> DeleteSettings(long chatId)
    {
        Log.Debug("Starting delete ChatSetting for ChatID: '{ChatId}'", chatId);

        var deleteSetting = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .Where("chat_id", chatId)
            .DeleteAsync();

        Log.Debug(
            "Delete ChatSetting by ChatID: '{ChatId}' result => {ChatGroups}",
            chatId,
            deleteSetting
        );
        return deleteSetting;
    }

    public async Task<int> PurgeSettings(int daysOffset)
    {
        var purgeSettings = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .WhereRaw($"datediff(now(), updated_at) > {daysOffset}")
            .DeleteAsync();
        Log.Information("About purge settings, total '{Purge}' rows of chats settings is removed", purgeSettings);

        return purgeSettings;
    }

    public async Task<List<CallBackButton>> GetSettingButtonByGroup(
        long chatId,
        bool appendChatId = false
    )
    {
        var selectColumns = new[]
        {
            "id",
            "enable_afk_status",
            "enable_anti_malfiles",
            "enable_fed_cas_ban",
            "enable_fed_es2_ban",
            "enable_fed_spamwatch",
            "enable_flood_check",
            "enable_fire_check",
            "enable_find_tags",
            "enable_force_subscription",
            "enable_human_verification",
            "enable_check_profile_photo",
            "enable_reply_notification",
            "enable_privacy_mode",
            "enable_spell_check",
            "enable_warn_username",
            "enable_welcome_message",
            // "enable_word_filter_group",
            "enable_word_filter_global",
            "enable_zizi_mata"
        };

        Log.Debug(
            "Append Settings button with Chat ID '{ChatId}'? {AppendChatId}",
            chatId,
            appendChatId
        );

        var data = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .Select(selectColumns)
            .Where("chat_id", chatId)
            .GetAsync();

        using var dataTable = data.ToJson().MapObject<DataTable>();

        var rowId = dataTable.Rows[0]["id"].ToString();
        Log.Debug("RowId: {RowId}", rowId);

        var transposedTable = dataTable.TransposedTable();

        var listBtn = new List<CallBackButton>();

        foreach (DataRow row in transposedTable.Rows)
        {
            var textOrig = row["id"].ToString();
            var value = row[rowId ?? string.Empty].ToString();

            Log.Verbose(
                "Orig: {TextOrig}, Value: {Value}",
                textOrig,
                value
            );

            var boolVal = value.ToBool();

            var forCallbackData = textOrig;
            var forCaptionText = textOrig;

            if (!boolVal) forCallbackData = textOrig?.Replace("enable", "disable");

            if (boolVal)
                forCaptionText = textOrig?.Replace("enable", "✅");
            else
                forCaptionText = textOrig?.Replace("enable", "🚫");

            var btnText = forCaptionText?.Replace("enable_", "")
                .Replace("_", " ");

            var tail = appendChatId ? $" {chatId}" : "";

            listBtn.Add
            (
                new CallBackButton
                {
                    Text = btnText.ToTitleCase(),
                    Data = $"setting {forCallbackData}" + tail
                }
            );
        }

        listBtn.Add(new CallBackButton { Text = "❌ Tutup", Data = "delete-message current-message" });

        Log.Verbose("ListBtn: {Btn}", listBtn.ToJson(true));

        return listBtn;
    }

    public async Task<int> SaveSettingsAsync(Dictionary<string, object> data)
    {
        var chatId = data["chat_id"].ToInt64();
        var where = new Dictionary<string, object> { { "chat_id", chatId } };

        Log.Debug("Checking Chat Settings for {ChatId}", chatId);

        var isExist = await IsSettingExist(chatId);

        int insert;

        if (!isExist)
        {
            Log.Information("Inserting new Chat Settings for {ChatId}", chatId);

            insert = await _queryService
                .CreateMySqlFactory()
                .FromTable(BaseTable)
                .InsertAsync(data);
        }
        else
        {
            Log.Information("Updating Chat Settings for {ChatId}", chatId);

            insert = await _queryService
                .CreateMySqlFactory()
                .FromTable(BaseTable)
                .Where(where)
                .UpdateAsync(data);
        }

        UpdateCacheAsync(chatId).InBackground();

        return insert;
    }

    public async Task<int> UpdateCell(
        long chatId,
        string key,
        object value
    )
    {
        Log.Debug(
            "Updating Chat Settings '{ChatId}'. Field '{Key}' with value '{Value}'",
            chatId,
            key,
            value
        );
        var where = new Dictionary<string, object> { { "chat_id", chatId } };
        var data = new Dictionary<string, object> { { key, value } };

        var save = await _queryService
            .CreateMySqlFactory()
            .FromTable(BaseTable)
            .Where(where)
            .UpdateAsync(data);

        await UpdateCacheAsync(chatId);

        return save;
    }
}
