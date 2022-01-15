using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using WinTenDev.Zizi.Services.Internals;

namespace WinTenDev.Zizi.Services.Telegram;

public class UserProfilePhotoService
{
    private readonly ILogger<UserProfilePhotoService> _logger;
    private readonly CacheService _cacheService;
    private readonly SettingsService _settingsService;
    private readonly TelegramBotClient _botClient;

    public UserProfilePhotoService(
        ILogger<UserProfilePhotoService> logger,
        CacheService cacheService,
        SettingsService settingsService,
        TelegramBotClient botClient
    )
    {
        _logger = logger;
        _cacheService = cacheService;
        _settingsService = settingsService;
        _botClient = botClient;
    }

    private string GetCacheKey(long userId)
    {
        var cacheKey = "user-profile-photos_" + userId;
        return cacheKey;
    }

    public async Task<UserProfilePhotos> GetUserProfilePhotosAsync(long userId)
    {
        var userProfilePhotos = await _cacheService.GetOrSetAsync(GetCacheKey(userId), async () => {
            var userProfilePhotos = await _botClient.GetUserProfilePhotosAsync(userId);
            return userProfilePhotos;
        });

        return userProfilePhotos;
    }

    public async Task<UserProfilePhotos> GetUserProfilePhotosCoreAsync(long userId)
    {
        var userProfilePhotos = await _botClient.GetUserProfilePhotosAsync(userId);
        return userProfilePhotos;
    }

    public async Task<UserProfilePhotos> ResetUserProfilePhotoCacheAsync(long userId)
    {
        var userProfilePhotos = await _botClient.GetUserProfilePhotosAsync(userId);
        await _cacheService.SetAsync(GetCacheKey(userId), userProfilePhotos);

        return userProfilePhotos;
    }

    public async Task<bool> HasUserProfilePhotosAsync(long userId)
    {
        var chatPhoto = await GetUserProfilePhotosAsync(userId);
        var hasPhoto = chatPhoto.TotalCount > 0;
        Log.Debug("UserId {UserId} has Profile photo? {HasPhoto}", userId, hasPhoto);

        return hasPhoto;
    }

    public async Task<bool> CheckUserProfilePhoto(
        long chatId,
        long userId
    )
    {
        var chatSettings = await _settingsService.GetSettingsByGroup(chatId);
        if (!chatSettings.EnableCheckProfilePhoto)
        {
            _logger.LogInformation("User Profil photo check is disabled on ChatID '{ChatId}'", chatId);
            return true;
        }

        var hasPhoto = await HasUserProfilePhotosAsync(userId);
        if (hasPhoto)
        {
            return true;
        }

        return false;
    }

    public async Task EvictCacheAsync(long userId)
    {
        var cacheKey = GetCacheKey(userId);
        await _cacheService.EvictAsync(cacheKey);
    }
}