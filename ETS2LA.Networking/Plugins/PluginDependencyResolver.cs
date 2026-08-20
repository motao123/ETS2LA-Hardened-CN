namespace ETS2LA.Networking.Plugins;

public sealed record ResolvedNetworkPlugin(NetworkPlugin Plugin, NetworkPluginVersion Version);

public static class PluginDependencyResolver
{
    public static IReadOnlyList<ResolvedNetworkPlugin> Resolve(
        IEnumerable<NetworkPlugin> availablePlugins,
        IEnumerable<string> installedPluginIds,
        string targetPluginId,
        string appVersion,
        OperatingSystem operatingSystem)
    {
        PluginSecurityPaths.ValidatePluginId(targetPluginId);
        var installed = installedPluginIds.ToHashSet(StringComparer.Ordinal);
        var available = new Dictionary<string, NetworkPlugin>(StringComparer.Ordinal);
        foreach (var plugin in availablePlugins)
        {
            PluginSecurityPaths.ValidatePluginId(plugin.Id);
            if (!available.TryAdd(plugin.Id, plugin))
                throw new InvalidOperationException($"Plugin catalog contains duplicate ID '{plugin.Id}'.");
        }

        var state = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new List<ResolvedNetworkPlugin>();
        Visit(targetPluginId, isTarget: true);
        return result;

        void Visit(string pluginId, bool isTarget)
        {
            PluginSecurityPaths.ValidatePluginId(pluginId);
            if (!isTarget && installed.Contains(pluginId))
                return;
            if (state.TryGetValue(pluginId, out var currentState))
            {
                if (currentState == 1)
                    throw new InvalidOperationException($"Plugin dependency cycle detected at '{pluginId}'.");
                return;
            }
            if (!available.TryGetValue(pluginId, out var plugin))
                throw new InvalidOperationException($"Required plugin '{pluginId}' is missing from the plugin catalog.");

            var version = plugin.GetLatestCompatibleVersion(appVersion, operatingSystem)
                ?? throw new InvalidOperationException($"Plugin '{pluginId}' has no compatible version.");
            state[pluginId] = 1;
            foreach (var dependencyId in version.Dependencies ?? new List<string>())
                Visit(dependencyId, isTarget: false);
            state[pluginId] = 2;
            result.Add(new ResolvedNetworkPlugin(plugin, version));
        }
    }
}
