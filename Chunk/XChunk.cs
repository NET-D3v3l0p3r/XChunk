using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using XChunk.Chunk.Component;
using XChunk.Extensions;
using System.IO;
using System.Threading.Tasks;

namespace XChunk.Chunk
{
    public class XChunk
    {
        private static object SYNC_LOCK = new object();
        private static int CHUNK_ID;

        public static bool IS_OBSOLETE { get; set; }
        public static int TILE_SIZE { get; set; }

        public int ChunkId { get; set; }

        public static int Width { get; set; }
        public static int Depth { get; set; }
        public static int Height { get; set; }

        public ChunkRenderer ChunkRenderer { get; private set; }

        public Vector3 LocalPosition { get; set; }
        public Vector3 GlobalPosition { get; set; }

        public float[] HeightMap { get; set; }

        public List<BoundingBox> BoundingBoxes = new List<BoundingBox>();
        public XChunk[] ChunkNeighbours;

        public int ChunkOffset { get; private set; }
        public bool ReCreate;

        public BoundingBox ChunkBoundingBox { get; private set; }
        public VertexPositionTexture[] ChunkVertices { get; private set; }

        private List<ITile> flatBuffer = new List<ITile>();
        private bool isInitialized;

        public XChunk(ChunkRenderer renderer, Vector3 localTranslation, Vector3 globalTranslation)
        {
            ChunkRenderer = renderer;

            LocalPosition = localTranslation;
            GlobalPosition = globalTranslation;

            ChunkBoundingBox = new BoundingBox(GlobalPosition + LocalPosition - new Vector3(0, 256, 0), GlobalPosition + LocalPosition + new Vector3(16, 512, 16));
            ChunkNeighbours = new XChunk[4];

            ReCreate = true;

        }

        public void ReGenerate(Vector3 localTranslation, Vector3 globalTranslation)
        {
            LocalPosition = localTranslation;
            GlobalPosition = globalTranslation;

            ChunkBoundingBox = new BoundingBox(GlobalPosition + LocalPosition - new Vector3(0, 256, 0), GlobalPosition + LocalPosition + new Vector3(16, 512, 16));

            ChunkNeighbours[0] = null;
            ChunkNeighbours[1] = null;
            ChunkNeighbours[2] = null;
            ChunkNeighbours[3] = null;

            TILE_SIZE -= ChunkVertices.Length / 4;

            ChunkVertices = new VertexPositionTexture[0];
            BoundingBoxes.Clear();

            ReCreate = true;

        }

