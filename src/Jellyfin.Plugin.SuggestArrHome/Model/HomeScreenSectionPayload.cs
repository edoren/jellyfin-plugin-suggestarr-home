namespace Jellyfin.Plugin.SuggestArrHome.Model;

public class HomeScreenSectionPayload
{
    public Guid UserId { get; set; }
    public string? AdditionalData { get; set; }
}
