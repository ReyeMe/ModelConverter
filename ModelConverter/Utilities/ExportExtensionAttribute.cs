namespace ModelConverter.Utilities
{
    using System;

    /// <summary>
    /// Import/Export plugin supported file
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ExportExtensionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportExtensionAttribute"/> class
        /// </summary>
        /// <param name="name">Extension name</param>
        /// <param name="extension">File extension (does not include .)</param>
        public ExportExtensionAttribute(string name, string extension)
        {
            this.Extension = extension;
            this.Name = name;
        }

        /// <summary>
        /// Gets or sets file extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets file type name
        /// </summary>
        public string Name { get; set; }
    }
}