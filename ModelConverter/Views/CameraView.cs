namespace ModelConverter.Views
{
    using System;
    using System.Windows;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Camera data view
    /// </summary>
    public class CameraView : BindingSource
    {
        /// <summary>
        /// Camera look direction
        /// </summary>
        private Vector3D direction;

        /// <summary>
        /// Far clipping plane distance
        /// </summary>
        private double farPlane;

        /// <summary>
        /// Camera field of view
        /// </summary>
        private double fov;

        /// <summary>
        /// Near clipping plane distance
        /// </summary>
        private double nearPlane;

        /// <summary>
        /// Camera position
        /// </summary>
        private Point3D position;

        /// <summary>
        /// Center point of the scene
        /// </summary>
        private Point3D? sceneCenter = null;

        /// <summary>
        /// Camera up direction
        /// </summary>
        private Vector3D directionUp;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraView"/> class
        /// </summary>
        /// <param name="direction">Look direction</param>
        /// <param name="directionUp">Up direction</param>
        /// <param name="position">Camera position</param>
        /// <param name="fov">Camera field of view</param>
        /// <param name="farPlane">Far clipping plane</param>
        /// <param name="nearPlane">Near clipping plane</param>
        public CameraView(Vector3D direction, Vector3D directionUp, Point3D position, double fov, double farPlane, double nearPlane)
        {
            this.Direction = direction;
            this.UpDirection = directionUp;
            this.Position = position;
            this.FieldOfView = fov;
            this.nearPlane = nearPlane;
            this.farPlane = farPlane;
        }

        /// <summary>
        /// Gets or sets camera direction
        /// </summary>
        public Vector3D Direction
        {
            get
            {
                return this.direction;
            }

            set
            {
                this.direction = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets far clipping plane
        /// </summary>
        public double FarPlane
        {
            get
            {
                return this.farPlane;
            }

            set
            {
                this.farPlane = Math.Max(Math.Abs(value), this.NearPlane + 1.0);
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets camera field of view
        /// </summary>
        public double FieldOfView
        {
            get
            {
                return this.fov;
            }

            set
            {
                this.fov = Math.Max(Math.Abs(value), 1.0);
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets near clipping plane
        /// </summary>
        public double NearPlane
        {
            get
            {
                return this.nearPlane;
            }

            set
            {
                this.farPlane = Math.Max(Math.Abs(value), 1.0);
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets camera position
        /// </summary>
        public Point3D Position
        {
            get
            {
                return this.position;
            }

            set
            {
                this.position = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets camera up direction
        /// </summary>
        public Vector3D UpDirection
        {
            get
            {
                return this.directionUp;
            }

            set
            {
                this.directionUp = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Rotate camera using mouse
        /// </summary>
        /// <param name="delta">Mouse movement delta</param>
        public void Rotate(Vector delta)
        {
            if (!this.sceneCenter.HasValue)
            {
                return;
            }

            Vector3D direction = new Vector3D(this.Direction.X, this.Direction.Y, this.Direction.Z);
            direction.Normalize();
            Vector3D up = new Vector3D(this.UpDirection.X, this.UpDirection.Y, this.UpDirection.Z);
            up.Normalize();
            Vector3D side = Vector3D.CrossProduct(direction, up);
            side.Normalize();
            up = Vector3D.CrossProduct(direction, side);
            up.Normalize();

            Point3D vectorBase = this.Position + (direction * 15.0);

            RotateTransform3D transform3D = new RotateTransform3D(new QuaternionRotation3D(new Quaternion(side, delta.Y)), this.sceneCenter.Value);
            this.Position = transform3D.Transform(this.Position);
            vectorBase = transform3D.Transform(vectorBase);
            transform3D = new RotateTransform3D(new AxisAngleRotation3D(up, -delta.X), this.sceneCenter.Value);
            this.Position = transform3D.Transform(this.Position);
            vectorBase = transform3D.Transform(vectorBase);
            Vector3D newLook = vectorBase - this.Position;
            newLook.Normalize();
            this.Direction = newLook;
        }

        /// <summary>
        /// Zoom camera in and out
        /// </summary>
        /// <param name="delta">Mouse wheel delta</param>
        public void Zoom(int delta)
        {
            if (!this.sceneCenter.HasValue)
            {
                return;
            }

            double movementDelta = delta / 100.0;

            if (delta > 0 && this.position.DistanceTo(this.sceneCenter.Value) > movementDelta)
            {
                this.Position = Point3D.Add(this.Position, this.Direction * movementDelta);
            }
            else if (delta < 0)
            {
                this.Position = Point3D.Add(this.Position, this.Direction * movementDelta);
            }
        }

        /// <summary>
        /// Zoom all object into the view
        /// </summary>
        /// <param name="scene">Scene geometry</param>
        public void ZoomFit(Model3DCollection scene)
        {
            Point3D minimum = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Point3D maximum = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (Model3D model in scene)
            {
                if (!(model is GeometryModel3D) || (model is GeometryModel3D && !(((GeometryModel3D)model).Material is EmissiveMaterial)))
                {
                    Rect3D bounds = model.Bounds;

                    if (!bounds.IsEmpty)
                    {
                        Point3D boundsSecondPoint = new Point3D(bounds.Location.X + bounds.Size.X, bounds.Location.Y + bounds.Size.Y, bounds.Location.Z + bounds.Size.Z);

                        minimum.X = Math.Min(Math.Min(bounds.Location.X, boundsSecondPoint.X), minimum.X);
                        minimum.Y = Math.Min(Math.Min(bounds.Location.Y, boundsSecondPoint.Y), minimum.Y);
                        minimum.Z = Math.Min(Math.Min(bounds.Location.Z, boundsSecondPoint.Z), minimum.Z);

                        maximum.X = Math.Max(Math.Max(bounds.Location.X, boundsSecondPoint.X), maximum.X);
                        maximum.Y = Math.Max(Math.Max(bounds.Location.Y, boundsSecondPoint.Y), maximum.Y);
                        maximum.Z = Math.Max(Math.Max(bounds.Location.Z, boundsSecondPoint.Z), maximum.Z);
                    }
                }
            }

            if ((double.IsInfinity(minimum.X) || double.IsInfinity(minimum.X) || double.IsInfinity(minimum.X)) ||
                (double.IsInfinity(maximum.X) || double.IsInfinity(maximum.X) || double.IsInfinity(maximum.X)))
            {
                return;
            }

            // Calculate bounding box sphere and camera offset
            Point3D center = new Point3D((minimum.X + maximum.X) / 2.0, (minimum.Y + maximum.Y) / 2.0, (minimum.Z + maximum.Z) / 2.0);
            double radius = center.DistanceTo(maximum);
            double camDistance = (radius * 1.4) / Math.Tan(((Math.PI / 180.0) * this.FieldOfView) / 2.0);

            this.sceneCenter = center;

            // Set camera
            this.Position = Point3D.Add(center, this.Direction * -Math.Abs(camDistance));
        }
    }
}