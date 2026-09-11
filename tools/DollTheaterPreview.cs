using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Convergence.Client.Encounters.FirstSeverance;

// Contact sheet from the actual game pose math. Not an in-game screenshot and
// not a replacement for lighting/zoom/multiplayer acceptance in tModLoader.
public static class DollTheaterPreview
{
    public static void Create(string assets, string output,bool includeMotion=true)
    {
        Validate(assets);
        using var atlas=new Bitmap(Path.Combine(assets,"DollRigAtlas.png"));
        using var harness=new Bitmap(Path.Combine(assets,"..","NullCantorRigAtlas.png"));
        using var bodyFrames=new Bitmap(Path.Combine(assets,"RestraintFrames.png"));
        using var handFrames=new Bitmap(Path.Combine(assets,"RemoteClawFrames.png"));
        using var shell=new Bitmap(Path.Combine(assets,"..","NullCantorShell.png"));
        using var npc=new Bitmap(Path.Combine(assets,"DollAttendant.png"));
        using var result=new Bitmap(1536,1024);
        using var g=Graphics.FromImage(result);
        using var font=new Font("Segoe UI",17);
        using var small=new Font("Segoe UI",12);
        g.Clear(Color.FromArgb(21,20,27));
        g.InterpolationMode=InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode=PixelOffsetMode.Half;
        using var pale=new SolidBrush(Color.FromArgb(218,209,197));
        using var quiet=new SolidBrush(Color.FromArgb(125,119,127));
        using var border=new Pen(Color.FromArgb(66,57,63));
        g.DrawString("FIRST SEVERANCE  /  THE UNFORTUNATE DOLL PLAY",font,pale,30,20);
        g.DrawString("Runtime textures + shared game pose. Offline material / silhouette study; no gameplay captured.",small,quiet,30,54);
        g.DrawLine(border,310,100,310,960); g.DrawLine(border,890,100,890,960);
        g.DrawString("Before the curtain",font,pale,24,104);
        g.DrawString("Phase I / the doll coffin",font,pale,335,104);
        g.DrawString("Phase II / pulled out, not released",font,pale,916,104);
        float floor=840;
        g.DrawLine(border,22,floor,285,floor);
        g.DrawImage(npc,new RectangleF(38,floor-52,32,52),new RectangleF(0,0,32,52),GraphicsUnit.Pixel);
        g.DrawImage(npc,new RectangleF(94,floor-104,64,104),new RectangleF(0,0,32,52),GraphicsUnit.Pixel);
        g.DrawImage(npc,new RectangleF(30,240,224,364),new RectangleF(0,0,32,52),GraphicsUnit.Pixel);
        g.DrawString("1x           2x",small,quiet,37,floor+13);
        g.DrawString("Above: 7x inspection\nActual sprite: 32 x 52\n12 expression / gesture cels",small,quiet,26,890);
        var pose=new FirstSeveranceDollPose();
        g.InterpolationMode=InterpolationMode.Bilinear;
        pose.Encased(3,0,false); DrawPose(g,atlas,harness,pose,603,500,.89f);
        var shellSize=FirstSeveranceShellSurface.Size(180)*.89f;
        g.DrawImage(shell,new RectangleF(603-shellSize.X/2,500-shellSize.Y/2,shellSize.X,shellSize.Y));
        pose.Body(3,0,0,1,1,false); DrawPose(g,atlas,harness,pose,1160,465,.68f,bodyFrames:bodyFrames);
        g.DrawString("Mostly enclosed: only a short lock and fingertips.\nNo face or complete arm pasted in front of the casing.",small,quiet,335,905);
        g.DrawString("Accepted porcelain arms / old restrained body silhouette.\nSmaller captive face inside the crown, not a giant NPC head.",small,quiet,916,905);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        result.Save(output,ImageFormat.Png);
        Capture(npc,shell,Path.Combine(Path.GetDirectoryName(output),"capture.png"));
        Frames(handFrames,bodyFrames,Path.Combine(Path.GetDirectoryName(output),"authored-frames.png"));
        if(includeMotion) Motion(atlas,harness,bodyFrames,Path.Combine(Path.GetDirectoryName(output),"motion"));
    }

