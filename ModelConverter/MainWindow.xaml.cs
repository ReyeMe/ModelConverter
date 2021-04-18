namespace ModelConverter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Interaction logic for main window
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Hit test buffer
        /// </summary>
        private readonly SortedList<double, GeometryModel3D> hitTestBuffer = new SortedList<double, GeometryModel3D>();

        /// <summary>
        /// Last known mouse position
        /// </summary>
        private Point? lastMousePosition = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets latest selected geometry
        /// </summary>
        private GeometryModel3D LatestSelectedGeometry
        {
            get
            {
                return ((Views.MainView)this.DataContext).LatestSelectedGeometry;
            }

            set
            {
                ((Views.MainView)this.DataContext).LatestSelectedGeometry = value;
            }
        }

        /// <summary>
        /// Return the result of the hit test to the callback
        /// </summary>
        /// <param name="result">Hit test result</param>
        /// <returns>Result behavior</returns>
        public HitTestResultBehavior HitTestResult(HitTestResult result)
        {
            RayMeshGeometry3DHitTestResult mesh = result as RayMeshGeometry3DHitTestResult;

            if (mesh != null && ((GeometryModel3D)mesh.ModelHit) != null && !(((GeometryModel3D)mesh.ModelHit).Material is EmissiveMaterial))
            {
                this.hitTestBuffer.Add(mesh.DistanceToRayOrigin, (GeometryModel3D)mesh.ModelHit);
            }

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// Mouse is moving around
        /// </summary>
        /// <param name="sender">Capture grid</param>
        /// <param name="e">Mouse move event</param>
        private void Viewport3DMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                // Capture mouse on drag
                if (!this.CaptureGrid.IsMouseCaptured)
                {
                    this.CaptureGrid.CaptureMouse();
                    this.lastMousePosition = e.GetPosition(this.CaptureGrid);
                    return;
                }

                // Rotate camera
                if (this.lastMousePosition.HasValue)
                {
                    Point current = e.GetPosition(this.CaptureGrid);
                    ((Views.MainView)this.DataContext).Camera.Rotate(this.lastMousePosition.Value - current);
                    this.lastMousePosition = current;

                    // Rotate sun
                    DirectionalLight sun = ((Views.MainView)this.DataContext).Scene.OfType<DirectionalLight>().FirstOrDefault();

                    if (sun != null)
                    {
                        sun.Direction = ((Views.MainView)this.DataContext).Camera.Direction;
                    }
                }
            }
        }

        /// <summary>
        /// Select face or release mouse
        /// </summary>
        /// <param name="sender">Capture grid</param>
        /// <param name="e">Mouse button event</param>
        private void Viewport3DMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.CaptureGrid.IsMouseCaptured)
            {
                // Release mouse
                this.CaptureGrid.ReleaseMouseCapture();
                this.lastMousePosition = null;
            }
            else if (e.ChangedButton != System.Windows.Input.MouseButton.Right)
            {
                GeometryModel3D lastSelected = this.LatestSelectedGeometry;

                this.hitTestBuffer.Clear();
                Point currentPosition = e.GetPosition(this.Viewport);
                VisualTreeHelper.HitTest(this.Viewport, null, new HitTestResultCallback(this.HitTestResult), new PointHitTestParameters(currentPosition));

                if (this.LatestSelectedGeometry != null)
                {
                    this.LatestSelectedGeometry.Material = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.Material).Brush) { Color = Colors.White };
                    this.LatestSelectedGeometry.BackMaterial = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.BackMaterial).Brush) { Color = Colors.White };
                    this.LatestSelectedGeometry = null;
                }

                ((Views.MainView)this.DataContext).SelectedFace = null;

                if (this.hitTestBuffer.Any())
                {
                    GeometryModel3D model = this.hitTestBuffer.First().Value;

                    if (lastSelected != model)
                    {
                        this.LatestSelectedGeometry = model;
                        this.LatestSelectedGeometry.Material = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.Material).Brush) { Color = Colors.Red };
                        this.LatestSelectedGeometry.BackMaterial = new DiffuseMaterial(((DiffuseMaterial)this.LatestSelectedGeometry.BackMaterial).Brush) { Color = Colors.Red };
                    }

                    List<GeometryModel3D> sceneGeometries = ((Views.MainView)this.DataContext).Scene
                        .OfType<GeometryModel3D>()
                        .Where(geometry => geometry.Material is DiffuseMaterial || (geometry.Material is EmissiveMaterial && !(((EmissiveMaterial)geometry.Material).Brush is VisualBrush)))
                        .ToList();

                    int index = sceneGeometries.IndexOf(model);

                    if (index >= 0)
                    {
                        ((Views.MainView)this.DataContext).SelectedFace = ((Views.MainView)this.DataContext).LoadedModels
                            .SelectMany(geometry => geometry.Faces)
                            .ElementAtOrDefault(index);
                    }
                }
            }
        }

        /// <summary>
        /// Zoom camera with mouse wheel
        /// </summary>
        /// <param name="sender">Capture grid</param>
        /// <param name="e">Mouse wheel event</param>
        private void Viewport3DMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ((Views.MainView)this.DataContext).Camera.Zoom(e.Delta);
        }

        /// <summary>
        /// Window is loaded
        /// </summary>
        /// <param name="sender">Main window object</param>
        /// <param name="e">Empty event</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // We know longer need to size to the contents.
            this.ClearValue(Window.SizeToContentProperty);

            // Don't want our window to be able to get any smaller than this.
            this.SetValue(Window.MinWidthProperty, this.Width);
            this.SetValue(Window.MinHeightProperty, this.Height);
        }
    }
}