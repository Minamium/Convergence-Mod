#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace Convergence.Client.Encounters.FirstSeverance;

// Small authored-texture mesh, using the existing RitualSurfacePass rendering
// pattern. Separate ownership: never resets weapon geometry or changes a target.
internal sealed class FirstSeveranceDollSurface
{
    private readonly VertexPositionColorTexture[] vertices=new VertexPositionColorTexture[12*6];
    private BasicEffect? effect;

    internal void Draw(SpriteBatch batch,Texture2D texture,Rectangle source,DollSprite part,
        Vector2 position,Vector2 scale,Color tint,float seconds,bool reduced,float strength)
    {
        int rows=reduced?6:12,used=0;
        Vector2 previousLeft=Point(0,0),previousRight=Point(source.Width,0);
        for(int row=1;row<=rows;row++)
        {
            float y=source.Height*row/(float)rows,priorY=source.Height*(row-1)/(float)rows;
            Vector2 left=Point(0,y),right=Point(source.Width,y);
            Add(previousLeft,0,priorY); Add(previousRight,source.Width,priorY); Add(left,0,y);
            Add(left,0,y); Add(previousRight,source.Width,priorY); Add(right,source.Width,y);
            previousLeft=left; previousRight=right;
        }
        Vector2 Point(float x,float y)
        {
            float bend=FirstSeveranceDollPose.FlexOffset(part,y,seconds,reduced)*strength;
            return position-Main.screenPosition+(new Vector2(x-part.Pivot.X+bend,y-part.Pivot.Y)*scale).RotatedBy(part.Rotation);
        }
        void Add(Vector2 point,float x,float y) => vertices[used++]=new(new Vector3(point,0),tint,
            new Vector2((source.X+x)/texture.Width,(source.Y+y)/texture.Height));

        // Flush the preceding opaque parts, submit this one surface, then return
        // to the caller's exact world-space pass. All layer order is preserved.
        batch.End();
        var device=Main.instance.GraphicsDevice;
        var blend=device.BlendState; var depth=device.DepthStencilState;
        var raster=device.RasterizerState; var sampler=device.SamplerStates[0];
        try
        {
            effect??=new BasicEffect(device) { TextureEnabled=true,VertexColorEnabled=true,LightingEnabled=false };
            effect.Texture=texture; effect.World=Matrix.Identity; effect.View=Main.GameViewMatrix.TransformationMatrix;
            effect.Projection=Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,-1,1);
            device.BlendState=BlendState.AlphaBlend; device.DepthStencilState=DepthStencilState.None;
            device.RasterizerState=RasterizerState.CullNone; device.SamplerStates[0]=SamplerState.LinearClamp;
            foreach(var pass in effect.CurrentTechnique.Passes)
            { pass.Apply(); device.DrawUserPrimitives(PrimitiveType.TriangleList,vertices,0,used/3); }
        }
        finally
        {
            device.BlendState=blend; device.DepthStencilState=depth;
            device.RasterizerState=raster; device.SamplerStates[0]=sampler;
            batch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
                DepthStencilState.None,Main.Rasterizer,null,Main.GameViewMatrix.TransformationMatrix);
        }
    }

    internal void Unload()
    {
        var old=effect; effect=null;
        if(old is not null) Main.QueueMainThreadAction(old.Dispose);
    }
}
