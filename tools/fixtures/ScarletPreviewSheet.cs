// Tiny bitmap text (3x5, digits and capitals) and contact-sheet composition for the
// Scarlet preview. The hidden device has no SpriteFont, so labels are drawn from pixels.
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

internal static class PreviewText
{
    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['0'] = "111101101101111", ['1'] = "010110010010111", ['2'] = "111001111100111", ['3'] = "111001111001111",
        ['4'] = "101101111001001", ['5'] = "111100111001111", ['6'] = "111100111101111", ['7'] = "111001001010010",
        ['8'] = "111101111101111", ['9'] = "111101111001111",
        ['A'] = "010101111101101", ['B'] = "110101110101110", ['C'] = "011100100100011", ['D'] = "110101101101110",
        ['E'] = "111100110100111", ['F'] = "111100110100100", ['G'] = "011100101101011", ['H'] = "101101111101101",
        ['I'] = "111010010010111", ['J'] = "001001001101010", ['K'] = "101101110101101", ['L'] = "100100100100111",
        ['M'] = "101111111101101", ['N'] = "110101101101101", ['O'] = "010101101101010", ['P'] = "110101110100100",
        ['Q'] = "010101101110011", ['R'] = "110101110101101", ['S'] = "011100010001110", ['T'] = "111010010010010",
        ['U'] = "101101101101111", ['V'] = "101101101101010", ['W'] = "101101111111101", ['X'] = "101101010101101",
        ['Y'] = "101101010010010", ['Z'] = "111001010100111",
        ['-'] = "000000111000000", ['+'] = "000010111010000", ['.'] = "000000000000010", [':'] = "000010000010000",
        ['/'] = "001001010100100", ['='] = "000111000111000", ['_'] = "000000000000111", [' '] = "000000000000000"
    };

    internal static int Width(string text, int scale) => text.Length * 4 * scale - scale;
    internal const int Height = 5;

    internal static void Draw(SpriteBatch batch, Texture2D pixel, string text, Vector2 at, int scale, Color color, bool shadow = true)
    {
        if (shadow) Draw(batch, pixel, text, at + new Vector2(scale, scale), scale, new Color(0, 0, 0, 200), false);
        float x = at.X;
        foreach (char raw in text.ToUpperInvariant())
        {
            if (Glyphs.TryGetValue(raw, out var rows))
            {
                for (int i = 0; i < 15; i++)
                {
                    if (rows[i] != '1') continue;
                    batch.Draw(pixel, new Rectangle((int)x + i % 3 * scale, (int)at.Y + i / 3 * scale, scale, scale), color);
                }
            }
            x += 4 * scale;
        }
    }
}

internal sealed record SheetCell(Color[] Pixels, int Width, int Height, string Label, Color Bar);

internal static class PreviewSheet
{
    // Area-average downscale (box filter) so one-pixel lines survive instead of aliasing away.
    internal static Color[] Downsample(Color[] source, int sourceWidth, int sourceHeight, int width, int height)
    {
        var result = new Color[width * height];
        float fx = sourceWidth / (float)width, fy = sourceHeight / (float)height;
        for (int y = 0; y < height; y++)
        {
            int y0 = (int)MathF.Floor(y * fy), y1 = Math.Min(sourceHeight, Math.Max(y0 + 1, (int)MathF.Ceiling((y + 1) * fy)));
            for (int x = 0; x < width; x++)
            {
                int x0 = (int)MathF.Floor(x * fx), x1 = Math.Min(sourceWidth, Math.Max(x0 + 1, (int)MathF.Ceiling((x + 1) * fx)));
                int r = 0, g = 0, b = 0, n = 0;
                for (int sy = y0; sy < y1; sy++)
                {
                    int row = sy * sourceWidth;
                    for (int sx = x0; sx < x1; sx++)
                    {
                        var c = source[row + sx];
                        r += c.R; g += c.G; b += c.B; n++;
                    }
                }
                result[y * width + x] = new Color((byte)(r / n), (byte)(g / n), (byte)(b / n), (byte)255);
            }
        }
        return result;
    }

    internal static SheetCell Cell(RenderTarget2D target, int width, int height, string label, Color bar)
    {
        var pixels = new Color[target.Width * target.Height];
        target.GetData(pixels);
        return new SheetCell(Downsample(pixels, target.Width, target.Height, width, height), width, height, label, bar);
    }

    internal static void Save(GraphicsDevice device, Texture2D pixel, IReadOnlyList<SheetCell> cells, int columns,
        int cellWidth, int cellHeight, string title, string path)
    {
        const int gap = 6, label = 22, header = 30;
        int rows = (cells.Count + columns - 1) / columns;
        int width = columns * cellWidth + (columns + 1) * gap;
        int height = header + rows * (cellHeight + label + gap) + gap;
        using var target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        var textures = new List<Texture2D>();
        var oldTargets = device.GetRenderTargets();
        device.SetRenderTarget(target);
        device.Clear(new Color(18, 18, 22));
        using var batch = new SpriteBatch(device);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        PreviewText.Draw(batch, pixel, title, new Vector2(gap, 6), 3, new Color(235, 235, 240));
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            int x = gap + i % columns * (cellWidth + gap), y = header + i / columns * (cellHeight + label + gap);
            var texture = new Texture2D(device, cell.Width, cell.Height);
            texture.SetData(cell.Pixels);
            textures.Add(texture);
            batch.Draw(texture, new Rectangle(x, y, cellWidth, cellHeight), Color.White);
            batch.Draw(pixel, new Rectangle(x, y + cellHeight, cellWidth, label), new Color(34, 34, 40));
            batch.Draw(pixel, new Rectangle(x, y + cellHeight, cellWidth, 4), cell.Bar);
            PreviewText.Draw(batch, pixel, cell.Label, new Vector2(x + 4, y + cellHeight + 6), 2, new Color(225, 225, 230));
        }
        batch.End();
        device.SetRenderTargets(oldTargets);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var file = File.Create(path)) target.SaveAsPng(file, width, height);
        foreach (var texture in textures) texture.Dispose();
    }
}
