namespace ModelConverter.Utilities
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// WaveFront import class
    /// </summary>
    public static class ModelData
    {
        /// <summary>
        /// WaveFront model file face
        /// </summary>
        public class Face
        {
            /// <summary>
            /// Gets or sets a value indicating whether face is double sided
            /// </summary>
            public bool IsDoubleSided { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether face is rendered as mesh
            /// </summary>
            public bool IsMesh { get; set; }

            /// <summary>
            /// Gets or sets material name
            /// </summary>
            public string Material { get; set; } = string.Empty;

            /// <summary>
            /// Gets normal vector indices
            /// </summary>
            public List<int> Normals { get; } = new List<int>();

            /// <summary>
            /// Gets vertices indices
            /// </summary>
            public List<int> Vertices { get; } = new List<int>();
        }

        /// <summary>
        /// MTL material
        /// </summary>
        public class Material
        {
            /// <summary>
            /// Gets or sets material color
            /// </summary>
            public SolidColorBrush Color { get; set; } = Brushes.White;

            /// <summary>
            /// Gets or sets texture path
            /// </summary>
            public BitmapSource Texture { get; set; } = null;

            /// <summary>
            /// Gets or sets path to the texture file
            /// </summary>
            public string TexturePath { get; set; } = string.Empty;
        }

        /// <summary>
        /// WaveFront model file
        /// </summary>
        public class Model
        {
            /// <summary>
            /// Gets model faces
            /// </summary>
            public List<Face> Faces { get; } = new List<Face>();

            /// <summary>
            /// Gets or sets model name
            /// </summary>
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// WaveFront model collection list
        /// </summary>
        public class ModelCollection : List<Model>
        {
            /// <summary>
            /// Gets or sets material texture names
            /// </summary>
            public Dictionary<string, Material> MaterialTextures { get; set; }

            /// <summary>
            /// Gets face normal
            /// </summary>
            public List<Vector3D> Normals { get; } = new List<Vector3D>();

            /// <summary>
            /// Gets model vertices
            /// </summary>
            public List<Point3D> Vertices { get; } = new List<Point3D>();

            /// <summary>
            /// Convert WaveFront model collection into WPF model collection
            /// </summary>
            /// <param name="models">Model collection</param>
            public static implicit operator List<Model3D>(ModelCollection models)
            {
                List<Model3D> wpfModelCollection = new List<Model3D>();
                Point3DCollection vertices = new Point3DCollection(models.Vertices);

                foreach (Model model in models)
                {
                    foreach (Face face in model.Faces)
                    {
                        List<Point3D> faceVertices = new List<Point3D>();
                        List<int> indexes = new List<int>();

                        // Create 2 triangles for each face
                        if (face.Vertices.Count > 3)
                        {
                            faceVertices.AddRange(Enumerable.Range(0, 4).Select(index => vertices[face.Vertices[index]]));

                            indexes.Add(0);
                            indexes.Add(1);
                            indexes.Add(2);

                            indexes.Add(2);
                            indexes.Add(3);
                            indexes.Add(0);
                        }
                        else if (face.Vertices.Count == 3)
                        {
                            // Make quad out of single triangle
                            faceVertices.AddRange(Enumerable.Range(0, 3).Select(index => vertices[face.Vertices[index]]));
                            indexes.AddRange(Enumerable.Range(0, 3));

                            indexes.Add(2);
                            indexes.Add(2);
                            indexes.Add(0);
                        }

                        if (faceVertices.Any() && indexes.Any())
                        {
                            // Assign material brush
                            indexes.Reverse();
                            Brush materialBrush = Brushes.White;

                            if (!string.IsNullOrEmpty(face.Material) && models.MaterialTextures.ContainsKey(face.Material))
                            {
                                Material material = models.MaterialTextures[face.Material];

                                if (material.Texture != null)
                                {
                                    Image image = new Image { Source = material.Texture };
                                    RenderOptions.SetCachingHint(image, CachingHint.Cache);
                                    RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                                    materialBrush = new VisualBrush(image);
                                }
                                else
                                {
                                    materialBrush = material.Color;
                                }
                            }

                            // Generate 3D mesh for each face
                            // This will make it easier to pick faces by clicking them instead of making whole model as one big mesh
                            MeshGeometry3D mesh = new MeshGeometry3D
                            {
                                Positions = new Point3DCollection(faceVertices),
                                TriangleIndices = new Int32Collection(indexes),
                                TextureCoordinates = new PointCollection
                                {
                                    new Point(1.0f, 1.0f),
                                    new Point(0.0f, 1.0f),
                                    new Point(0.0f, 0.0f),
                                    new Point(1.0f, 0.0f),
                                }
                            };

                            GeometryModel3D geometryModel = new GeometryModel3D();
                            geometryModel.Geometry = mesh;
                            geometryModel.Material = new DiffuseMaterial(materialBrush);
                            geometryModel.BackMaterial = new DiffuseMaterial(materialBrush);
                            wpfModelCollection.Add(geometryModel);
                        }
                    }
                }

                return wpfModelCollection;
            }
        }
    }
}