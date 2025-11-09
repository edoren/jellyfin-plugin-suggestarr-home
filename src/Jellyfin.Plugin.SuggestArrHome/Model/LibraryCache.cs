using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Plugin.SuggestArrHome.Model;

public class LibraryCache
{
    public static Dictionary<Guid, List<Requests>> CachedRequests { get; set; } = [];
    public static Dictionary<Guid, List<BaseItem>> CachedFoundItems { get; set; } = [];
}