        public void Generate()
        {
            if (!isInitialized)
                ChunkId = CHUNK_ID++;

            isInitialized = true;

            int x = (int)LocalPosition.X / Width;
            int yx = (int)LocalPosition.Z / Depth;

            if (yx + 1 < ChunkRenderer.SIZE)
                ChunkNeighbours[0] = ChunkRenderer.Chunks[x + (yx + 1) * ChunkRenderer.SIZE];
            if (x + 1 < ChunkRenderer.SIZE)
                ChunkNeighbours[1] = ChunkRenderer.Chunks[(x + 1) + yx * ChunkRenderer.SIZE];
            if (yx - 1 >= 0)
                ChunkNeighbours[2] = ChunkRenderer.Chunks[x + (yx - 1) * ChunkRenderer.SIZE];
            if (x - 1 >= 0)
                ChunkNeighbours[3] = ChunkRenderer.Chunks[(x - 1) + yx * ChunkRenderer.SIZE];


            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Depth; j++)
                {
                    //0 - NORTH
                    //1 - EAST
                    //2 - SOUTH
                    //3 - WEST


                    int left = 0;
                    int up = 0;
                    int right = 0;
                    int down = 0;

                    if (i - 1 >= 0)
                        left = (int)HeightMap[(i - 1) + j * Width];
                    else if (ChunkNeighbours[3] == null)
                        left = Height + 1;
                    else
                        left = (int)ChunkNeighbours[3].HeightMap[(Width - 1) + j * Width];

                    if (j - 1 >= 0)
                        up = (int)HeightMap[i + (j - 1) * Width];
                    else if (ChunkNeighbours[2] == null)
                        up = Height + 1;
                    else up = (int)ChunkNeighbours[2].HeightMap[i + (Depth - 1) * Width];

                    if (i + 1 < Width)
                        right = (int)HeightMap[(i + 1) + j * Width];
                    else if (ChunkNeighbours[1] == null)
                        right = Height + 1;
                    else right = (int)ChunkNeighbours[1].HeightMap[0 + j * Width];

                    if (j + 1 < Depth)
                        down = (int)HeightMap[i + (j + 1) * Width];
                    else if (ChunkNeighbours[0] == null)
                        down = Height + 1;
                    else down = (int)ChunkNeighbours[0].HeightMap[i + 0 * Width];


                    addFlat(i, (int)HeightMap[i + j * Width], j, ChunkRenderer.TextureAtlas["Top_Grass"], LocalPosition + GlobalPosition);
                    //ADD SIDES
                    for (int y = 0; y <= HeightMap[i + j * Width]; y++)
                    {
                        if (y > left)
                            addUpZ(i, y - 1, j, ChunkRenderer.TextureAtlas["Side_Dirt"], LocalPosition + GlobalPosition);

                        if (y > up)
                            addUpX(i, y - 1, j, ChunkRenderer.TextureAtlas["Side_Dirt"], LocalPosition + GlobalPosition);

                        if (y > right)
                            addUpZ(i + 1, y - 1, j, ChunkRenderer.TextureAtlas["Side_Dirt"], LocalPosition + GlobalPosition);

                        if (y > down)
                            addUpX(i, y - 1, j + 1, ChunkRenderer.TextureAtlas["Side_Dirt"], LocalPosition + GlobalPosition);

                    }
                }
            }
            generateArray();
        }

        public static void Flush()
        {
            IS_OBSOLETE = true;
        }

        private void addFlat(int x, int y, int z, Vector2 texCoord, Vector3 translation)
        {
            flatBuffer.Add(new TileFlat(translation, x, y, z, texCoord));
            BoundingBoxes.Add(new BoundingBox(translation + new Vector3(x, y, z), translation + new Vector3(x, y, z) + new Vector3(1, -1, 1)));
        }
        private void addUpX(int x, int y, int z, Vector2 texCoord, Vector3 translation)
        {
            flatBuffer.Add(new TileUpX(translation, x, y, z, texCoord));
        }
        private void addUpZ(int x, int y, int z, Vector2 texCoord, Vector3 translation)
        {
            flatBuffer.Add(new TileUpZ(translation, x, y, z, texCoord));
        }

        private void generateArray()
        {
            TILE_SIZE += flatBuffer.Count;


            ChunkVertices = new VertexPositionTexture[flatBuffer.Count * 4];

            int offset = 0;
            for (int i = 0; i < flatBuffer.Count; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (flatBuffer[i] != null)
                        ChunkVertices[j + offset] = flatBuffer[i].Vertices[j];
                }
                offset += 4;
            }

            flatBuffer.Clear();

        }

        public void CalculateOffset()
        {
            ChunkOffset = 0;
            for (int i = 0; i < ChunkId; i++)
                ChunkOffset += ChunkRenderer.Chunks[i].ChunkVertices.Length;
        }

        //public void WriteToHDD()
        //{
        //    Task.Run(() =>
        //    {
        //        lock (SYNC_LOCK)
        //        {
        //            string path = @"data\chunks\" + ChunkId + ".dat";
        //            File.WriteAllBytes(path, BoundingBoxes.SerializeToByteArray());
        //            BoundingBoxes.Clear();
        //            GC.Collect();
        //        }
        //    });

        //}
        //public void ReadFromHDD()
        //{
        //    Task.Run(() =>
        //    {
        //        string path = @"data\chunks\" + ChunkId + ".dat";
        //        object raw = File.ReadAllBytes(path).DeserializeToDynamicType();

        //        BoundingBoxes = (List<BoundingBox>)raw;
        //        File.Delete(path);
        //        GC.Collect();
        //    });
        //}

    }
}
