namespace ModelConverter
{
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// Plugin list window interaction logic
    /// </summary>
    public partial class PluginListWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginListWindow"/> class
        /// </summary>
        public PluginListWindow()
        {
            this.InitializeComponent();

            // Load list of plugins into datagrid
            HashSet<string> pluginNames = new HashSet<string>();

            foreach (Utilities.PluginLoader.Plugin plugin in Utilities.PluginLoader.GetAllPlugins<Utilities.PluginLoader.Plugin>())
            {
                if (!pluginNames.Contains(plugin.Name))
                {
                    pluginNames.Add(plugin.Name);
                    this.pluginList.Items.Add(plugin);
                }
            }
        }
    }
}