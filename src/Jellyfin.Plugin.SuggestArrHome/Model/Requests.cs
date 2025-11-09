namespace Jellyfin.Plugin.SuggestArrHome.Model;

public class Requests(string tmdb_request_id, string user_id)
{
    public string TmdbRequestId { get; } = tmdb_request_id;
    public string UserId { get; } = user_id;
}
