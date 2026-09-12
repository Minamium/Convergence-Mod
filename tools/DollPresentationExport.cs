using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Asset finishing only: isolate existing authored cels, register sockets, resize,
// quantize and pack. This tool does not paint character poses or add animation.
public static class DollPresentationExport
{
    static Rectangle Bounds(Bitmap source, Rectangle cell, bool green)
    {
        int left=cell.Right, right=cell.Left, top=cell.Bottom, bottom=cell.Top;
        for(int y=cell.Top;y<cell.Bottom;y++) for(int x=cell.Left;x<cell.Right;x++)
        {
            Color c=source.GetPixel(x,y);
            if(green ? DollFrameExport.Empty(c) : c.A<2) continue;
            left=Math.Min(left,x); right=Math.Max(right,x);
            top=Math.Min(top,y); bottom=Math.Max(bottom,y);
        }
        if(right<=left||bottom<=top) throw new InvalidDataException("Missing authored artwork.");
        return Rectangle.FromLTRB(left,top,right+1,bottom+1);
    }

    static Color CleanBroom(Color c)
        => DollFrameExport.Quantize(Color.FromArgb(255,c.R,Math.Min(c.G,Math.Max(c.R,c.B)),c.B));

    public static void ExportBroom(string input, string output, string previewDirectory)
    {
        using var source=new Bitmap(input);
        if(source.Width%4!=0||source.Height%4!=0) throw new InvalidDataException("Broom requires an equal 4x4 source grid.");
        var boxes=new Rectangle[16]; var feet=new float[16];
        float maxHeight=0, leftReach=0, rightReach=0;
        for(int i=0;i<16;i++)
        {
            Rectangle cell=new(i%4*source.Width/4,i/4*source.Height/4,source.Width/4,source.Height/4);
            Rectangle box=Bounds(source,cell,true); boxes[i]=box;
            long sum=0; int count=0;
            for(int y=box.Bottom-6;y<box.Bottom;y++) for(int x=box.Left;x<box.Right;x++)
                if(!DollFrameExport.Empty(source.GetPixel(x,y))) {sum+=x;count++;}
            if(count==0) throw new InvalidDataException("Missing feet in broom cel "+i);
            feet[i]=sum/(float)count;
            maxHeight=Math.Max(maxHeight,box.Height);
            leftReach=Math.Max(leftReach,feet[i]-box.Left);
            rightReach=Math.Max(rightReach,box.Right-feet[i]);
        }
        float scale=Math.Min(48f/maxHeight,Math.Min(46f/leftReach,45f/rightReach));
        using var atlas=new Bitmap(96,80*16,PixelFormat.Format32bppArgb);
        for(int frame=0;frame<16;frame++)
        {
            Rectangle box=boxes[frame]; int count=0;
            for(int y=0;y<80;y++) for(int x=0;x<96;x++)
            {
                int sx=(int)MathF.Round(feet[frame]+(x-48)/scale);
                int sy=(int)MathF.Round(box.Bottom-1+(y-68)/scale);
                if(!box.Contains(sx,sy)) continue;
                Color c=source.GetPixel(sx,sy); if(DollFrameExport.Empty(c)) continue;
                if(x==0||y==0||x==95||y==79) throw new InvalidDataException("Clipped broom cel "+frame);
                atlas.SetPixel(x,frame*80+y,CleanBroom(c)); count++;
            }
            if(count<400) throw new InvalidDataException("Undersized broom cel "+frame);
            Console.WriteLine($"broom {frame}: source={box}, foot=({feet[frame]:F2},{box.Bottom-1}), scale={scale:F6}, pixels={count}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        atlas.Save(output,ImageFormat.Png);
        foreach(int zoom in new[]{1,2,4}) Contact(atlas,96,80,1,16,4,zoom,Path.Combine(previewDirectory,$"broom-cels-{zoom}x.png"));
    }

    public static void ExportBox(string input, string output, string previewDirectory)
    {
        using var source=new Bitmap(input);
        if(source.GetPixel(0,0).A!=0) throw new InvalidDataException("Box source must have genuine transparency.");
        Rectangle bounds=Bounds(source,new Rectangle(0,0,source.Width,source.Height),false);
        float scale=Math.Min(44f/bounds.Width,34f/bounds.Height);
        using var item=new Bitmap(48,40,PixelFormat.Format32bppArgb);
        int count=0;
        for(int y=0;y<40;y++) for(int x=0;x<48;x++)
        {
            int sx=(int)MathF.Round(bounds.Left+(bounds.Width-1)*.5f+(x-23.5f)/scale);
            int sy=(int)MathF.Round(bounds.Top+(bounds.Height-1)*.5f+(y-19.5f)/scale);
            if(!bounds.Contains(sx,sy)) continue;
            Color c=source.GetPixel(sx,sy); if(c.A==0) continue;
            if(x==0||x==47||y==0||y==39) throw new InvalidDataException("Clipped treasure box.");
            item.SetPixel(x,y,c); count++;
        }
        Rectangle result=Bounds(item,new Rectangle(0,0,48,40),false);
        if(count<500||result.Width>44||result.Height>34) throw new InvalidDataException("Invalid treasure-box export bounds.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        item.Save(output,ImageFormat.Png);
        foreach(int zoom in new[]{1,2,4}) Contact(item,48,40,1,1,1,zoom,Path.Combine(previewDirectory,$"treasure-box-{zoom}x.png"));
        Console.WriteLine($"box: source={bounds}, content={result}, pixels={count}, real alpha retained");
    }

    static List<Rectangle> AlphaIslands(Bitmap source)
    {
        int width=source.Width, height=source.Height;
        var opaque=new bool[width*height]; var visited=new bool[opaque.Length];
        for(int y=0;y<height;y++) for(int x=0;x<width;x++) opaque[y*width+x]=source.GetPixel(x,y).A>=100;
        var queue=new Queue<int>(); var found=new List<Rectangle>();
        for(int point=0;point<opaque.Length;point++)
        {
            if(!opaque[point]||visited[point]) continue;
            queue.Enqueue(point); visited[point]=true;
            int left=width, right=0, top=height, bottom=0, count=0;
            while(queue.Count>0)
            {
                int p=queue.Dequeue(),x=p%width,y=p/width; count++;
                left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
                Visit(x-1,y);Visit(x+1,y);Visit(x,y-1);Visit(x,y+1);
            }
            if(count>1000) found.Add(Rectangle.FromLTRB(left,top,right+1,bottom+1));
        }
        void Visit(int x,int y)
        {
            if(x<0||x>=width||y<0||y>=height) return;
            int p=y*width+x;if(!opaque[p]||visited[p]) return;
            visited[p]=true;queue.Enqueue(p);
        }
        if(found.Count!=16) throw new InvalidDataException("Expected 16 authored restraint islands, got "+found.Count);
        found.Sort((a,b)=>a.Top.CompareTo(b.Top));
        var ordered=new List<Rectangle>();
        for(int row=0;row<4;row++)
        {
            var group=found.GetRange(row*4,4);
            group.Sort((a,b)=>a.Left.CompareTo(b.Left)); ordered.AddRange(group);
        }
        return ordered;
    }

    // The thin brass equator is an authored registration landmark, not generated
    // art. Find it near the recorded row-specific socket center and preserve it.
    static PointF OrbCenter(Bitmap source, Rectangle box, int expectedY, out float radius)
    {
        int hintX=(box.Left+box.Right)/2, bestLength=0, bestLeft=hintX-32, bestRight=hintX+32, bestY=expectedY;
        for(int y=expectedY-5;y<=expectedY+5;y++)
        {
            if(!Gold(source.GetPixel(hintX,y))) continue;
            int left=hintX,right=hintX;
            while(left>hintX-48&&Gold(source.GetPixel(left-1,y))) left--;
            while(right<hintX+48&&Gold(source.GetPixel(right+1,y))) right++;
            int length=right-left+1;
            if(length>bestLength) {bestLength=length;bestLeft=left;bestRight=right;bestY=y;}
        }
        if(bestLength<40) throw new InvalidDataException("Cannot identify the authored socket equator.");
        // The equator's shine can fade on one side, shortening its visible gold
        // run. Use the continuous dark lower hemisphere for the horizontal center
        // and radius so phase-to-phase lighting never shifts the sphere socket.
        var centers=new List<float>(); var radii=new List<float>();
        for(int y=bestY+4;y<=bestY+24;y+=4)
        {
            if(!Dark(source.GetPixel(hintX,y))) continue;
            int left=hintX,right=hintX;
            while(left>hintX-52&&Dark(source.GetPixel(left-1,y))) left--;
            while(right<hintX+52&&Dark(source.GetPixel(right+1,y))) right++;
            float half=(right-left)*.5f,dy=y-bestY;
            if(half<10||half>47) continue;
            centers.Add((left+right)*.5f);
            radii.Add(MathF.Sqrt(half*half+dy*dy)+2f);
        }
        if(centers.Count<4) throw new InvalidDataException("Cannot identify the authored sphere rim.");
        centers.Sort();radii.Sort();
        radius=radii[radii.Count/2];
        return new PointF(centers[centers.Count/2],bestY);
    }
    static bool Gold(Color c) => c.A>=100&&c.R>65&&c.R>c.G*1.07f&&c.G>c.B*1.15f&&c.R-c.B>15;
    static bool Dark(Color c) => c.A>=100&&c.R<=90&&c.G<=90&&c.B<=95&&c.R-c.B<=32;

    public static void ExportRestraint(string input, string output, string previewDirectory)
    {
        using var source=new Bitmap(input);
        if(source.GetPixel(0,0).A!=0) throw new InvalidDataException("Restraint source must have genuine transparency.");
        var boxes=AlphaIslands(source); var centers=new PointF[16]; var radii=new float[16];
        int[] rowY={174,544,922,1299}; float scale=.75f;
        for(int i=0;i<16;i++)
        {
            centers[i]=OrbCenter(source,boxes[i],rowY[i/4],out radii[i]);
            float reachTop=centers[i].Y-boxes[i].Top;
            scale=Math.Min(scale,134f/reachTop);
            scale=Math.Min(scale,116f/Math.Max(centers[i].X-boxes[i].Left,boxes[i].Right-centers[i].X));
            scale=Math.Min(scale,209f/(boxes[i].Bottom-centers[i].Y));
        }
        using var atlas=new Bitmap(240*4,352*4,PixelFormat.Format32bppArgb);
        for(int frame=0;frame<16;frame++)
        {
            Rectangle box=boxes[frame]; PointF center=centers[frame]; int count=0;
            for(int y=0;y<352;y++) for(int x=0;x<240;x++)
            {
                int sx=(int)MathF.Round(center.X+(x-120)/scale),sy=(int)MathF.Round(center.Y+(y-138)/scale);
                if(!box.Contains(sx,sy)) continue;
                Color c=source.GetPixel(sx,sy);if(c.A==0) continue;
                if(x==0||x==239||y==0||y==351) throw new InvalidDataException("Clipped restraint cel "+frame);
                atlas.SetPixel(frame%4*240+x,frame/4*352+y,c);count++;
            }
            if(count<12000) throw new InvalidDataException("Undersized restraint cel "+frame);
            Console.WriteLine($"restraint {frame}: source={box}, sphere_source=({center.X:F2},{center.Y:F2}), socket=(120,138), native_radius~{radii[frame]*scale:F2}, scale={scale:F6}, pixels={count}");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        atlas.Save(output,ImageFormat.Png);
        Contact(atlas,240,352,4,16,4,1,Path.Combine(previewDirectory,"restraint-cels-1x.png"));
        using var first=atlas.Clone(new Rectangle(0,0,240,352),PixelFormat.Format32bppArgb);
        Contact(first,240,352,1,1,1,2,Path.Combine(previewDirectory,"restraint-socket-2x.png"));
    }

    static void Contact(Bitmap atlas,int width,int height,int sourceColumns,int count,int columns,int zoom,string output)
    {
        int rows=(count+columns-1)/columns;
        using var preview=new Bitmap(width*columns*zoom,height*rows*zoom,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(preview))
        {
            g.Clear(Color.FromArgb(43,44,49));
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for(int i=0;i<count;i++)
                g.DrawImage(atlas,new Rectangle(i%columns*width*zoom,i/columns*height*zoom,width*zoom,height*zoom),
                    new Rectangle(i%sourceColumns*width,i/sourceColumns*height,width,height),GraphicsUnit.Pixel);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        preview.Save(output,ImageFormat.Png);
    }
}
