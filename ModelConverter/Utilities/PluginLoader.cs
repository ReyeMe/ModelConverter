namespace ModelConverter.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Plugin loader
    /// </summary>
    public static class PluginLoader
    {
        /// <summary>
        /// Plugin types
        /// </summary>
        private static readonly List<Type> IoTypes = new List<Type> { typeof(IExport), typeof(IImport) };

        /// <summary>
        /// List of all loaded plugins
        /// </summary>
        private static readonly List<Plugin> Plugins = new List<Plugin>();

        /// <summary>
        /// Path to the plugins folder
        /// </summary>
        private static readonly string PluginsFolder = Path.Combine(Path.GetDirectoryName(typeof(PluginLoader).Assembly.Location), "plugins");

        /// <summary>
        /// Get all loaded plugins of specified type
        /// </summary>
        /// <typeparam name="PluginType">Plugin type to fetch</typeparam>
        /// <returns>List of plugins</returns>
        public static List<PluginType> GetAllPlugins<PluginType>() where PluginType : Plugin
        {
            return PluginLoader.Plugins.OfType<PluginType>().ToList();
        }

        /// <summary>
        /// Load plugins
        /// </summary>
        internal static void Load()
        {
            // Read assemblies in plugins folder
            foreach (string assemblyPath in Directory.GetFiles(PluginLoader.PluginsFolder, "*.dll", SearchOption.TopDirectoryOnly))
            {
                Assembly assembly = Assembly.LoadFile(assemblyPath);
                IEnumerable<Type> foundTypes = assembly.GetTypes().Where(type => PluginLoader.IoTypes.Any(io => type != io && io.IsAssignableFrom(type)));

                foreach (Type io in foundTypes)
                {
                    PluginAttribute attribute = io.GetCustomAttributes(typeof(PluginAttribute), false).FirstOrDefault() as PluginAttribute;

                    if (attribute != null)
                    {
                        try
                        {
                            PluginLoader.Plugins.Add(
                                Plugin.GetPluginFromType(
                                    PluginLoader.IoTypes.FindIndex(type => type.IsAssignableFrom(io)),
                                    assemblyPath,
                                    io,
                                    attribute));
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Import plugin
        /// </summary>
        public sealed class ExportPlugin : Plugin
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ExportPlugin"/> class
            /// </summary>
            /// <param name="file">Path to the DLL file</param>
            /// <param name="entryPoint">Plugin entry point</param>
            /// <param name="attribute">Plugin entry class attribute data</param>
            internal ExportPlugin(string file, Type entryPoint, PluginAttribute attribute) : base(file, entryPoint, attribute)
            {
                List<ExportExtensionAttribute> attributes = entryPoint.GetCustomAttributes(typeof(ExportExtensionAttribute), false).OfType<ExportExtensionAttribute>().ToList();

                if (!attributes.Any())
                {
                    throw new Exception("'ExportExtension' attribute is missing!");
                }

                Dictionary<string, ImportExportFilter> filters = new Dictionary<string, ImportExportFilter>();

                foreach (ExportExtensionAttribute filter in attributes)
                {
                    ImportExportFilter fileFilter = new ImportExportFilter(filter.Name, filter.Extension);

                    if (!filters.ContainsKey(fileFilter.Extension))
                    {
                        filters.Add(fileFilter.Extension, fileFilter);
                    }
                }

                this.Filters = filters.Values.ToList().AsReadOnly();
            }

            /// <summary>
            /// Gets file dialog filter
            /// </summary>
            public ReadOnlyCollection<ImportExportFilter> Filters { get; }

            /// <summary>
            /// Run plugin
            /// </summary>
            /// <param name="models">Models to export</param>
            /// <param name="file">Path to the file to import</param>
            public void Run(ModelData.ModelCollection models, string file)
            {
                ((IExport)Activator.CreateInstance(this.EntryPoint)).ExportFile(models, file);
            }
        }

        /// <summary>
        /// Import/Export plugin file dialog filters
        /// </summary>
        public sealed class ImportExportFilter
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ImportExportFilter"/> class
            /// </summary>
            /// <param name="name">Extension type name</param>
            /// <param name="extension">File extension</param>
            internal ImportExportFilter(string name, string extension)
            {
                this.Name = name.Replace("|", string.Empty).Replace(";", string.Empty);
                this.Extension = "*." + extension.Replace("|", string.Empty).Replace(";", string.Empty);
            }

            /// <summary>
            /// Gets file extension
            /// </summary>
            public string Extension { get; }

            /// <summary>
            /// Gets extension type name
            /// </summary>
            public string Name { get; }
        }

        /// <summary>
        /// Import plugin
        /// </summary>
        public sealed class ImportPlugin : Plugin
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ImportPlugin"/> class
            /// </summary>
            /// <param name="file">Path to the DLL file</param>
            /// <param name="entryPoint">Plugin entry point</param>
            /// <param name="attribute">Plugin entry class attribute data</param>
            internal ImportPlugin(string file, Type entryPoint, PluginAttribute attribute) : base(file, entryPoint, attribute)
            {
                List<ImportExtensionAttribute> attributes = entryPoint.GetCustomAttributes(typeof(ImportExtensionAttribute), false).OfType<ImportExtensionAttribute>().ToList();

                if (!attributes.Any())
                {
                    throw new Exception("'ImportExtension' attribute is missing!");
                }

                Dictionary<string, ImportExportFilter> filters = new Dictionary<string, ImportExportFilter>();

                foreach (ImportExtensionAttribute filter in attributes)
                {
                    ImportExportFilter fileFilter = new ImportExportFilter(filter.Name, filter.Extension);

                    if (!filters.ContainsKey(fileFilter.Extension))
                    {
                        filters.Add(fileFilter.Extension, fileFilter);
                    }
                }

                this.Filters = filters.Values.ToList().AsReadOnly();
            }

            /// <summary>
            /// Gets file dialog filter
            /// </summary>
            public ReadOnlyCollection<ImportExportFilter> Filters { get; }

            /// <summary>
            /// Run plugin
            /// </summary>
            /// <param name="file">Path to the file to import</param>
            /// <returns>Collection of imported models</returns>
            public ModelData.ModelCollection Run(string file)
            {
                return ((IImport)Activator.CreateInstance(this.EntryPoint)).ImportFile(file);
            }
        }

        /// <summary>
        /// Plugin class
        /// </summary>
        public abstract class Plugin
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Plugin"/> class
            /// </summary>
            /// <param name="file">Path to the DLL file</param>
            /// <param name="entryPoint">Plugin entry point</param>
            /// <param name="attribute">Plugin entry class attribute data</param>
            internal Plugin(string file, Type entryPoint, PluginAttribute attribute)
            {
                this.File = Path.GetFileName(file);
                this.EntryPoint = entryPoint;
                this.Name = string.IsNullOrWhiteSpace(attribute.PluginName) ? Path.GetFileNameWithoutExtension(file) : attribute.PluginName;
                this.Description = string.IsNullOrWhiteSpace(attribute.Description) ? string.Empty : attribute.Description;
            }

            /// <summary>
            /// Gets plugin description
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Gets plugin entry point
            /// </summary>
            public Type EntryPoint { get; }

            /// <summary>
            /// Gets file name of the plugin with full path
            /// </summary>
            public string File { get; }

            /// <summary>
            /// Gets plugin name
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Get plugin from type
            /// </summary>
            /// <param name="type">Plugin type</param>
            /// <param name="file">Assembly path</param>
            /// <param name="entryPoint">Plugin entry point</param>
            /// <param name="attribute">Plugin attribute data</param>
            /// <returns>Loaded plugin</returns>
            internal static Plugin GetPluginFromType(int type, string file, Type entryPoint, PluginAttribute attribute)
            {
                switch (type)
                {
                    case 0:
                        return new ExportPlugin(file, entryPoint, attribute);

                    case 1:
                        return new ImportPlugin(file, entryPoint, attribute);

                    default:
                        throw new NotSupportedException();
                }
            }
        }
    }
}