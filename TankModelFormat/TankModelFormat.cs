namespace TankModelFormat
{
    using ModelConverter.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Export 3D object in tank game model format
    /// </summary>
    [Plugin("Tank model format export", "Plugin for exporting models for sega Saturn game \"TankGame\"")]
    [ExportExtension("Tank model file", "TMF")]
    internal class TankModelFormat : ModelConverter.IExport
    {
        /// <summary>
        /// Vertices scale
        /// </summary>
        private const double DoubleScale = 65536.0;

        /// <summary>
        /// Face flags
        /// </summary>
        [Flags]
        public enum TmfFaceFlags : byte
        {
            /// <summary>
            /// No flags applied
            /// </summary>
            None = 0,

            /// <summary>
            /// Face is doulbe sided
            /// </summary>
            DoubleSided = 1,

            /// <summary>
            /// Face is meshed
            /// </summary>
            Meshed = 2
        }

        /// <summary>
        /// Model type
        /// </summary>
        public enum TmFType : byte
        {
            /// <summary>
            ///  Static model
            /// </summary>
            Static = 0
        }

        /// <summary>
        /// Export model
        /// </summary>
        /// <param name="model">Model to export</param>
        /// <param name="filePath">File path to export</param>
        public void ExportFile(ModelData.ModelCollection model, string filePath)
        {
            // Check model integrity
            if (model.Count > byte.MaxValue)
            {
                throw new Exception("Maximum number of models in one file can be 256!");
            }
            else if (model.Count == 0)
            {
                throw new Exception("File does not contain any models");
            }
            else if (model.MaterialTextures.Count > byte.MaxValue)
            {
                throw new Exception("Maximum number of textures refereced in one file can be 256!");
            }

            Dictionary<string, ModelData.Material> materials = model.MaterialTextures.OrderBy(material => material.Value.Texture == null).ToDictionary(item => item.Key, item => item.Value);

            TmfHeader header = new TmfHeader
            {
                Type = TmFType.Static,
                TextureCount = (byte)model.MaterialTextures.Count,
                ModelCount = (byte)model.Count,
                Reserved = Enumerable.Repeat((byte)0x00, 5).ToArray(),
                Textures = materials.Select(material => TankModelFormat.GetTextureEntry(material.Value)).ToArray(),
                Models = model.Select(item => TankModelFormat.GetModelEntry(item, materials, model.Vertices, model.Normals)).ToArray()
            };

            File.WriteAllBytes(filePath, TankModelFormat.GetBytes(header));

            MessageBox.Show(Application.Current.MainWindow, string.Format("Done!\nModels: {0}\nTextures: {1}", header.ModelCount, header.TextureCount), "Export", MessageBoxButton.OK);
        }

        /// <summary>
        /// Get bytes from object
        /// </summary>
        /// <param name="data">Object to get bytes from</param>
        /// <returns>Byte array</returns>
        private static byte[] GetBytes(object data)
        {
            List<byte> bytes = new List<byte>();
            Type dataType = data.GetType();

            if (dataType.IsValueType && !dataType.IsPrimitive && !dataType.IsEnum)
            {
                foreach (FieldInfo field in dataType.GetFields().OrderBy(field => Marshal.OffsetOf(dataType, field.Name).ToInt32()))
                {
                    bytes.AddRange(TankModelFormat.GetBytes(field.GetValue(data)));
                }
            }
            else
            {
                if (dataType.IsArray)
                {
                    foreach (object item in data as Array)
                    {
                        bytes.AddRange(TankModelFormat.GetBytes(item));
                    }
                }
                else if (dataType.IsEnum)
                {
                    bytes.Add((byte)data);
                }
                else
                {
                    int length = Marshal.SizeOf(data);
                    byte[] array = new byte[length];
                    IntPtr ptr = Marshal.AllocHGlobal(length);
                    Marshal.StructureToPtr(data, ptr, true);
                    Marshal.Copy(ptr, array, 0, length);
                    Marshal.FreeHGlobal(ptr);
                    bytes.AddRange(array.Reverse());
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Get face entry
        /// </summary>
        /// <param name="face">Model face</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global vertices</param>
        /// <param name="localVertices">Local vertices</param>
        /// <param name="normals">Global normals</param>
        /// <param name="localNormals">Local normals</param>
        /// <returns>Face entry</returns>
        private static TmfFace GetFaceEntry(
                    ModelData.Face face,
                    Dictionary<string, ModelData.Material> materials,
                    List<Point3D> vertices,
                    Dictionary<int, TmfVertice> localVertices,
                    List<Vector3D> normals)
        {
            ushort[] indexes = face.Vertices
                .Select(vertice => TankModelFormat.GetVerticeEntry(
                    vertice,
                    localVertices,
                    vertices[vertice].X,
                    vertices[vertice].Y,
                    vertices[vertice].Z))
                .ToArray();

            if (indexes.Length < 4)
            {
                indexes = indexes.Concat(Enumerable.Repeat(indexes[indexes.Length - 1], 4 - indexes.Length)).ToArray();
            }

            if (indexes.Length != 4)
            {
                throw new Exception("All faces must be quads!");
            }

            Vector3D faceVector = face.Normals.Select(normal => normals[normal]).Aggregate((a, b) => a + b);
            faceVector.Normalize();

            int materialIndex = materials.Keys.ToList().IndexOf(face.Material);

            if (materialIndex < 0)
            {
                throw new Exception(string.Format("Material '{0}' is missing", face.Material));
            }

            TmfFace entry = new TmfFace
            {
                TextureIndex = (byte)materialIndex,
                Indexes = indexes,
                Normal = TankModelFormat.GetVertice(faceVector.X, faceVector.Y, faceVector.Z),
                Flags = TmfFaceFlags.None,
                Reserved = new byte[] { 0, 0 }
            };

            if (face.IsMesh)
            {
                entry.Flags |= TmfFaceFlags.Meshed;
            }

            if (face.IsDoubleSided)
            {
                entry.Flags |= TmfFaceFlags.DoubleSided;
            }

            return entry;
        }

        /// <summary>
        /// Get model entry
        /// </summary>
        /// <param name="model">3D model</param>
        /// <param name="materials">Model materials</param>
        /// <param name="vertices">Global model vertices</param>
        /// <param name="normals">Global normals</param>
        /// <returns>Model entry</returns>
        private static TmfModelHeader GetModelEntry(
            ModelData.Model model,
            Dictionary<string, ModelData.Material> materials,
            List<Point3D> vertices,
            List<Vector3D> normals)
        {
            Dictionary<int, TmfVertice> localVertices = new Dictionary<int, TmfVertice>();
            TmfFace[] faces = model.Faces.Select(face => TankModelFormat.GetFaceEntry(face, materials, vertices, localVertices, normals)).ToArray();
            TmfVertice[] modelVertices = localVertices.Values.ToArray();

            return new TmfModelHeader
            {
                FaceCount = (ushort)faces.Length,
                VerticesCount = (ushort)modelVertices.Length,
                Vertices = modelVertices,
                Faces = faces
            };
        }

        /// <summary>
        /// Get texture entry from material
        /// </summary>
        /// <param name="material">Face material</param>
        /// <returns>Texture entry</returns>
        private static TmfTextureEntry GetTextureEntry(ModelData.Material material)
        {
            TmfTextureEntry entry = new TmfTextureEntry();

            if (material.Texture != null)
            {
                string file = Path.GetFileName(material.TexturePath).ToUpper();
                byte[] bytes = Encoding.ASCII.GetBytes(file).Take(13).ToArray();
                entry.FileName = bytes.Concat(Enumerable.Repeat((byte)'\0', 13 - bytes.Length)).ToArray();
                entry.Color = Enumerable.Repeat(byte.MaxValue, 3).ToArray();
            }
            else
            {
                entry.FileName = Enumerable.Repeat((byte)'\0', 13).ToArray();
                entry.Color = new byte[]
                {
                    material.Color.Color.R,
                    material.Color.Color.G,
                    material.Color.Color.B
                };
            }

            return entry;
        }

        /// <summary>
        /// Get vertice data
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Vertice data</returns>
        private static TmfVertice GetVertice(double x, double y, double z)
        {
            return new TmfVertice
            {
                X = (Int32)(x * TankModelFormat.DoubleScale),
                Y = (Int32)(y * TankModelFormat.DoubleScale),
                Z = (Int32)(z * TankModelFormat.DoubleScale),
            };
        }

        /// <summary>
        /// Get vertice entry
        /// </summary>
        /// <param name="vertice">Vertice index</param>
        /// <param name="localVertices">List of local vertices</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Vertice entry index</returns>
        private static ushort GetVerticeEntry(int vertice, Dictionary<int, TmfVertice> localVertices, double x, double y, double z)
        {
            int result;

            if (!localVertices.ContainsKey(vertice))
            {
                localVertices.Add(vertice, TankModelFormat.GetVertice(x, y, z));
            }

            result = localVertices.Keys.ToList().IndexOf(vertice);

            if (result > ushort.MaxValue)
            {
                throw new Exception("Too many vertices!");
            }

            return (ushort)result;
        }

        /// <summary>
        /// Model face
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TmfFace
        {
            /// <summary>
            /// Normal vector
            /// </summary>
            public TmfVertice Normal;

            /// <summary>
            /// Face quad indexes
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] Indexes;

            /// <summary>
            /// Face flags (meshed, double-sided, etc...)
            /// </summary>
            public TmfFaceFlags Flags;

            /// <summary>
            /// Texture index
            /// </summary>
            public byte TextureIndex;

            /// <summary>
            /// Reserved space
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] Reserved;

        }

        /// <summary>
        /// File header
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TmfHeader
        {
            /// <summary>
            /// Model file type
            /// </summary>
            public TmFType Type;

            /// <summary>
            /// Number of texture file entries in the file
            /// </summary>
            public byte TextureCount;

            /// <summary>
            /// Number of models in the file
            /// </summary>
            public byte ModelCount;

            /// <summary>
            /// Reserved space for future stuff
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] Reserved;

            /// <summary>
            /// Face textures
            /// </summary>
            public TmfTextureEntry[] Textures;

            /// <summary>
            /// 3D models
            /// </summary>
            public TmfModelHeader[] Models;
        };

        /// <summary>
        /// Model entry header
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TmfModelHeader
        {
            /// <summary>
            /// Number of vertices
            /// </summary>
            public ushort VerticesCount;

            /// <summary>
            /// Number of faces
            /// </summary>
            public ushort FaceCount;

            /// <summary>
            /// Model vertices
            /// </summary>
            public TmfVertice[] Vertices;

            /// <summary>
            /// Model faces
            /// </summary>
            public TmfFace[] Faces;
        };

        /// <summary>
        /// Texture file entry
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TmfTextureEntry
        {
            /// <summary>
            /// File name
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
            public byte[] FileName;

            /// <summary>
            /// Diffuse color
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Color;
        };

        /// <summary>
        /// Model vertice
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TmfVertice
        {
            /// <summary>
            /// X coordinate
            /// </summary>
            public Int32 X;

            /// <summary>
            /// Y coordinate
            /// </summary>
            public Int32 Y;

            /// <summary>
            /// Z coordinate
            /// </summary>
            public Int32 Z;
        }
    }
}