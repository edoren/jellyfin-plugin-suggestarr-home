using System.Diagnostics;
using Jellyfin.Extensions;
using Jellyfin.Plugin.SuggestArrHome.Model;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SuggestArrHome;

public class ResultsHandler
{
    private readonly ICollectionManager m_collectionManager;
    private readonly IPlaylistManager m_playlistManager;
    private readonly IDtoService m_dtoService;
    private readonly IUserManager m_userManager;
    private readonly ILogger m_logger;
    private readonly ILibraryManager m_libraryManager;

    public ResultsHandler(ICollectionManager collectionManager, IPlaylistManager playlistManager,
            IUserManager userManager, IDtoService dtoService, ILogger<ResultsHandler> logger, ILibraryManager libraryManager)
    {
        m_collectionManager = collectionManager;
        m_playlistManager = playlistManager;
        m_dtoService = dtoService;
        m_userManager = userManager;
        m_logger = logger;
        m_libraryManager = libraryManager;
    }

    public QueryResult<BaseItemDto> GetRecommendationsResults(HomeScreenSectionPayload payload)
    {
        Stopwatch timer = new();
        timer.Start();
        
        m_logger.LogDebug($"{payload.AdditionalData} - Start: {timer.ElapsedMilliseconds}ms");
        DtoOptions dtoOptions = new()
        {
            Fields =
            [
                ItemFields.PrimaryImageAspectRatio,
                ItemFields.MediaSourceCount
            ],
            ImageTypes =
            [
                ImageType.Primary,
                ImageType.Backdrop,
                ImageType.Banner,
                ImageType.Thumb
            ],
            ImageTypeLimit = 1
        };

        User user = m_userManager.GetUserById(payload.UserId)!;
        m_logger.LogDebug($"{payload.AdditionalData} - User: {timer.ElapsedMilliseconds}ms");
        
        LibraryCache.CachedFoundItems.TryGetValue(user.Id, out List<BaseItem>? items);
        items ??= [];
        m_logger.LogDebug($"{payload.AdditionalData} - Children: {timer.ElapsedMilliseconds}ms");

        items.Shuffle();
        m_logger.LogDebug($"{payload.AdditionalData} - Shuffle: {timer.ElapsedMilliseconds}ms");

        items = items.Take(Math.Min(items.Count, 16)).ToList();
        m_logger.LogDebug($"{payload.AdditionalData} - ToList: {timer.ElapsedMilliseconds}ms");

        var results = new QueryResult<BaseItemDto>(m_dtoService.GetBaseItemDtos(items, dtoOptions, user));
        m_logger.LogDebug($"{payload.AdditionalData} - Results: {timer.ElapsedMilliseconds}ms");
        
        timer.Stop();

        return results;
    }
}