using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XChunk.Chunk.Component
{
    public struct TileUpX : ITile
    {

        public VertexPositionTexture[] Vertices { get; set; }


        public TileUpX(Vector3 position, int x, int y, int z, Vector2 texCoord)
            : this()
        {
            Vertices = new VertexPositionTexture[4];
            texCoord = new Vector2(texCoord.X * ChunkRenderer.TEXTURE_RATIO_X, texCoord.Y * ChunkRenderer.TEXTURE_RATIO_Y);

            Vertices[0] = new VertexPositionTexture(position + new Vector3(x, y, z), new Vector2(0 + texCoord.X, ChunkRenderer.TEXTURE_RATIO_Y + texCoord.Y));
            Vertices[1] = new VertexPositionTexture(position + new Vector3(x + 1, y, z), new Vector2(ChunkRenderer.TEXTURE_RATIO_X + texCoord.X, ChunkRenderer.TEXTURE_RATIO_Y + texCoord.Y));
            Vertices[2] = new VertexPositionTexture(position + new Vector3(x + 1, y + 1, z),  new Vector2(ChunkRenderer.TEXTURE_RATIO_X + texCoord.X, 0 + texCoord.Y));
            Vertices[3] = new VertexPositionTexture(position + new Vector3(x, y + 1, z), new Vector2(0 + texCoord.X, 0 + texCoord.Y));

        }
    }
}
