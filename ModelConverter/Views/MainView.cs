namespace ModelConverter.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using System.Windows.Shapes;
    using MS = Microsoft.Win32;

    /// <summary>
    /// Main view model
    /// </summary>
    public class MainView : BindingSource
    {
        /// <summary>
        /// Currently loaded models
        /// </summary>
        private Utilities.ModelData.ModelCollection loadedModels = null;

        /// <summary>
        /// 3D scene
        /// </summary>
        private Model3DCollection scene = new Model3DCollection();

        /// <summary>
        /// Selected face
        /// </summary>
        private Utilities.ModelData.Face selectedFace = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainView"/> class
        /// </summary>
        public MainView()
        {
            // Initialize commands
            this.OpenFileCommand = new Utilities.ActionCommand(this.OpenFile);
            this.ExportFileCommand = new Utilities.ActionCommand(this.ExportFile);

            this.RotateLeftCommand = new Utilities.ActionCommand(() => { this.Rotate(false); });
            this.RotateRightCommand = new Utilities.ActionCommand(() => { this.Rotate(true); });

            this.OpenPluginListCommand = new Utilities.ActionCommand(() => { new PluginListWindow().ShowDialog(); });
            this.OpenAboutCommand = new Utilities.ActionCommand(() =>
            {
                string version = typeof(MainView).Assembly.GetName().Version.ToString();
                MessageBox.Show(App.Current.MainWindow, string.Format("Author: David Jurík (www.reye.me)\nVersion:{0}", version), "About", MessageBoxButton.OK);
            });

            Utilities.PluginLoader.Load();
            this.Settings = Settings.Load();
        }

        /// <summary>
        /// Gets 3D view camera
        /// </summary>
        public CameraView Camera { get; } = new CameraView(new Vector3D(-0.58, -0.58, -0.58), new Vector3D(0.0, 0.0, 1.0), new Point3D(10.0, 10.0, 10.0), 75.0, 200.0, 1.0);

        /// <summary>
        /// Gets export file command
        /// </summary>
        public ICommand ExportFileCommand { get; }

        /// <summary>
        /// Gets or sets a value indicating whether selected face is double sided
        /// </summary>
        public bool IsSelectedFaceDoubleSided
        {
            get
            {
                return this.SelectedFace != null ? this.SelectedFace.IsDoubleSided : false;
            }

            set
            {
                if (this.SelectedFace != null)
                {
                    this.SelectedFace.IsDoubleSided = value;
                }

                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether selected face is rendered as mesh
        /// </summary>
        public bool IsSelectedFaceMesh
        {
            get
            {
                return this.SelectedFace != null ? this.SelectedFace.IsMesh : false;
            }

            set
            {
                if (this.SelectedFace != null)
                {
                    this.SelectedFace.IsMesh = value;
                }

                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets latest selected face geometry
        /// </summary>
        public GeometryModel3D LatestSelectedGeometry { get; set; } = null;

        /// <summary>
        /// Gets loaded models
        /// </summary>
        public Utilities.ModelData.ModelCollection LoadedModels
        {
            get
            {
                return this.loadedModels;
            }
        }

        /// <summary>
        /// Gets open about dialog command
        /// </summary>
        public ICommand OpenAboutCommand { get; }

        /// <summary>
        /// Gets open file command
        /// </summary>
        public ICommand OpenFileCommand { get; }

        /// <summary>
        /// Gets command to show list of all plugins
        /// </summary>
        public ICommand OpenPluginListCommand { get; }

        /// <summary>
        /// Gets rotate face indexes left
        /// </summary>
        public ICommand RotateLeftCommand { get; }

        /// <summary>
        /// Gets rotate face indexes right
        /// </summary>
        public ICommand RotateRightCommand { get; }

        /// <summary>
        /// Gets or sets 3D scene data
        /// </summary>
        public Model3DCollection Scene
        {
            get
            {
                return this.scene;
            }

            set
            {
                this.scene = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets currently selected face
        /// </summary>
        public Utilities.ModelData.Face SelectedFace
        {
            get
            {
                return this.selectedFace;
            }

            set
            {
                this.selectedFace = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(this.IsSelectedFaceDoubleSided));
                this.RaisePropertyChanged(nameof(this.IsSelectedFaceMesh));
            }
        }

        /// <summary>
        /// Gets application settings
        /// </summary>
        public Settings Settings { get; }

        /// <summary>
        /// Make grid
        /// </summary>
        /// <returns>WPF 3D grid</returns>
        private static GeometryModel3D MakeGrid()
        {
            // Make grid plane
            MeshGeometry3D mesh = new MeshGeometry3D
            {
                Positions = new Point3DCollection
                    {
                        new Point3D(-10.0, -10.0, 0.0),
                        new Point3D(-10.0, 10.0, 0.0),
                        new Point3D(10.0, 10.0, 0.0),
                        new Point3D(10.0, -10.0, 0.0)
                    },
                TextureCoordinates = new PointCollection
                    {
                        new Point(0.0, 1.0),
                        new Point(1.0, 1.0),
                        new Point(1.0, 0.0),
                        new Point(0.0, 0.0),
                    },
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 },
                Normals = new Vector3DCollection(Enumerable.Repeat(new Vector3D(0.0, 0.0, 1.0), 4)),
            };

            // Make grid brush (I did not manage to get tiling to work at all)
            VisualBrush gridBrush = new VisualBrush() { Stretch = Stretch.Fill };
            Grid grid = new Grid { IsHitTestVisible = false };

            for (int offset = 0; offset < 500; offset += 100)
            {
                double positive = 500.0 + offset;
                double negative = 500.0 - offset;

                if (offset == 0)
                {
                    grid.Children.Add(new Line { X1 = 500.0, X2 = 500.0, Y1 = 0.0, Y2 = 1000.0, Stroke = Brushes.Red, StrokeThickness = 5.0, Stretch = Stretch.None, IsHitTestVisible = false });
                }
                else
                {
                    grid.Children.Add(new Line { X1 = positive, X2 = positive, Y1 = 0.0, Y2 = 1000.0, Stroke = Brushes.LightPink, StrokeThickness = 1.0, Stretch = Stretch.None, IsHitTestVisible = false });
                    grid.Children.Add(new Line { X1 = negative, X2 = negative, Y1 = 0.0, Y2 = 1000.0, Stroke = Brushes.LightPink, StrokeThickness = 1.0, Stretch = Stretch.None, IsHitTestVisible = false });
                }

                if (offset == 0)
                {
                    grid.Children.Add(new Line { X1 = 0.0, X2 = 1000.0, Y1 = 500.0, Y2 = 500.0, Stroke = Brushes.Blue, StrokeThickness = 5.0, Stretch = Stretch.None, IsHitTestVisible = false });
                }
                else
                {
                    grid.Children.Add(new Line { X1 = 0.0, X2 = 1000.0, Y1 = positive, Y2 = positive, Stroke = Brushes.LightBlue, StrokeThickness = 1.0, Stretch = Stretch.None, IsHitTestVisible = false });
                    grid.Children.Add(new Line { X1 = 0.0, X2 = 1000.0, Y1 = negative, Y2 = negative, Stroke = Brushes.LightBlue, StrokeThickness = 1.0, Stretch = Stretch.None, IsHitTestVisible = false });
                }
            }

            gridBrush.Visual = grid;

            return new GeometryModel3D
            {
                Geometry = mesh,
                Material = new EmissiveMaterial(gridBrush),
                BackMaterial = new EmissiveMaterial(gridBrush)
            };
        }

        /// <summary>
        /// Export model file
        /// </summary>
        private void ExportFile()
        {
            List<Utilities.PluginLoader.ExportPlugin> plugins = Utilities.PluginLoader.GetAllPlugins<Utilities.PluginLoader.ExportPlugin>();

            if (plugins.Any())
            {
                MS.SaveFileDialog saveFile = new MS.SaveFileDialog
                {
                    Filter = string.Join("|", plugins.SelectMany(plugin => plugin.Filters.Select(filter => filter.Name + "|" + filter.Extension))),
                    OverwritePrompt = true,
                    AddExtension = true,
                    Title = "Export model file",
                    InitialDirectory = string.IsNullOrWhiteSpace(this.Settings.LastExportPath) ? string.Empty : this.Settings.LastExportPath,
                    ValidateNames = true
                };

                if (saveFile.ShowDialog().Value)
                {
                    this.Settings.LastExportPath = System.IO.Path.GetDirectoryName(saveFile.FileName);
                    this.Settings.Save();

                    try
                    {
                        plugins[saveFile.FilterIndex - 1].Run(this.loadedModels, saveFile.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            App.Current.MainWindow,
                            "There was an error while exporting file!\n" + ex.Message,
                            "Export failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// open new model file
        /// </summary>
        private void OpenFile()
        {
            List<Utilities.PluginLoader.ImportPlugin> plugins = Utilities.PluginLoader.GetAllPlugins<Utilities.PluginLoader.ImportPlugin>();

            if (plugins.Any())
            {
                MS.OpenFileDialog openFile = new MS.OpenFileDialog
                {
                    Filter = string.Join("|", plugins.SelectMany(plugin => plugin.Filters.Select(filter => filter.Name + "|" + filter.Extension))),
                    Multiselect = false,
                    ValidateNames = true,
                    ShowReadOnly = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    InitialDirectory = string.IsNullOrWhiteSpace(this.Settings.LastOpenPath) ? string.Empty : this.Settings.LastOpenPath,
                    Title = "Open model file"
                };

                if (openFile.ShowDialog(App.Current.MainWindow).Value)
                {
                    Utilities.ModelData.ModelCollection loaded = null;
                    this.Settings.LastOpenPath = System.IO.Path.GetDirectoryName(openFile.FileName);
                    this.Settings.Save();

                    try
                    {
                        loaded = plugins[openFile.FilterIndex - 1].Run(openFile.FileName);

                        if (loaded == null)
                        {
                            throw new InvalidOperationException("Return value from 'ImportFile()' must not be 'null'!");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            App.Current.MainWindow,
                            "There was an error while importing file!\n" + ex.Message,
                            "Import failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        return;
                    }

                    this.LatestSelectedGeometry = null;
                    this.SelectedFace = null;
                    this.loadedModels = loaded;

                    // Reload scene
                    this.Reload();

                    this.Camera.ZoomFit(this.Scene);
                }
            }
        }

        /// <summary>
        /// Reload scene
        /// </summary>
        private void Reload()
        {
            // Create empty scene
            Model3DCollection newScene = new Model3DCollection();

            // Add light
            DirectionalLight sun = new DirectionalLight(Colors.White, this.Camera.Direction);
            newScene.Add(sun);

            foreach (Model3D model in (List<Model3D>)this.loadedModels)
            {
                newScene.Add(model);
            }

            newScene.Add(MainView.MakeGrid());

            // Set scene and zoom camera
            this.Scene = newScene;
        }

        /// <summary>
        /// Rotate face indexes
        /// </summary>
        /// <param name="direction">Rotation direction</param>
        private void Rotate(bool direction)
        {
            if (direction)
            {
                this.SelectedFace.Vertices.Add(this.SelectedFace.Vertices.First());
                this.SelectedFace.Vertices.RemoveAt(0);

                this.SelectedFace.Normals.Add(this.SelectedFace.Normals.First());
                this.SelectedFace.Normals.RemoveAt(0);
            }
            else
            {
                this.SelectedFace.Vertices.Insert(0, this.SelectedFace.Vertices.Last());
                this.SelectedFace.Vertices.RemoveAt(this.SelectedFace.Vertices.Count - 1);

                this.SelectedFace.Normals.Insert(0, this.SelectedFace.Normals.Last());
                this.SelectedFace.Normals.RemoveAt(this.SelectedFace.Vertices.Count - 1);
            }

            int index = -1;

            if (this.LatestSelectedGeometry != null)
            {
                List<GeometryModel3D> sceneGeometries = this.Scene
                            .OfType<GeometryModel3D>()
                            .Where(geometry => geometry.Material is DiffuseMaterial || (geometry.Material is EmissiveMaterial && !(((EmissiveMaterial)geometry.Material).Brush is VisualBrush)))
                            .ToList();

                index = sceneGeometries.IndexOf(this.LatestSelectedGeometry);
            }

            this.Reload();

            if (index >= 0)
            {
                List<GeometryModel3D> sceneGeometries = this.Scene
                            .OfType<GeometryModel3D>()
                            .Where(geometry => geometry.Material is DiffuseMaterial || (geometry.Material is EmissiveMaterial && !(((EmissiveMaterial)geometry.Material).Brush is VisualBrush)))
                            .ToList();

                this.LatestSelectedGeometry = sceneGeometries[index];
                this.LatestSelectedGeometry.Material = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.Material).Brush) { Color = Colors.Red };
                this.LatestSelectedGeometry.BackMaterial = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.BackMaterial).Brush) { Color = Colors.Red };

                this.SelectedFace = this.LoadedModels
                    .SelectMany(geometry => geometry.Faces)
                    .ElementAtOrDefault(index);
            }
        }
    }
}