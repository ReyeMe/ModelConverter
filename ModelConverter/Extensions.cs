namespace ModelConverter
{
    using System;
    using System.IO;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Contains object extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets distance between two points
        /// </summary>
        /// <param name="from">First point</param>
        /// <param name="to">Second point</param>
        /// <returns>Distance between two points</returns>
        public static double DistanceTo(this Point3D from, Point3D to)
        {
            return Math.Sqrt(Math.Pow(to.X + from.X, 2.0) + Math.Pow(to.Y + from.Y, 2.0) + Math.Pow(to.Z + from.Z, 2.0));
        }

        /// <summary>
        /// Load TGA file as bitmap source
        /// </summary>
        /// <param name="file">Absolute path to the TGA file</param>
        /// <returns>Bitmap source for model view</returns>
        public static BitmapSource LoadTga(string file)
        {
            try
            {
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        TgaLib.TgaImage tga = new TgaLib.TgaImage(reader);
                        BitmapSource source = tga.GetBitmap();
                        source.Freeze();
                        return source;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return null;
        }
    }
}