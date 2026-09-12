using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Mechanical registration/quantization of authored cels. No synthesized poses.
public static class DollCompanionExport
{
    public static void Export(string idlePath,string walkPath,string castPath,string iconPath,string output,string preview)
    {
        using var idle=new Bitmap(idlePath);
        using var walk=new Bitmap(walkPath);
        using var cast=new Bitmap(castPath);
        using var atlas=new Bitmap(48,64*36);
        for(int i=0;i<12;i++) for(int y=0;y<52;y++) for(int x=0;x<32;x++)
            atlas.SetPixel(x+8,i*64+y+11,idle.GetPixel(x,i*52+y));
        Pack(walk,atlas,12); Pack(cast,atlas,24);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        atlas.Save(output,ImageFormat.Png);
        using var board=new Bitmap(48*6*4,64*6*4);
        using(var g=Graphics.FromImage(board)) {
            g.Clear(Color.FromArgb(42,44,49));
            g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for(int i=0;i<36;i++) g.DrawImage(atlas,new Rectangle(i%6*192,i/6*256,192,256),new Rectangle(0,i*64,48,64),GraphicsUnit.Pixel);
        }
        board.Save(preview,ImageFormat.Png);
        using var icon=new Bitmap(iconPath);
        var bounds=Bounds(icon,new Rectangle(0,0,icon.Width,icon.Height));
        using var item=new Bitmap(44,44);
        float scale=40f/Math.Max(bounds.Width,bounds.Height);
        for(int y=0;y<44;y++) for(int x=0;x<44;x++) {
            int sx=(int)(bounds.Left+bounds.Width*.5f+(x-21.5f)/scale),sy=(int)(bounds.Top+bounds.Height*.5f+(y-21.5f)/scale);
            if(!bounds.Contains(sx,sy)) continue;
            var c=icon.GetPixel(sx,sy); if(DollFrameExport.Empty(c)) continue;
            item.SetPixel(x,y,Color.FromArgb(c.A,c.R,c.G,c.B));
        }
        item.Save(Path.Combine(Path.GetDirectoryName(output),"../../Items/RitualArmaments/DollCovenant.png"),ImageFormat.Png);
        Console.WriteLine("Companion: 36 cels (12 existing idle, 8 walk, 4 float, 12 cast), 48x64 each; item 44x44.");
    }
    static Rectangle Bounds(Bitmap src,Rectangle cell)
    {
        int l=cell.Right,r=cell.Left,t=cell.Bottom,b=cell.Top;
        for(int y=cell.Top;y<cell.Bottom;y++) for(int x=cell.Left;x<cell.Right;x++) {
            if(DollFrameExport.Empty(src.GetPixel(x,y))) continue;
            l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);
        }
        if(r<=l||b<=t) throw new InvalidDataException("Empty authored cel.");
        return Rectangle.FromLTRB(l,t,r+1,b+1);
    }
    static void Pack(Bitmap src,Bitmap atlas,int offset)
    {
        var boxes=new Rectangle[12];var anchors=new float[12];float height=0,reach=0;
        for(int i=0;i<12;i++) {
            var box=Bounds(src,new Rectangle(i%4*src.Width/4,i/4*src.Height/3,src.Width/4,src.Height/3));
            boxes[i]=box;long sum=0;int n=0;
            for(int y=box.Bottom-6;y<box.Bottom;y++) for(int x=box.Left;x<box.Right;x++)
                if(!DollFrameExport.Empty(src.GetPixel(x,y))) {sum+=x;n++;}
            anchors[i]=sum/(float)n;
            height=Math.Max(height,box.Height);reach=Math.Max(reach,Math.Max(anchors[i]-box.Left,box.Right-anchors[i]));
        }
        float sx=Math.Min(22/reach,48/height),sy=48/height;
        for(int i=0;i<12;i++) {
            int count=0;
            for(int y=0;y<64;y++) for(int x=0;x<48;x++) {
                int px=(int)MathF.Round(anchors[i]+(x-24)/sx),py=(int)MathF.Round(boxes[i].Bottom-1+(y-60)/sy);
                if(!boxes[i].Contains(px,py)) continue;
                var c=src.GetPixel(px,py);if(DollFrameExport.Empty(c)) continue;
                if(x==0||x==47||y==0||y==63) throw new InvalidDataException("Clipped companion cel.");
                atlas.SetPixel(x,(offset+i)*64+y,DollFrameExport.Quantize(c));count++;
            }
            if(count<180) throw new InvalidDataException("Undersized companion cel.");
        }
    }
}