    public static void Validate(string assets)
    {
        string[] names={"DollAttendant.png","DollRigAtlas.png","DollCoffin.png","DollHead.png"};
        int[] widths={32,384,256,34},heights={624,384,256,34};
        for(int i=0;i<names.Length;i++)
        {
            using var texture=new Bitmap(Path.Combine(assets,names[i]));
            if(texture.Width!=widths[i] || texture.Height!=heights[i]) throw new InvalidDataException("Unexpected native dimensions: "+names[i]);
            var colors=new System.Collections.Generic.HashSet<int>();
            int solid=0,empty=0;
            for(int y=0;y<texture.Height;y++) for(int x=0;x<texture.Width;x++)
            {
                Color c=texture.GetPixel(x,y);
                if(c.A==0) { empty++; if(c.R!=0||c.G!=0||c.B!=0) throw new InvalidDataException("Unclean transparent RGB"); }
                else if(c.A==255) { solid++; colors.Add(c.ToArgb()); }
                else throw new InvalidDataException("Non-binary game alpha");
            }
            if(solid==0||empty==0||colors.Count>32) throw new InvalidDataException("Invalid cutout or palette: "+names[i]);
        }
    }

    static void DrawPose(Graphics g,Bitmap atlas,Bitmap harness,FirstSeveranceDollPose pose,float x,float y,float scale,float seconds=3,bool reduced=false,Bitmap bodyFrames=null)
    {
        using var cord=new Pen(Color.FromArgb(140,124,107),1.2f);
        foreach(var cable in pose.Cords) g.DrawLine(cord,x+cable.AnchorX*scale,y+cable.AnchorY*scale,
            x+cable.Attachment.X*scale,y+cable.Attachment.Y*scale);
        foreach(var part in pose.Sprites)
        {
            var state=g.Save();
            g.TranslateTransform(x+part.Position.X*scale,y+part.Position.Y*scale);
            g.RotateTransform(part.Rotation*180/MathF.PI);
            g.ScaleTransform(part.Scale.X*scale,part.Scale.Y*scale);
            var region=FirstSeveranceDollPose.Region(part);
            var texture=part.Harness?harness:atlas;
            if(part.Harness&&part.Cell==0&&bodyFrames!=null)
            {
                var uv=FirstSeveranceDollFrames.Region(false,FirstSeveranceDollFrames.Body(seconds,0,reduced));
                var pivot=FirstSeveranceDollFrames.Pivot(false);
                g.DrawImage(bodyFrames,new RectangleF(-pivot.X*2,-pivot.Y*2,uv.Width*2,uv.Height*2),
                    new RectangleF(uv.X,uv.Y,uv.Width,uv.Height),GraphicsUnit.Pixel);
            }
            else if(FirstSeveranceDollPose.Flexible(part))
            {
                int rows=reduced?6:12;
                for(int row=0;row<rows;row++)
                {
                    float y0=region.Height*row/(float)rows,y1=region.Height*(row+1)/(float)rows;
                    float x0=-part.Pivot.X+FirstSeveranceDollPose.FlexOffset(part,y0,seconds,reduced);
                    float x1=-part.Pivot.X+FirstSeveranceDollPose.FlexOffset(part,y1,seconds,reduced);
                    var points=new[]{new PointF(x0,y0-part.Pivot.Y),new PointF(x0+region.Width,y0-part.Pivot.Y),new PointF(x1,y1-part.Pivot.Y)};
                    g.DrawImage(texture,points,new RectangleF(region.X,region.Y+y0,region.Width,y1-y0),GraphicsUnit.Pixel);
                }
            }
            else g.DrawImage(texture,new RectangleF(-part.Pivot.X,-part.Pivot.Y,region.Width,region.Height),
                new RectangleF(region.X,region.Y,region.Width,region.Height),GraphicsUnit.Pixel);
            g.Restore(state);
        }
    }

