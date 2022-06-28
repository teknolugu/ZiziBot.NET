using System;
using System.Threading;
using CacheTower;
using CacheTower.Extensions;
using CacheTower.Providers.FileSystem;
using CacheTower.Providers.Memory;
using CacheTower.Providers.Redis;
using CacheTower.Serializers.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx.Synchronous;
using StackExchange.Redis;
using WinTenDev.Zizi.Models.Configs;
using WinTenDev.Zizi.Utils.IO;

namespace WinTenDev.Zizi.Utils.Extensions;

public static class CacheTowerServiceExtension
{
	public static IServiceCollection AddCacheTower(this IServiceCollection services)
	{
		var cacheTowerPath = "Storage/Cache-Tower/".EnsureDirectory();
		var serviceProvider = services.BuildServiceProvider();
		var cacheConfig = serviceProvider.GetRequiredService<IOptions<CacheConfig>>().Value;
		var lastError = ErrorUtil.ParseErrorTextAsync().Result;
		var shouldInvalidate = lastError.FullText.Contains("CacheTower");

		if (cacheConfig.InvalidateOnStart || shouldInvalidate)
		{
			cacheTowerPath.DeleteDirectory().EnsureDirectory();
		}

		if (shouldInvalidate)
			"No Error".SaveErrorToText().WaitAndUnwrapException();

		services.AddCacheStack(
			builder => {
				var jsonSerializerSettings = new JsonSerializerSettings()
				{
					Formatting = Formatting.Indented
				};

				if (cacheConfig.EnableInMemoryCache)
					builder.AddMemoryCacheLayer();

				if (cacheConfig.EnableJsonCache)
					builder.AddFileCacheLayer(
						new FileCacheLayerOptions(
							directoryPath: cacheTowerPath,
							serializer: new NewtonsoftJsonCacheSerializer(jsonSerializerSettings),
							manifestSaveInterval: TimeSpan.FromSeconds(5)
						)
					);

				if (cacheConfig.EnableRedisCache)
					builder.AddRedisCacheLayer(
						connection: ConnectionMultiplexer.Connect(cacheConfig.RedisConnection),
						options: new RedisCacheLayerOptions(new NewtonsoftJsonCacheSerializer(jsonSerializerSettings))
					);
			}
		);

		return services;
	}
}
