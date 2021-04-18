namespace ModelConverter.Utilities
{
    using System;

    /// <summary>
    /// Import/Export plugin supported file
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ImportExtensionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportExtensionAttribute"/> class
        /// </summary>
        /// <param name="name">Extension name</param>
        /// <param name="extension">File extension (does not include .)</param>
        public ImportExtensionAttribute(string name, string extension)
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