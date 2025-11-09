using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SuggestArrHome.JellyfinVersionSpecific;

public static class UpdateRequestsTaskHelper
{
    public static IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.DailyTrigger,
                TimeOfDayTicks = TimeSpan.FromMinutes(15).Ticks
            },
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.StartupTrigger,
            }
        ];
    }
}
