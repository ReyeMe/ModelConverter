using System;
using System.IO;

namespace ModelConverter
{
    /// <summary>
    /// Application settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets last path of open file dialog
        /// </summary>
        public string LastOpenPath { get; set; }

        /// <summary>
        /// Gets or sets last path of export file dialog
        /// </summary>
        public string LastExportPath { get; set; }

        /// <summary>
        /// Load settings
        /// </summary>
        /// <returns>Loaded settings</returns>
        public static Settings Load()
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(File.ReadAllText("Settings.json"));
            }
            catch (Exception ex)
            {
                ex.ToString();
                return new Settings();
            }
        }

        /// <summary>
        /// Save settigns
        /// </summary>
        public void Save()
        {
            try
            {
                string data = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        Formatting = Newtonsoft.Json.Formatting.Indented,
                        Culture = System.Globalization.CultureInfo.InvariantCulture,
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto
                    });

                File.WriteAllText("Settings.json", data);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }
    }
}
