namespace ModelConverter
{
    /// <summary>
    /// Export interface
    /// </summary>
    public interface IExport
    {
        /// <summary>
        /// Export model
        /// </summary>
        /// <param name="model">Model to export</param>
        /// <param name="filePath">File path to export</param>
        void ExportFile(Utilities.ModelData.ModelCollection model, string filePath);
    }
}
