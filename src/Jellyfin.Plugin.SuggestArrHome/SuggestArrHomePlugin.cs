using Jellyfin.Plugin.SuggestArrHome.Configuration;
using Jellyfin.Plugin.SuggestArrHome.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;

namespace Jellyfin.Plugin.SuggestArrHome;
public class SuggestArrHomePlugin : BasePlugin<PluginConfiguration>, IHasPluginConfiguration, IHasWebPages
{
    public override Guid Id => Guid.Parse("1fb74668-826a-4e24-ad7f-e9d8a20d3193");

    public override string Name => "SuggestArrHome";

    private readonly ILogger<SuggestArrHomePlugin> m_logger;
    private readonly ITaskManager m_taskManager;

    public static SuggestArrHomePlugin Instance { get; set; } = null!;


    public SuggestArrHomePlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<SuggestArrHomePlugin> logger, ITaskManager taskManager) : base(applicationPaths, xmlSerializer)
    {
        Instance = this;

        m_logger = logger;
        m_taskManager = taskManager;

        ConfigurationChanged += OnConfigurationChanged;
    }

    internal void OnConfigurationChanged(object? sender, BasePluginConfiguration baseConf)
    {
        if (baseConf is PluginConfiguration pluginConfiguration)
        {
            m_logger.LogInformation("Plugin configuration changed");

            RegisterHomeSection();

            m_taskManager.QueueScheduledTask<UpdateRequestsTask>();
        }
    }

    internal void RegisterHomeSection()
    {
        JObject jsonPayload = new() {
                { "id", "RecommendedForYou" },
                { "displayText", "Recommended For You" },
                { "limit", 1 },
                { "resultsAssembly", GetType().Assembly.FullName },
                { "resultsClass", typeof(ResultsHandler).FullName },
                { "resultsMethod", nameof(ResultsHandler.GetRecommendationsResults) }
            };

        Assembly? homeScreenSectionsAssembly =
            AssemblyLoadContext.All.SelectMany(x => x.Assemblies).FirstOrDefault(x =>
                x.FullName?.Contains(".HomeScreenSections") ?? false);

        if (homeScreenSectionsAssembly == null)
        {
            m_logger.LogError($"Couldn't find Home Screen Sections assembly when attempting to register section. Ensure you have `Home Screen Sections` installed on your server.");
            return;
        }

        Type? pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
        if (pluginInterfaceType == null)
        {
            m_logger.LogError($"Couldn't find PluginInterface type in Home Screen Sections plugin when attempting to register section. Ensure you have the latest version of `Home Screen Sections` installed on your server.");
            return;
        }

        var registerSectionMethod = pluginInterfaceType.GetMethod("RegisterSection");
        if (registerSectionMethod == null)
        {
            m_logger.LogError($"Could not find RegisterSection method");
            return;
        }
        var result = registerSectionMethod.Invoke(null, [jsonPayload]);
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.config.html", GetType().Namespace)
        };
    }
}
