namespace Wavefront
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;
    using ModelConverter;
    using ModelConverter.Utilities;

    /// <summary>
    /// Import wavefront file
    /// </summary>
    [Plugin("Wavefront model import", "Imports *.obj files")]
    [ImportExtension("Wavefront file", "obj")]
    public class Wavefront : IImport
    {
        /// <summary>
        /// Import model
        /// </summary>
        /// <param name="filePath">File to import</param>
        public ModelData.ModelCollection ImportFile(string filePath)
        {
            ModelData.ModelCollection models = new ModelData.ModelCollection();
            string lastMaterial = string.Empty;

            foreach (string line in File.ReadLines(filePath).Where(line => !line.StartsWith("#") && !line.StartsWith("vp") && !line.StartsWith("l") && line.Contains(' ')))
            {
                string lineCode = line.Substring(0, line.IndexOf(' ')).Trim();

                switch (lineCode)
                {
                    case "o":
                        models.Add(new ModelData.Model() { Name = line.Remove(0, 2).Trim() });
                        break;

                    case "usemtl":
                        lastMaterial = line.Substring(6).Trim();
                        break;

                    case "v":
                        models.Vertices.Add(Wavefront.ParseVertex(line));
                        break;

                    case "vn":
                        models.Normals.Add(Wavefront.ParseNormal(line));
                        break;

                    case "f":

                        if (!models.Any())
                        {
                            models.Add(new ModelData.Model());
                        }

                        models.Last().Faces.Add(Wavefront.ParseFace(line, lastMaterial));
                        break;

                    default:
                        break;
                }
            }

            Wavefront.ReadMtl(models, filePath);
            return models;
        }

        /// <summary>
        /// Get absolute path to the texture file
        /// </summary>
        /// <param name="mtlPath">MTL texture path</param>
        /// <param name="modelFolder">Model folder path</param>
        /// <returns>Absolute texture file path</returns>
        private static string GetAbsoluteTexturePath(string mtlPath, string modelFolder)
        {
            try
            {
                if (File.Exists(mtlPath))
                {
                    return mtlPath.ToLower();
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            if (!string.IsNullOrWhiteSpace(mtlPath))
            {
                return Path.Combine(modelFolder, mtlPath).ToLower();
            }

            return null;
        }

        /// <summary>
        /// parse color
        /// </summary>
        /// <param name="line">Diffuse color</param>
        /// <returns>Solid color</returns>
        private static SolidColorBrush ParseColor(string line)
        {
            Point3D color = Wavefront.ParseVertex(line);
            return new SolidColorBrush(Color.FromRgb((byte)(byte.MaxValue * color.X), (byte)(byte.MaxValue * color.Y), (byte)(byte.MaxValue * color.Z)));
        }

        /// <summary>
        /// Parse face line
        /// </summary>
        /// <param name="line">Face line</param>
        /// <param name="material">Current material</param>
        /// <returns>Parsed face</returns>
        private static ModelData.Face ParseFace(string line, string material)
        {
            ModelData.Face face = new ModelData.Face { Material = material };

            foreach (string vertex in line.Substring(1).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] components = vertex.Split(new[] { '/' }, StringSplitOptions.None);

                if (components.Any())
                {
                    int temp;

                    if (int.TryParse(components.First(), out temp))
                    {
                        face.Vertices.Add(temp - 1);
                    }

                    if (components.Length == 3 && int.TryParse(components.Last(), out temp))
                    {
                        face.Normals.Add(temp - 1);
                    }
                }
            }

            return face;
        }

        /// <summary>
        /// Parse normal
        /// </summary>
        /// <param name="line">Normal line</param>
        /// <returns>Parsed normal</returns>
        private static Vector3D ParseNormal(string line)
        {
            Point3D coordinates = Wavefront.ParseVertex(line);
            return new Vector3D(coordinates.X, coordinates.Y, coordinates.Z);
        }

        /// <summary>
        /// Parse vertex
        /// </summary>
        /// <param name="line">Vertex line</param>
        /// <returns>Parsed vertex</returns>
        private static Point3D ParseVertex(string line)
        {
            List<double> coordinates = line
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(3)
                .Select(coordinate =>
                {
                    double value = 0.0;
                    double.TryParse(coordinate, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                    return value;
                })
                .ToList();

            return new Point3D(coordinates[0], coordinates[1], coordinates[2]);
        }

        /// <summary>
        /// Read texture definition file
        /// </summary>
        /// <param name="models">Loaded models</param>
        /// <param name="waveFrontFile">Path to the WaveFront file</param>
        private static void ReadMtl(ModelData.ModelCollection models, string waveFrontFile)
        {
            models.MaterialTextures = new Dictionary<string, ModelData.Material>();
            models.MaterialTextures.Add(string.Empty, new ModelData.Material { Color = Brushes.White, Texture = null, TexturePath = string.Empty });
            string modelDirectory = Path.GetDirectoryName(waveFrontFile);
            string mtlFile = Path.Combine(modelDirectory, Path.GetFileNameWithoutExtension(waveFrontFile) + ".mtl");
            Dictionary<string, BitmapSource> tgaFiles = new Dictionary<string, BitmapSource>();

            if (File.Exists(mtlFile))
            {
                foreach (string line in File.ReadLines(mtlFile).Where(line => !string.IsNullOrEmpty(line) && line.Contains(" ")))
                {
                    string lineCode = line.Substring(0, line.IndexOf(' ')).Trim();

                    switch (lineCode.ToLower())
                    {
                        case "newmtl":
                            models.MaterialTextures.Add(line.Replace(lineCode, string.Empty).Trim(), new ModelData.Material());
                            break;

                        case "kd":
                            models.MaterialTextures.Last().Value.Color = Wavefront.ParseColor(line);
                            break;

                        case "map_kd":
                            string file = Wavefront.GetAbsoluteTexturePath(line.Replace(lineCode, string.Empty).Trim(), modelDirectory);

                            if (!tgaFiles.ContainsKey(file))
                            {
                                BitmapSource source = Extensions.LoadTga(file);

                                if (source != null)
                                {
                                    models.MaterialTextures.Last().Value.Texture = source;
                                    models.MaterialTextures.Last().Value.TexturePath = file;
                                }
                            }
                            else
                            {
                                models.MaterialTextures.Last().Value.Texture = tgaFiles[file];
                            }

                            break;

                        default:
                            break;
                    }
                }
            }
        }
    }
}