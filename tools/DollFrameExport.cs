using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Mechanical matte extraction, registration and packing, not an art generator.
// The eight distinct poses come from the preserved image-generation originals.
public static class DollFrameExport
{
    static bool Empty(Color c) => c.A<100 || c.G>75&&c.G>c.R*1.22+18&&c.G>c.B*1.22+18;
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
        if(islands.Count!=8) throw new InvalidDataException("Expected eight separate authored poses, got "+islands.Count);
        islands.Sort((a,b)=> (a.Bounds.Top<height/2?0:1)!=(b.Bounds.Top<height/2?0:1)
            ?a.Bounds.Top.CompareTo(b.Bounds.Top):a.Bounds.Left.CompareTo(b.Bounds.Left));
        int cw=hand?224:240,ch=352;
        float sx=(hand?150.5f:224)/islands[0].Bounds.Width,sy=327.5f/islands[0].Bounds.Height;
        using var atlas=new Bitmap(cw*4,ch*2);
        for(int frame=0;frame<8;frame++)
        {
            var box=islands[frame].Bounds;
            // Same top socket/spire anchor, common scale across all eight poses:
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
