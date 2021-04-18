namespace ModelConverter.Utilities
{
    using System;

    /// <summary>
    /// Plugin entry class attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PluginAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginAttribute"/> class
        /// </summary>
        /// <param name="pluginName">Plugin name</param>
        /// <param name="description">Plugin description</param>
        public PluginAttribute(string pluginName, string description = "")
        {
            this.PluginName = pluginName;
            this.Description = description;
        }

        /// <summary>
        /// Gets or sets plugin description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets plugin name
        /// </summary>
        public string PluginName { get; set; }
    }
}