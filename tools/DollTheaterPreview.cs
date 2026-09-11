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
    public static void Create(string assets, string output)
    {
        Validate(assets);
        using var atlas=new Bitmap(Path.Combine(assets,"DollRigAtlas.png"));
        using var shell=new Bitmap(Path.Combine(assets,"DollCoffin.png"));
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
        g.DrawString("Above: 7x inspection\nActual sprite: 32 x 52\nBlink: 2 aligned frames",small,quiet,26,890);
        var pose=new FirstSeveranceDollPose();
        pose.Encased(3,0,false); DrawPose(g,atlas,pose,603,500,.89f);
        g.DrawImage(shell,new RectangleF(603-256*.89f*2.25f/2,500-256*.89f*2.25f/2,256*.89f*2.25f,256*.89f*2.25f));
        pose.EncasedFace(); DrawPose(g,atlas,pose,603,500,.89f);
        pose.Body(3,0,0,1,1,false); DrawPose(g,atlas,pose,1160,465,.68f);
        g.DrawString("Porcelain petals retain the old hinged eclosion.\nA real arm, white hair and a sorrowful face emerge first.",small,quiet,335,905);
        g.DrawString("Unequal cable tension / shared shoulder-elbow-wrist pivots.\nThe chest remains the only damage target; art is not collision.",small,quiet,916,905);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        result.Save(output,ImageFormat.Png);
    }

    public static void Validate(string assets)
    {
        string[] names={"DollAttendant.png","DollRigAtlas.png","DollCoffin.png","DollHead.png"};
        int[] widths={32,384,256,34},heights={104,384,256,34};
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

    static void DrawPose(Graphics g,Bitmap atlas,FirstSeveranceDollPose pose,float x,float y,float scale)
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
            g.DrawImage(atlas,new RectangleF(-part.Pivot.X,-part.Pivot.Y,128,part.CropHeight),
                new RectangleF(part.Cell%3*128,part.Cell/3*128,128,part.CropHeight),GraphicsUnit.Pixel);
            g.Restore(state);
        }
    }
}
