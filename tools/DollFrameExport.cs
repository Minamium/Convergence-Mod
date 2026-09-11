using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Mechanical matte extraction, registration and packing, not an art generator.
// The distinct poses come from the preserved image-generation originals.
public static class DollFrameExport
{
    static bool Empty(Color c) => c.A<100 || c.G>75&&c.G>c.R*1.22+18&&c.G>c.B*1.22+18;
    static readonly int[] NpcPalette = {
        0x121017,0x1d1a22,0x29252f,0x38323d,0x49404a,0x5b505a,0x6b626b,0x817881,
        0x99929a,0xb1aab0,0xc9c4c9,0xe1dce0,0xf4edef,0x302a29,0x493a36,0x635048,
        0x80675b,0x9c8070,0xb69a85,0xd0b69e,0xe5cfb7,0xf4e3ce,0x756b6a,0x938481,
        0xb0a19b,0xc8b9af,0xdbcec2,0xede2d4,0xfcf4e6,0x444855,0x747b89,0xa9aeb6 };
    static Color Quantize(Color c)
    {
        int best=int.MaxValue,winner=0;
        foreach(int rgb in NpcPalette) {
            int r=c.R-(rgb>>16&255),g=c.G-(rgb>>8&255),b=c.B-(rgb&255),d=r*r+2*g*g+b*b;
            if(d<best) {best=d;winner=rgb;}
        }
        return Color.FromArgb(255,winner>>16&255,winner>>8&255,winner&255);
    }
    public static void ExportNpc(string input,string output)
    {
        using var source=new Bitmap(input);
        var boxes=new Rectangle[12]; var anchors=new float[12];
        float reach=0,maxHeight=0;
        for(int i=0;i<12;i++) {
            int left=source.Width,right=0,top=source.Height,bottom=0;
            for(int y=i/4*source.Height/3;y<(i/4+1)*source.Height/3;y++)
            for(int x=i%4*source.Width/4;x<(i%4+1)*source.Width/4;x++) {
                if(Empty(source.GetPixel(x,y))) continue;
                left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
            }
            if(right<=left||bottom<=top) throw new InvalidDataException("Missing NPC cel "+i);
            boxes[i]=Rectangle.FromLTRB(left,top,right+1,bottom+1);
            long sum=0;int count=0;
            for(int y=bottom-5;y<=bottom;y++) for(int x=left;x<=right;x++)
                if(!Empty(source.GetPixel(x,y))) {sum+=x;count++;}
            anchors[i]=sum/(float)count;
            reach=Math.Max(reach,Math.Max(anchors[i]-left,right-anchors[i]));
            maxHeight=Math.Max(maxHeight,boxes[i].Height);
        }
        float scale=Math.Min(14.5f/reach,48/maxHeight),scaleY=48/maxHeight;
        using var atlas=new Bitmap(32,52*12);
        using var preview=new Bitmap(32*4*6,52*4*2);
        for(int i=0;i<12;i++) for(int y=0;y<52;y++) for(int x=0;x<32;x++) {
            var box=boxes[i];
            int px=(int)MathF.Round(anchors[i]+(x-16)/scale),py=(int)MathF.Round(box.Bottom-1+(y-49)/scaleY);
            if(!box.Contains(px,py)) continue;
            var c=source.GetPixel(px,py);if(Empty(c)) continue;
            if(x==0||x==31||y==0||y==51) throw new InvalidDataException("Clipped NPC frame "+i);
            c=Quantize(c);atlas.SetPixel(x,i*52+y,c);
            for(int sy=0;sy<4;sy++) for(int sx=0;sx<4;sx++) preview.SetPixel(i%6*128+x*4+sx,i/6*208+y*4+sy,c);
        }
        atlas.Save(output,ImageFormat.Png);
        preview.Save(Path.Combine(Path.GetDirectoryName(input),"npc-poses-4x.png"),ImageFormat.Png);
        Console.WriteLine("NPC: 12 distinct cels, common scale="+scale+", foot anchor y49, 32-colour palette");
    }
    public static void Export(string input,string output,bool hand)
    {
        using var source=new Bitmap(input);
        int width=source.Width,height=source.Height;
        var solid=new bool[width*height];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) solid[y*width+x]=!Empty(source.GetPixel(x,y));
        var visited=new bool[solid.Length]; var queue=new Queue<int>();
        var islands=new List<(Rectangle Bounds,int Count)>();
        for(int i=0;i<solid.Length;i++)
        {
            if(!solid[i]||visited[i]) continue;
            queue.Enqueue(i); visited[i]=true;
            int left=width,right=0,top=height,bottom=0,count=0;
            while(queue.Count>0)
            {
                int p=queue.Dequeue(),x=p%width,y=p/width; count++;
                left=Math.Min(left,x); right=Math.Max(right,x); top=Math.Min(top,y); bottom=Math.Max(bottom,y);
                Visit(x-1,y); Visit(x+1,y); Visit(x,y-1); Visit(x,y+1);
            }
            if(count>1000) islands.Add((Rectangle.FromLTRB(left,top,right+1,bottom+1),count));
        }
        void Visit(int x,int y)
        {
            if(x<0||x>=width||y<0||y>=height) return;
            int p=y*width+x; if(!solid[p]||visited[p]) return;
            visited[p]=true; queue.Enqueue(p);
        }
        if(islands.Count!=16) throw new InvalidDataException("Expected sixteen separate authored poses, got "+islands.Count);
        islands.Sort((a,b)=> a.Bounds.Top/(height/4)!=b.Bounds.Top/(height/4)
            ?a.Bounds.Top.CompareTo(b.Bounds.Top):a.Bounds.Left.CompareTo(b.Bounds.Left));
        int cw=hand?224:240,ch=352;
        int maxWidth=0,maxHeight=0;
        foreach(var island in islands) {maxWidth=Math.Max(maxWidth,island.Bounds.Width);maxHeight=Math.Max(maxHeight,island.Bounds.Height);}
        float sx=(hand?192f:212)/maxWidth,sy=327.5f/maxHeight;
        using var atlas=new Bitmap(cw*4,ch*4);
        for(int frame=0;frame<16;frame++)
        {
            var box=islands[frame].Bounds;
            // Same top socket/spire anchor, common scale across all sixteen poses:
            // never normalize the curled hand back to the open hand's height.
            long sum=0; int samples=0;
            for(int y=box.Top;y<box.Top+10;y++) for(int x=box.Left;x<box.Right;x++)
                if(solid[y*width+x]) {sum+=x;samples++;}
            float anchor=sum/(float)samples;
            int count=0;
            for(int y=0;y<ch;y++) for(int x=0;x<cw;x++)
            {
                int px=(int)MathF.Round(anchor+(x-120)/sx),py=(int)MathF.Round(box.Top+(y-8)/sy);
                if(!box.Contains(px,py)) continue;
                var c=source.GetPixel(px,py); if(Empty(c)) continue;
                // Remove chroma spill at cutout edges without changing ivory.
                c=Color.FromArgb(255,c.R,Math.Min(c.G,Math.Max(c.R,c.B)),c.B);
                atlas.SetPixel(frame%4*cw+x,frame/4*ch+y,c); count++;
                if(x==0||y==0||x==cw-1||y==ch-1) throw new InvalidDataException("Clipped frame "+frame);
            }
            if(count<2000) throw new InvalidDataException("Empty/undersized frame "+frame);
            Console.WriteLine($"{(hand?"hand":"body")} {frame}: source={box}, anchor={anchor:F2}, pixels={count}");
        }
        atlas.Save(output,ImageFormat.Png);
    }
}
