using Jellyfin.Plugin.SuggestArrHome.Configuration;
using Jellyfin.Plugin.SuggestArrHome.JellyfinVersionSpecific;
using Jellyfin.Plugin.SuggestArrHome.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Jellyfin.Plugin.SuggestArrHome.Services;

public class UpdateRequestsTask : IScheduledTask
{
    public string Name => "SuggestArrHome Update Requests";

    public string Key => "Jellyfin.Plugin.SuggestArrHome.UpdateRequests";

    public string Description => "Update requests from SuggestArr";

    public string Category => "Plugins";

    private readonly IServerApplicationHost m_serverApplicationHost;
    private readonly IApplicationPaths m_applicationPaths;
    private readonly IServiceProvider m_serviceProvider;

    public UpdateRequestsTask(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths, IServiceProvider serviceProvider)
    {
        m_serverApplicationHost = serverApplicationHost;
        m_applicationPaths = applicationPaths;
        m_serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ILogger logger = m_serviceProvider.GetRequiredService<ILogger<UpdateRequestsTask>>();

        PluginConfiguration? config = SuggestArrHomePlugin.Instance?.Configuration;
        if (config == null || !config.IsDbConfigValid())
        {
            logger.LogError("Could not retrieve configuration");
            return;
        }

        var connectionString = $"Host={config.DbHost};Port={config.DbPort};Username={config.DbUser};Password={config.DbPassword};Database={config.DbName}";

        logger.LogInformation("Updating the SuggestArr requests");
        IUserManager userManager = m_serviceProvider.GetRequiredService<IUserManager>();
        ICollectionManager collectionManager = m_serviceProvider.GetRequiredService<ICollectionManager>();
        IPlaylistManager playlistManager = m_serviceProvider.GetRequiredService<IPlaylistManager>();
        ILibraryManager libraryManager = m_serviceProvider.GetRequiredService<ILibraryManager>();
        ITaskManager taskManager = m_serviceProvider.GetRequiredService<ITaskManager>();

        await using var conn = new NpgsqlConnection(connectionString);
        try
        {
            await conn.OpenAsync(cancellationToken);
        }
        catch (NpgsqlException e)
        {
            logger.LogError($"Error connecting to the PostgreSQL database, check the configuration in the plugin settings: {e.Message}");
            return;
        }

        var query = "SELECT tmdb_request_id, user_id FROM requests WHERE requested_by = 'SuggestArr' AND user_id IS NOT null";
        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        LibraryCache.CachedRequests.Clear();

        logger.LogDebug($"Caching data from SuggestArr DB");
        var requestCount = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            var request = new Requests(
                reader.GetString(0),
                reader.GetString(1)
            );
            var userId = Guid.Parse(request.UserId);
            if (LibraryCache.CachedRequests.TryGetValue(userId, out var existingRequests))
            {
                existingRequests.Add(request);
            }
            else
            {
                LibraryCache.CachedRequests.Add(userId, [request]);
            }
            requestCount++;
        }
        logger.LogDebug($"Updating finished, found {requestCount} requests on the database");

        taskManager.QueueScheduledTask<CacheSuggestionsTask>();
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => UpdateRequestsTaskHelper.GetDefaultTriggers();
}