    static void Capture(Bitmap npc,Bitmap shell,string output)
    {
        using var result=new Bitmap(1500,720);
        using var g=Graphics.FromImage(result);
        using var font=new Font("Segoe UI",13);
        using var pale=new SolidBrush(Color.FromArgb(210,201,189));
        g.Clear(Color.FromArgb(21,20,27));
        g.InterpolationMode=InterpolationMode.NearestNeighbor; g.PixelOffsetMode=PixelOffsetMode.Half;
        float[] times={0,.35f,.57f,.76f,.90f};
        string[] labels={"Already present","Disassembly / hold","Accelerating pull","Into the center","Captured"};
        for(int frame=0;frame<times.Length;frame++)
        {
            float x=150+frame*300,y=275,scale=.65f;
            g.DrawString(labels[frame]+"\n"+(times[frame]*100).ToString("0")+"% of accepted Raid intro",font,pale,frame*300+14,20);
            var size=FirstSeveranceShellSurface.Size(times[frame]*180)*scale;
            g.InterpolationMode=InterpolationMode.Bilinear;
            g.DrawImage(shell,new RectangleF(x-size.X/2,y-size.Y/2,size.X,size.Y));
            g.InterpolationMode=InterpolationMode.NearestNeighbor;
            for(int i=0;i<FirstSeveranceDollCapture.Count;i++)
            {
                var part=FirstSeveranceDollCapture.Sample(i,times[frame],new(0,506),new(0,0),false);
                if(part.Opacity<.001f) continue;
                var state=g.Save();
                g.TranslateTransform(x+part.Position.X*scale,y+part.Position.Y*scale);
                g.RotateTransform(part.Rotation*180/MathF.PI);
                g.ScaleTransform(part.Scale*scale,part.Scale*scale);
                using var attrs=new ImageAttributes();
                var matrix=new ColorMatrix(); matrix.Matrix33=part.Opacity; attrs.SetColorMatrix(matrix);
                g.DrawImage(npc,new Rectangle(-part.Width/2,-part.Height/2,part.Width,part.Height),
                    part.X,part.Y,part.Width,part.Height,GraphicsUnit.Pixel,attrs);
                g.Restore(state);
            }
        }
        g.DrawString("AFTER ALL READY / offline shared capture curve, not preparation or an actual game recording.",font,pale,16,683);
        result.Save(output,ImageFormat.Png);
    }

    static void Frames(Bitmap hand,Bitmap body,string output)
    {
        using var image=new Bitmap(1600,1710); using var g=Graphics.FromImage(image);
        using var font=new Font("Segoe UI",13); using var ink=new SolidBrush(Color.LightGray);
        g.Clear(Color.FromArgb(21,20,27)); g.InterpolationMode=InterpolationMode.Bilinear;
        g.DrawString("16 AUTHORED CLAW POSES + 16 RESTRAINT POSES / actual runtime atlas frames, not rig screenshots",font,ink,20,15);
        for(int row=0;row<4;row++) for(int i=0;i<8;i++)
        {
            int frame=row%2*8+i;
            var uv=FirstSeveranceDollFrames.Region(row<2,frame);
            var tex=row<2?hand:body;
            if(tex.Width!=uv.Width*4||tex.Height!=uv.Height*4) throw new InvalidDataException("Invalid frame atlas size");
            g.DrawString((row<2?"Claw ":"Restraint ")+frame,font,ink,i*200+12,55+row*410);
            g.DrawImage(tex,new RectangleF(i*200+5,82+row*410,190,352),new RectangleF(uv.X,uv.Y,uv.Width,uv.Height),GraphicsUnit.Pixel);
        }
        image.Save(output,ImageFormat.Png);
    }

    static void Motion(Bitmap atlas,Bitmap harness,Bitmap bodyFrames,string folder)
    {
        Directory.CreateDirectory(folder);
        using var frame=new Bitmap(900,800);
        using var g=Graphics.FromImage(frame);
        using var font=new Font("Segoe UI",13);
        using var pale=new SolidBrush(Color.FromArgb(210,201,189));
        g.InterpolationMode=InterpolationMode.Bilinear; g.PixelOffsetMode=PixelOffsetMode.Half;
        var pose=new FirstSeveranceDollPose();
        for(int i=0;i<240;i++)
        {
            float seconds=i/60f;
            g.Clear(Color.FromArgb(21,20,27));
            g.DrawString("Offline rig + surface motion / 60 samples per second / not game FPS",font,pale,16,16);
            pose.Body(seconds,0,0,1,1,false);
            DrawPose(g,atlas,harness,pose,450,360,.78f,seconds,bodyFrames:bodyFrames);
            frame.Save(Path.Combine(folder,i.ToString("D4")+".png"),ImageFormat.Png);
        }
    }
}
