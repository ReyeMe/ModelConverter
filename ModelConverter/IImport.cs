namespace ModelConverter
{
    /// <summary>
    /// Import interface
    /// </summary>
    public interface IImport
    {
        /// <summary>
        /// Import model
        /// </summary>
        /// <param name="filePath">File to import</param>
        /// <returns>Loaded model collection</returns>
        Utilities.ModelData.ModelCollection ImportFile(string filePath);
    }
}
