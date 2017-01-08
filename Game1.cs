using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using XChunk.Extensions;
using XChunk.View;
using XChunk.Chunk;
using System.IO;

namespace XChunk
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        private SpriteFont _spriteFont;

        private FrameCounter _frameCounter = new FrameCounter();

        private Camera Camera;
        private ChunkRenderer chunkRenderer;

        private Chunk.XChunk currentChunk;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

        }


        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }


        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            graphics.SynchronizeWithVerticalRetrace = true;
            graphics.PreferredBackBufferWidth = 1920;
            graphics.PreferredBackBufferHeight = 1080;

            graphics.ApplyChanges();

            System.Windows.Forms.Form frm = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Window.Handle);
            frm.Location = new System.Drawing.Point(0, 0);
            frm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            frm.Text = "XChunk";

            _spriteFont = Content.Load<SpriteFont>("debug");

            IsFixedTimeStep = false;

            Camera = new Camera(GraphicsDevice, 0.007f, 1f);
            chunkRenderer = new ChunkRenderer(GraphicsDevice, Content, ChunkRenderer.Technique.Optimization_Memory);
            chunkRenderer.Start();
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here

        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            chunkRenderer.Update(gameTime);
            if (!chunkRenderer.IsReady)
                return;


            if (IsActive)
                Camera.Update();

            if (Keyboard.GetState().IsKeyDown(Keys.W))
                Camera.Move(new Vector3(0, 0, -1));

            if (Keyboard.GetState().IsKeyDown(Keys.A))
                Camera.Move(new Vector3(-1, 0, 0));

            if (Keyboard.GetState().IsKeyDown(Keys.S))
                Camera.Move(new Vector3(0, 0, 1));

            if (Keyboard.GetState().IsKeyDown(Keys.D))
                Camera.Move(new Vector3(1, 0, 0));

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
                Camera.Move(new Vector3(0, 1, 0));


            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
                Camera.Move(new Vector3(0, -1, 0));

            if (Keyboard.GetState().IsKeyDown(Keys.Up))
                Camera.Velocity *= 1.05f;
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
                Camera.Velocity /= 1.05f;

            for (int i = 0; i < chunkRenderer.Chunks.Length; i++)
            {
                if (chunkRenderer.Chunks[i].ChunkBoundingBox.Contains(Camera.CameraPosition) == ContainmentType.Contains)
                {
                    currentChunk = chunkRenderer.Chunks[i];
                    break;
                }
            }



            base.Update(gameTime);
        }

        RasterizerState rasterizerState = new RasterizerState()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None
        };
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);


            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = rasterizerState;

            chunkRenderer.Render(Camera.View, Camera.Projection, Matrix.Identity);

            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _frameCounter.Update(deltaTime);

            var fps = string.Format("FPS: {0}" + Environment.NewLine + "Position: {1}" + Environment.NewLine + "Allocated_Memory: " + ((GC.GetTotalMemory(false) / 1024f) / 1024f) + "MB", _frameCounter.AbnormalFramesPerSecond, Camera.CameraPosition);


            if (currentChunk != null)
                for (int j = 0; j < currentChunk.BoundingBoxes.Count; j++)
                {
                    BoundingBoxRenderer.Render(currentChunk.BoundingBoxes[j], GraphicsDevice, Camera.View, Camera.Projection, Color.Red);
                }

            spriteBatch.Begin();
            spriteBatch.DrawString(_spriteFont, fps, new Vector2(1, 1), Color.Red);

            if (!chunkRenderer.IsReady)
            {
                spriteBatch.DrawString(_spriteFont, "Loading...", new Vector2(1920 / 2 - 50, 1080 / 2), Color.Black);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    public static class BoundingBoxRenderer
    {
        static VertexPositionColor[] verts = new VertexPositionColor[8];
        static short[] indices = new short[]
	    {
		0, 1,
		1, 2,
		2, 3,
		3, 0,
		0, 4,
		1, 5,
		2, 6,
		3, 7,
		4, 5,
		5, 6,
		6, 7,
		7, 4,
    	};

        static BasicEffect effect;

        /// <summary>
        /// Renders the bounding box for debugging purposes.
        /// </summary>
        /// <param name="box">The box to render.</param>
        /// <param name="graphicsDevice">The graphics device to use when rendering.</param>
        /// <param name="view">The current view matrix.</param>
        /// <param name="projection">The current projection matrix.</param>
        /// <param name="color">The color to use drawing the lines of the box.</param>
        public static void Render(
            BoundingBox box,
            GraphicsDevice graphicsDevice,
            Matrix view,
            Matrix projection,
            Color color)
        {
            if (effect == null)
            {
                effect = new BasicEffect(graphicsDevice)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false
                };
            }

            Vector3[] corners = box.GetCorners();
            for (int i = 0; i < 10 - 2; i++)
            {
                verts[i].Position = corners[i];
                verts[i].Color = color;
            }

            effect.View = view;
            effect.Projection = projection;
            effect.CurrentTechnique.Passes[0].Apply();

            graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.LineList,
            verts,
            0,
            8,
            indices,
            0,
            indices.Length / 2);
        }

    }
}
