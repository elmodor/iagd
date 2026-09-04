using IAGrim.Parser.Arc;
using SkiaSharp;
using Xunit;

namespace Parser.Tests;

public class DDSImageReaderTests {
    [Fact]
    public void Read_CanDecodeDxt1Image() {
        var dds = CreateTestDds();
        var pixels = DDSImageReader.Read(dds, 0);
        Assert.NotNull(pixels);
        Assert.NotEmpty(pixels);
        Assert.Equal(16, pixels.Length);
    }

    [Fact]
    public void ExtractImage_CanCreateBitmap() {
        var dds = CreateTestDds();
        // ExtractImage expects Grim Dawn wrapper:
        // bytes 0..7 = unused/header data
        // bytes 8..11 = DDS payload size
        // bytes 12.. = DDS data
        var wrapped = new byte[12 + dds.Length];
        BitConverter.GetBytes(dds.Length).CopyTo(wrapped, 8);
        Array.Copy(dds, 0, wrapped, 12, dds.Length);
        using var image = DDSImageReader.ExtractImage(wrapped);
        Assert.NotNull(image);
        Assert.Equal(4, image.Width);
        Assert.Equal(4, image.Height);

        using var skImage = SKImage.FromBitmap(image);
        using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
        Assert.NotNull(data);
        Assert.True(data.Size > 0);
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                var pixel = image.GetPixel(x, y);
                Assert.True(pixel.Red > 200, $"Pixel ({x},{y}) should be red, but was {pixel}.");
                Assert.True(pixel.Green < 50, $"Pixel ({x},{y}) should have little green, but was {pixel}.");
                Assert.True(pixel.Blue < 50, $"Pixel ({x},{y}) should have little blue, but was {pixel}.");
                Assert.Equal(255, pixel.Alpha);
            }
        }
    }

    [RequiresItemsArcFact]
    public void ExtractItemIcons_ExtractsIconsFromItemsArc() {
        var itemArcPath = Path.Combine(AppContext.BaseDirectory, "TestData", "Items.arc");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"IAGrim-ParserTests-{Guid.NewGuid()}");
        try {
            Directory.CreateDirectory(outputDirectory);
            DDSImageReader.ExtractItemIcons(itemArcPath, outputDirectory);
            var files = Directory.GetFiles(outputDirectory, "*.png");
            Assert.NotEmpty(files);

            var iconPath = files[0];
            using var icon = SKBitmap.Decode(iconPath);
            Assert.NotNull(icon);
            Assert.True(icon.Width > 0);
            Assert.True(icon.Height > 0);

            var topLeft = icon.GetPixel(0, 0);
            Assert.Equal(0, topLeft.Alpha);

            var visiblePixels = 0;
            for (var y = 0; y < icon.Height; y++) {
                for (var x = 0; x < icon.Width; x++) {
                    if (icon.GetPixel(x, y).Alpha > 0) {
                        visiblePixels++;
                    }
                }
            }
            Assert.True(visiblePixels > 10, $"Expected the icon to contain visible pixels, but found {visiblePixels}.");
        }
        finally {
            if (Directory.Exists(outputDirectory)) {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static byte[] CreateTestDds() {
        // 4x4 DXT1 DDS:
        // DDS header = 128 bytes
        // DXT1 block = 8 bytes
        // One DXT1 block represents a 4x4 pixel area
        byte[] data = new byte[128 + 8];
        void WriteInt(int offset, int value) {
            BitConverter.GetBytes(value).CopyTo(data, offset);
        }
        // DDS magic
        data[0] = (byte)'D';
        data[1] = (byte)'D';
        data[2] = (byte)'S';
        data[3] = (byte)' ';
        // DDS_HEADER
        WriteInt(4, 124);         // dwSize
        WriteInt(8, 0x00081007);  // dwFlags
        WriteInt(12, 4);          // dwHeight
        WriteInt(16, 4);          // dwWidth
        WriteInt(20, 8);          // dwPitchOrLinearSize
        WriteInt(24, 0);          // dwDepth
        WriteInt(28, 0);          // dwMipMapCount
        // DDS_PIXELFORMAT
        WriteInt(76, 32);         // dwSize
        WriteInt(80, 0x00000004); // DDPF_FOURCC
        // "DXT1"
        WriteInt(84, 0x31545844);
        WriteInt(88, 0);          // dwRGBBitCount
        WriteInt(92, 0);          // dwRBitMask
        WriteInt(96, 0);          // dwGBitMask
        WriteInt(100, 0);         // dwBBitMask
        WriteInt(104, 0);          // dwABitMask
        // caps
        WriteInt(108, 0x1000);    // DDSCAPS_TEXTURE
        // DXT1 block
        // color0 = RGB565 red
        // color1 = RGB565 black
        // All 16 pixels use color0
        ushort red565 = 0x001F;
        ushort black565 = 0x0000;
        BitConverter.GetBytes(red565).CopyTo(data, 128);
        BitConverter.GetBytes(black565).CopyTo(data, 130);
        // All pixel indices = 0 => color0
        data[132] = 0;
        data[133] = 0;
        data[134] = 0;
        data[135] = 0;
        return data;
    }
}
