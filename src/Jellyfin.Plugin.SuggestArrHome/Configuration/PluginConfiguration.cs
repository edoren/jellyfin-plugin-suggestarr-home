using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.SuggestArrHome.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public enum DbTypeOptions
    {
        PostgreSQL,
        MySQL
    }

    public PluginConfiguration()
    {
        DbType = DbTypeOptions.PostgreSQL;
        DbHost = "127.0.0.1";
        DbPort = 5432;
        DbUser = "postgres";
        DbPassword = "postgres";
        DbName = "postgres";
    }

    public bool IsDbConfigValid()
    {
        return !string.IsNullOrEmpty(DbHost) && DbPort > 0 && !string.IsNullOrEmpty(DbUser) && !string.IsNullOrEmpty(DbPassword) && !string.IsNullOrEmpty(DbName);
    }

    public DbTypeOptions DbType { get; set; }
    public string DbHost { get; set; }
    public int DbPort { get; set; }
    public string DbUser { get; set; }
    public string DbPassword { get; set; }
    public string DbName { get; set; }
}
