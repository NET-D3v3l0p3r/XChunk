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

                buildBuffers();
                sw.Stop();
                Console.WriteLine("DONE IN " + sw.Elapsed.TotalSeconds + " SECONDS!");

                IsReady = true;

            });
        }

        private void buildBuffers()
        {
            vertexBuffer = new VertexBuffer(Device, typeof(VertexPositionTexture), XChunk.TILE_SIZE * 4, BufferUsage.None);

            int[] indices = new int[XChunk.TILE_SIZE * 6];

            int offset = 0;
            int vertexOffset = 0;


            for (int i = 0; i < XChunk.TILE_SIZE; i++)
            {

                for (int j = 0; j < 6; j++)
                {
                    indices[j + offset] = Definition.IndicesDefinitionSprite[j] + vertexOffset;
                }
                offset += 6;
                vertexOffset += 4;
            }
            offset = 0;

            indexBuffer = new IndexBuffer(Device, IndexElementSize.ThirtyTwoBits, XChunk.TILE_SIZE * 6, BufferUsage.WriteOnly);
            indexBuffer.SetData<int>(indices);

            VertexPositionTexture[] tempPuffer = new VertexPositionTexture[XChunk.TILE_SIZE * 4];
            for (int i = 0; i < Chunks.Length; i++)
            {
                for (int j = 0; j < Chunks[i].ChunkVertices.Length; j++)
                {
                    tempPuffer[j + offset] = Chunks[i].ChunkVertices[j];
                }
                offset += Chunks[i].ChunkVertices.Length;
            }

            vertexBuffer.SetData<VertexPositionTexture>(tempPuffer);

            verticesCount = XChunk.TILE_SIZE * 2;

            tempPuffer = new VertexPositionTexture[0];
            indices = new int[0];

            XChunk.IS_OBSOLETE = false;
            GC.Collect();
        }

        public void Update(GameTime gTime)
        {
            if (XChunk.IS_OBSOLETE && IsReady)
                Task.Run(() => { buildBuffers(); });
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


        #region Obsolete
        //private void __removeSequenceZ(int sequence)
        //{
        //    VertexPositionTexture[] verts = new VertexPositionTexture[sequenceSizesHorizontal[sequence + 1]];

        //    int offSet = 0;
        //    for (int i = 0; i <= sequence; i++)
        //    {
        //        offSet += sequenceSizesHorizontal[i];
        //    }

        //    verticesBuffer.SetData<VertexPositionTexture>(VertexPositionTexture.VertexDeclaration.VertexStride * offSet, verts, 0, verts.Length, VertexPositionTexture.VertexDeclaration.VertexStride);

        //    verts = new VertexPositionTexture[0];
        //    GC.Collect();
        //}

        //public void TEST_X(int sequence)
        //{

        //}
        //public void Expand(Direction direction)
        //{
        //    switch (direction)
        //    {
        //        case Direction.Forward:
        //            GlobalTranslation += new Vector3(0, 0, XChunk.Depth);

        //            for (int x = 0; x < SIZE; x++)
        //            {
        //                Vector3 localVector = new Vector3(x * XChunk.Width, 0, (SIZE - 1) * XChunk.Depth);

        //                Chunks[x + 0 * SIZE].ReInitialize(localVector, GlobalTranslation, simplexNoise.GetNoiseMap2D((int)localVector.X + (int)GlobalTranslation.X, (int)localVector.Z + (int)GlobalTranslation.Z, XChunk.Width, XChunk.Depth));

        //                XChunk currentChunk = Chunks[x + 0 * SIZE];
        //                for (int z = 0; z < SIZE - 1; z++)
        //                {
        //                    XChunk toChange = Chunks[x + (z + 1) * SIZE];
        //                    toChange.LocalPosition -= new Vector3(0, 0, XChunk.Depth);
        //                    Chunks[x + (z + 1) * SIZE] = currentChunk;
        //                    Chunks[x + z * SIZE] = toChange;
        //                }
        //            }

        //            Task.Run(() =>
        //            {

        //                for (int x = 0; x < SIZE; x++)
        //                    Chunks[x + (SIZE - 1) * SIZE].Generate();

        //                __internalExpand(direction);

        //                zAxisCounter++;
        //                if (zAxisCounter >= SIZE)
        //                    zAxisCounter = 0;

        //                GC.Collect();
        //            });


        //            break;
        //        case Direction.Backward:
        //            break;
        //        case Direction.Left:
        //            break;
        //        case Direction.Right:
        //            break;
        //    }
        //}
        //public VertexPositionTexture[] GetData(Vector3 chunk)
        //{
        //    XChunk index = Chunks[(int)chunk.X + (int)chunk.Y * SIZE];

        //    VertexPositionTexture[] buffer = new VertexPositionTexture[index.VertexBufferCount];

        //    if (Optimization == Technique.Optimization_Memory)
        //        verticesBuffer.GetData<VertexPositionTexture>(VertexPositionTexture.VertexDeclaration.VertexStride * index.VertexBufferIndex, buffer, 0, index.VertexBufferCount, VertexPositionTexture.VertexDeclaration.VertexStride);
        //    else
        //        Array.Copy(vertices, index.VertexBufferIndex, buffer, 0, index.VertexBufferCount);

        //    return buffer;
        //}
        //private void __internalStart()
        //{

        //    int offset = 0;
        //    int vertexOffset = 0;

        //    vertices = new VertexPositionTexture[flatBuffer.Count * 4];
        //    indices = new int[flatBuffer.Count * 6];

        //    for (int i = 0; i < flatBuffer.Count; i++)
        //    {

        //        for (int j = 0; j < 4; j++)
        //        {
        //            if (flatBuffer[i] != null)
        //                vertices[j + offset] = flatBuffer[i].Vertices[j];
        //        }
        //        offset += 4;
        //    }
        //    offset = 0;
        //    for (int i = 0; i < flatBuffer.Count; i++)
        //    {

        //        for (int j = 0; j < 6; j++)
        //        {
        //            if (flatBuffer[i] != null)
        //                indices[j + offset] = flatBuffer[i].Indices[j] + vertexOffset;
        //        }
        //        offset += 6;
        //        vertexOffset += 4;
        //    }


        //    verticesBuffer = new VertexBuffer(Device, typeof(VertexPositionTexture), vertices.Length, BufferUsage.None);
        //    indicesBuffer = new IndexBuffer(Device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);

        //    verticesCount = flatBuffer.Count * 2;

        //    verticesBuffer.SetData(vertices);
        //    indicesBuffer.SetData(indices);

        //    flatBuffer.Clear();
        //    if (Optimization == Technique.Optimization_Memory)
        //        vertices = new VertexPositionTexture[0];
        //    indices = new int[0];

        //}
        //private void __internalExpand(Direction direction)
        //{
        //    #region "Get vertices"
        //    int offset = 0;
        //    vertices = new VertexPositionTexture[flatBuffer.Count * 4];

        //    for (int i = 0; i < flatBuffer.Count; i++)
        //    {

        //        for (int j = 0; j < 4; j++)
        //        {
        //            if (flatBuffer[i] != null)
        //                vertices[j + offset] = flatBuffer[i].Vertices[j];
        //        }
        //        offset += 4;
        //    }
        //    #endregion

        //    switch (direction)
        //    {
        //        case Direction.Forward:
        //            __removeSequenceZ(zAxisCounter);
        //            int offSet = 0;
        //            for (int i = 0; i <= zAxisCounter; i++)
        //                offSet += sequenceSizesHorizontal[i];
        //            if (vertices.Length < sequenceSizesHorizontal[zAxisCounter + 1])
        //                verticesBuffer.SetData<VertexPositionTexture>(VertexPositionTexture.VertexDeclaration.VertexStride * offSet, vertices, 0, vertices.Length, VertexPositionTexture.VertexDeclaration.VertexStride);
        //            else Console.WriteLine("ERROR!");
        //            break;

        //        case Direction.Backward:
        //            break;
        //    }

        //    flatBuffer.Clear();
        //    vertices = new VertexPositionTexture[0];

        //}
        #endregion
    }
}