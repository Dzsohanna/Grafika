using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 4;

        private double AngleToZYPlane = 0;

        private double AngleToZXPlane = 0;

        private const double DistanceScaleFactor = 1.1;

        private const double AngleChangeStepSize = Math.PI / 180 * 5;

        public Vector3D<float> Position { get; private set; } = new Vector3D<float>(0, 2, 6);
        public Vector3D<float> Target { get; private set; } = Vector3D<float>.Zero;
        public Vector3D<float> UpVector { get; private set; } = Vector3D<float>.UnitY;

        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        public void MoveForward(float amount)
        {
             var direction = Vector3D.Normalize(-Position);
            DistanceToOrigin -= amount;

            if (DistanceToOrigin < 0.1f) DistanceToOrigin = 0.1f; 
        }


        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }

        public void SetTopRearView()
        {
            DistanceToOrigin = 8;
            AngleToZXPlane = Math.PI/10;
            AngleToZYPlane = Math.PI *2;
        }
        public enum CameraMode
        {
            Follow,     
            FirstPerson, 
            TopDown     
        }

        private CameraMode currentMode = CameraMode.Follow;

        public void ToggleCameraView()
        {
            currentMode = (CameraMode)(((int)currentMode + 1) % Enum.GetValues(typeof(CameraMode)).Length);
        }

        public void FollowTarget(Vector3D<float> targetPosition, float targetRotation)
        {
            switch (currentMode)
            {
                case CameraMode.Follow:
                    float distanceBehind = -5f;
                    float heightAbove = 1.5f;
                    var offsetX = (float)(Math.Sin(targetRotation) * distanceBehind);
                    var offsetZ = (float)(Math.Cos(targetRotation) * distanceBehind);

                    Position = new Vector3D<float>(
                        targetPosition.X - offsetX,
                        targetPosition.Y + heightAbove,
                        targetPosition.Z - offsetZ
                    );
                    Target = targetPosition;
                    UpVector = Vector3D<float>.UnitY;
                    break;

                case CameraMode.FirstPerson:
                    float eyeHeight = 0.8f; 
                    float lookAheadDistance = 10f; 

                    Position = new Vector3D<float>(
                        targetPosition.X,
                        targetPosition.Y + eyeHeight,
                        targetPosition.Z
                    );

                    Target = new Vector3D<float>(
                        targetPosition.X - (float)Math.Sin(targetRotation) * lookAheadDistance,
                        targetPosition.Y + eyeHeight,
                        targetPosition.Z - (float)Math.Cos(targetRotation) * lookAheadDistance
                    );

                    UpVector = Vector3D<float>.UnitY;
                    break;

                case CameraMode.TopDown:
                    Position = new Vector3D<float>(targetPosition.X, 10f, targetPosition.Z +15f);
                    Target = targetPosition;
                    UpVector = Vector3D<float>.UnitY;
                    break;
            }
        }


    }
}
