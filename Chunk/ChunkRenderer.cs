using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using XChunk.Chunk.Component;
using XChunk.MapGen;
using XChunk.Extensions;
using System.Threading;

namespace XChunk.Chunk
{
    public class ChunkRenderer
    {

        public GraphicsDevice Device { get; private set; }
        public ContentManager Content { get; private set; }

        public static int SIZE = 44;

        public static float TEXTURE_RATIO_X { get; private set; }
        public static float TEXTURE_RATIO_Y { get; private set; }

        public XChunk[] Chunks { get; private set; }
        public bool IsReady { get; private set; }

        public Vector3 GlobalTranslation { get; private set; }

        public Dictionary<string, Vector2> TextureAtlas { get; private set; }

        public Technique Optimization { get; private set; }

        private BasicEffect internalEffect;
        private Texture2D textureAtlas;

        private int verticesCount = 0;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        private int lastCount = 0;

        #region InfDev

        SimplexNoiseGenerator simplexNoise = new SimplexNoiseGenerator(15, 1.0f / 512.0f, 1.0f / 512.0f, 1.0f / 512.0f, 1.0f / 512.0f)
        {
            Factor = 150,
            Sealevel = 140,
            Octaves = 4
        };

        #endregion


        public enum Technique
        {
            Optimization_Memory,
            Optimization_FPS
        }
        public enum Direction
        {
            Forward,
            Backward,
            Left,
            Right,
            DEFUALT
        }

        public ChunkRenderer(GraphicsDevice device, ContentManager content, Technique optimization)
        {
            Device = device;
            Content = content;

            Optimization = optimization;

            Chunks = new XChunk[SIZE * SIZE];
            TextureAtlas = new Dictionary<string, Vector2>();

            // TODO: AUTOMATED 
            TEXTURE_RATIO_X = 63.85f / 256.0f;
            TEXTURE_RATIO_Y = 63.85f / 256.0f;


            TextureAtlas.Add("Top_Grass", new Vector2(1, 0));
            TextureAtlas.Add("Side_Dirt", new Vector2(2, 0));

            textureAtlas = Content.Load<Texture2D>("atlas");
            internalEffect = new BasicEffect(Device)
            {
                Texture = textureAtlas,
                TextureEnabled = true
            };
        }

        public void Start()
        {

            XChunk.Width = 16;
            XChunk.Depth = 16;
            XChunk.Height = 256;

            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    var ch = new XChunk(this, new Vector3(i * 16, 0, j * 16), new Vector3(0));
                    var hm = simplexNoise.GetNoiseMap2D(i * 16, j * 16, 16, 16);
                    ch.HeightMap = hm;
                    Chunks[i + j * SIZE] = ch;
                }
            }

            Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                for (int i = 0; i < Chunks.Length; i++)
                    Chunks[i].Generate();

                object raw = File.ReadAllBytes(@"data\chunks\indices.byte").DeserializeToDynamicType();
                int[] indices = (int[])raw;

                indexBuffer = new IndexBuffer(Device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                indexBuffer.SetData<int>(indices);

                indices = new int[0];

                vertexBuffer = new VertexBuffer(Device, typeof(VertexPositionTexture), XChunk.TILE_SIZE * 10, BufferUsage.WriteOnly);

                buildBuffers();
                sw.Stop();
                Console.WriteLine("DONE IN " + sw.Elapsed.TotalSeconds + " SECONDS!");

                IsReady = true;

            });
        }

        private void buildBuffers()
        {
            int offset = 0;

            for (int i = 0; i < Chunks.Length; i++)
            {
                vertexBuffer.SetData<VertexPositionTexture>(VertexPositionTexture.VertexDeclaration.VertexStride * offset, Chunks[i].ChunkVertices, 0, Chunks[i].ChunkVertices.Length, VertexPositionTexture.VertexDeclaration.VertexStride);

                offset += Chunks[i].ChunkVertices.Length;
            }

            verticesCount = XChunk.TILE_SIZE * 2;
            GC.Collect();
            XChunk.IS_OBSOLETE = false;
        }

        public void Update(GameTime gTime)
        {
            if (XChunk.IS_OBSOLETE && IsReady)
                buildBuffers();
        }
        public void Render(Matrix view, Matrix projection, Matrix world)
        {
            if (!IsReady)
                return;

            Device.SetVertexBuffer(vertexBuffer);
            Device.Indices = indexBuffer;

            internalEffect.View = view;
            internalEffect.Projection = projection;
            internalEffect.World = world;

            internalEffect.CurrentTechnique.Passes[0].Apply();

            Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, verticesCount);

        }

        public void expandWorld(Direction direction)
        {
            switch (direction)
            {
                case Direction.Forward:

                    GlobalTranslation += new Vector3(0, 0, XChunk.Depth);
                    Vector3 localVector = Vector3.Zero;
                    for (int x = 0; x < SIZE; x++)
                    {
                        localVector.X = x * XChunk.Width;
                        localVector.Z = (SIZE - 1) * XChunk.Depth;

                        float[] heightMap = simplexNoise.GetNoiseMap2D((int)localVector.X + (int)GlobalTranslation.X, (int)localVector.Z + (int)GlobalTranslation.Z, XChunk.Width, XChunk.Depth);

                        Chunks[x + 0 * SIZE].ReGenerate(localVector, GlobalTranslation);
                        Chunks[x + 0 * SIZE].HeightMap = heightMap;

                        XChunk currentChunk = Chunks[x + 0 * SIZE];
                        for (int z = 0; z < SIZE - 1; z++)
                        {
                            XChunk toChange = Chunks[x + (z + 1) * SIZE];
                            toChange.LocalPosition -= new Vector3(0, 0, XChunk.Depth);
                            Chunks[x + (z + 1) * SIZE] = currentChunk;
                            Chunks[x + z * SIZE] = toChange;
                        }
                    }

                    for (int i = 0; i < SIZE; i++)
                    {
                        Chunks[i + (SIZE - 1) * SIZE].Generate();
                    }

                    XChunk.Flush(); 



                    break;
            }
        }
    }
}