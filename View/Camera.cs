using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XChunk.Chunk;

namespace XChunk.View
{
    public class Camera
    {
        public static Vector3 REFERENCEVECTOR = new Vector3(0, 0, -1);

        public static GraphicsDevice GraphicsDevice { get; set; }

        public static float Yaw { get; private set; }
        public static float Pitch { get; private set; }

        public static float MouseSensity { get; set; }
        public static float Velocity { get; set; }

        public static Matrix View, Projection;

        public static Vector3 CameraPosition { get; set; }
        public static Vector3 Direction { get; private set; }

        public static BoundingFrustum ViewFrustum;

        private static int oldX, oldY;

        public Camera(GraphicsDevice device, float dpi, float velocity)
        {
            GraphicsDevice = device;

            MouseSensity = dpi;
            Velocity = velocity;

            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, .1f, ChunkRenderer.SIZE * 64); ;

            CameraPosition = new Vector3(16 * (ChunkRenderer.SIZE / 2), 256, 16 * (ChunkRenderer.SIZE / 2));

        }

        public static void Update()
        {
            float dX = Mouse.GetState().X - oldX;
            float dY = Mouse.GetState().Y - oldY;

            Pitch += -MouseSensity * dY;
            Yaw += -MouseSensity * dX;

            Pitch = MathHelper.Clamp(Pitch, -1.5f, 1.5f);


            Matrix rotation = Matrix.CreateRotationX(Pitch) * Matrix.CreateRotationY(Yaw);
            Vector3 transformedVector = Vector3.Transform(REFERENCEVECTOR, rotation);
            Direction = CameraPosition + transformedVector;
            View = Matrix.CreateLookAt(CameraPosition, Direction, new Vector3(0, 1, 0));
            try
            {
                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
            }
            catch { }
            oldX = GraphicsDevice.Viewport.Width / 2;
            oldY = GraphicsDevice.Viewport.Height / 2;

            ViewFrustum = new BoundingFrustum(View * Projection);
        }

        public void Move(Vector3 v)
        {
            Matrix rotation =  Matrix.CreateRotationY(Yaw);
            Vector3 transformed = Vector3.Transform(v, rotation);

            CameraPosition += transformed * Velocity;
        }
    }
}
