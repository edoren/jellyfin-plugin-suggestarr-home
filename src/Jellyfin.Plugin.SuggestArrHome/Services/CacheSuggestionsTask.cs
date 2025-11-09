using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.SuggestArrHome.JellyfinVersionSpecific;
using Jellyfin.Plugin.SuggestArrHome.Model;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Jellyfin.Plugin.SuggestArrHome.Services;

public class CacheSuggestionsTask : IScheduledTask
{
    public string Name => "SuggestArrHome Cache Suggestions";

    public string Key => "Jellyfin.Plugin.SuggestArrHome.CacheSuggestions";
    
    public string Description => "Update cache of suggestions from stored requests";
    
    public string Category => "Plugins";
    
    private readonly IServerApplicationHost m_serverApplicationHost;
    private readonly IApplicationPaths m_applicationPaths;
    private readonly IServiceProvider m_serviceProvider;

    public CacheSuggestionsTask(IServerApplicationHost serverApplicationHost, IApplicationPaths applicationPaths, IServiceProvider serviceProvider)
    {
        m_serverApplicationHost = serverApplicationHost;
        m_applicationPaths = applicationPaths;
        m_serviceProvider = serviceProvider;
    }
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ILogger logger = m_serviceProvider.GetRequiredService<ILogger<CacheSuggestionsTask>>();
        
        logger.LogDebug("Starting cache process for recommendations to fasten data request");
        IUserManager userManager = m_serviceProvider.GetRequiredService<IUserManager>();
        ICollectionManager collectionManager = m_serviceProvider.GetRequiredService<ICollectionManager>();
        IPlaylistManager playlistManager = m_serviceProvider.GetRequiredService<IPlaylistManager>();
        ILibraryManager libraryManager = m_serviceProvider.GetRequiredService<ILibraryManager>();

        var queried = libraryManager.QueryItems(new InternalItemsQuery
        {
            Recursive = true,
            HasTmdbId = true,
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
        });

        LibraryCache.CachedFoundItems.Clear();

        foreach (User user in userManager.Users) {
            logger.LogDebug($"Caching data for user {user.Username} with Id {user.Id}");
            if (LibraryCache.CachedRequests.TryGetValue(user.Id, out var userRequests))
            {
                var requestTmdbIds = userRequests.Select(x => { return x.TmdbRequestId; }).ToList();

                List<BaseItem> foundItems = [];
                foreach (var item in queried.Items) {
                    if (item != null && !item.IsPlayed(user, null) && item.ProviderIds.TryGetValue("Tmdb", out var tmdbId) && requestTmdbIds.Contains(tmdbId)){
                        foundItems.Add(item);
                    }
                }

                if (foundItems.Count > 0)
                {
                    logger.LogDebug($"Found {foundItems.Count} recommendations for user {user.Username}");
                    LibraryCache.CachedFoundItems.Add(user.Id, foundItems);
                }
            }
            logger.LogDebug($"Caching data for user {user.Username} finished");
        }
        
        logger.LogDebug($"Caching of recommendations finished");

        SuggestArrHomePlugin.Instance.RegisterHomeSection();

        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => CacheSuggestionsTaskHelper.GetDefaultTriggers();
}