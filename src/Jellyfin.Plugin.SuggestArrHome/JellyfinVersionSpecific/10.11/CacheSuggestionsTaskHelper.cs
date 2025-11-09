using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SuggestArrHome.JellyfinVersionSpecific;

public static class CacheSuggestionsTaskHelper
{
    public static IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromMinutes(15).Ticks
            }
        ];
    }
}
