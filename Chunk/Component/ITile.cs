using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
namespace XChunk.Chunk.Component
{
    public interface ITile
    {
        VertexPositionTexture[] Vertices { get; set; }
    }
}
