using System.IO.Compression;
using System.Buffers.Binary;
using ValleyArmory.Assets;
using ValleyArmory.Catalog;
using Xunit;

namespace ValleyArmory.Tests;

public sealed class SpriteSheetTests
{
    [Fact]
    public void WeaponsSpritesheetHasSevenOccupiedSixteenPixelCells()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "weapons.png");
        PngImage image = ReadRgbaPng(path);

        Assert.Equal(112, image.Width);
        Assert.Equal(16, image.Height);
        Assert.Equal(7, image.Width / 16);

        for (int cell = 0; cell < 7; cell++)
        {
            bool occupied = false;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (image.Alpha[y, cell * 16 + x] != 0)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (occupied)
                    break;
            }

            Assert.True(occupied, $"Sprite cell {cell} is empty.");
        }
    }

    [Fact]
    public void WeaponDefinitionsUseUniqueIndicesZeroThroughSixAndSharedTexture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        EquipmentDefinition[] weapons = catalog.Equipment
            .Where(item => item.Type is EquipmentType.Sword or EquipmentType.Dagger or EquipmentType.Hammer)
            .ToArray();

        Assert.Equal(7, weapons.Length);
        Assert.Equal(Enumerable.Range(0, 7), weapons.Select(item => item.Sprite!.SpriteIndex).OrderBy(index => index));
        Assert.All(weapons, weapon => Assert.Equal(AssetInjector.WeaponTextureAssetName, weapon.Sprite!.AssetName));
    }

    [Fact]
    public void WeaponSpriteCellsStayOpaqueOrFullyTransparent()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "weapons.png");
        PngImage image = ReadRgbaPng(path);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = image.Alpha[y, x];
                Assert.True(alpha == 0 || alpha == 255, $"Pixel ({x},{y}) has partial alpha {alpha}, which indicates anti-aliasing.");
            }
        }
    }

    [Fact]
    public void BootsSpritesheetHasThreeOccupiedSixteenPixelCellsWithTransparentMargins()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "boots.png");
        PngImage image = ReadRgbaPng(path);

        Assert.Equal(48, image.Width);
        Assert.Equal(16, image.Height);
        Assert.Equal(3, image.Width / 16);

        for (int cell = 0; cell < 3; cell++)
        {
            bool occupied = false;
            bool hasTransparentPixel = false;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (image.Alpha[y, cell * 16 + x] != 0)
                        occupied = true;
                    else
                        hasTransparentPixel = true;
                }
            }

            Assert.True(occupied, $"Boot sprite cell {cell} is empty.");
            Assert.True(hasTransparentPixel, $"Boot sprite cell {cell} has no transparent background pixels.");
        }
    }

    [Fact]
    public void BootsSpriteCellsDoNotLeakIntoEachOtherAndStayOpaqueOrFullyTransparent()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "boots.png");
        PngImage image = ReadRgbaPng(path);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = image.Alpha[y, x];
                Assert.True(alpha == 0 || alpha == 255, $"Pixel ({x},{y}) has partial alpha {alpha}, which indicates anti-aliasing.");
            }
        }
    }

    [Fact]
    public void BootDefinitionsUseUniqueIndicesZeroThroughTwoAndSharedTexture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        EquipmentDefinition[] boots = catalog.Equipment
            .Where(item => item.Type is EquipmentType.Boots)
            .ToArray();

        Assert.Equal(3, boots.Length);
        Assert.Equal(Enumerable.Range(0, 3), boots.Select(item => item.Sprite!.SpriteIndex).OrderBy(index => index));
        Assert.All(boots, boot => Assert.Equal(BootAssetInjector.BootTextureAssetName, boot.Sprite!.AssetName));
    }

    [Fact]
    public void ArmorSpritesheetMatchesTheRealShirtSourceRectFormula()
    {
        // Confirmed by disassembling StardewValley.ItemTypeDefinitions.ShirtDataDefinition.GetSourceRect
        // in the installed 1.6.15.24356 assembly: columns = texture.Width / 2; each icon is 8x8 at
        // x = (spriteIndex * 8) % columns, y = (spriteIndex * 8 / columns) * 32.
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armor.png");
        PngImage image = ReadRgbaPng(path);

        Assert.Equal(48, image.Width);
        Assert.Equal(32, image.Height);

        int columns = image.Width / 2;
        for (int spriteIndex = 0; spriteIndex < 3; spriteIndex++)
        {
            int x = spriteIndex * 8 % columns;
            int y = spriteIndex * 8 / columns * 32;

            bool occupied = false;
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 8; dx++)
                {
                    if (image.Alpha[y + dy, x + dx] != 0)
                        occupied = true;
                }
            }

            Assert.True(occupied, $"Armor icon at spriteIndex {spriteIndex} (rect {x},{y},8,8) is empty.");
        }
    }

    [Fact]
    public void ArmorSpriteCellsStayOpaqueOrFullyTransparent()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armor.png");
        PngImage image = ReadRgbaPng(path);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = image.Alpha[y, x];
                Assert.True(alpha == 0 || alpha == 255, $"Pixel ({x},{y}) has partial alpha {alpha}, which indicates anti-aliasing.");
            }
        }
    }

    [Fact]
    public void ArmorDefinitionsUseUniqueIndicesZeroThroughTwoAndSharedTexture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "armory.json");
        ArmoryCatalog catalog = new ArmoryCatalogLoader(new ArmoryCatalogValidator()).Load(path);
        EquipmentDefinition[] armor = catalog.Equipment
            .Where(item => item.Type is EquipmentType.Shirt)
            .ToArray();

        Assert.Equal(3, armor.Length);
        Assert.Equal(Enumerable.Range(0, 3), armor.Select(item => item.Sprite!.SpriteIndex).OrderBy(index => index));
        Assert.All(armor, item => Assert.Equal(ArmorAssetInjector.ArmorTextureAssetName, item.Sprite!.AssetName));
    }

    private static PngImage ReadRgbaPng(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);

        int width = 0;
        int height = 0;
        using MemoryStream compressed = new();
        int offset = 8;
        while (offset < bytes.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            ReadOnlySpan<byte> type = bytes.AsSpan(offset + 4, 4);
            ReadOnlySpan<byte> payload = bytes.AsSpan(offset + 8, length);
            offset += 12 + length;

            if (type.SequenceEqual("IHDR"u8))
            {
                width = BinaryPrimitives.ReadInt32BigEndian(payload[..4]);
                height = BinaryPrimitives.ReadInt32BigEndian(payload.Slice(4, 4));
                Assert.Equal(8, payload[8]);
                Assert.Equal(6, payload[9]);
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                compressed.Write(payload);
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                break;
            }
        }

        compressed.Position = 0;
        using ZLibStream decompressed = new(compressed, CompressionMode.Decompress);
        byte[] raw = new byte[height * (1 + width * 4)];
        int read = decompressed.Read(raw, 0, raw.Length);
        Assert.Equal(raw.Length, read);

        const int bytesPerPixel = 4;
        int stride = width * bytesPerPixel;
        byte[] decoded = new byte[height * stride];
        byte[,] alpha = new byte[height, width];
        int cursor = 0;
        for (int y = 0; y < height; y++)
        {
            byte filter = raw[cursor++];
            Assert.InRange(filter, (byte)0, (byte)4);
            int rowOffset = y * stride;
            for (int column = 0; column < stride; column++)
            {
                byte value = raw[cursor++];
                byte left = column >= bytesPerPixel ? decoded[rowOffset + column - bytesPerPixel] : (byte)0;
                byte above = y > 0 ? decoded[rowOffset - stride + column] : (byte)0;
                byte upperLeft = y > 0 && column >= bytesPerPixel
                    ? decoded[rowOffset - stride + column - bytesPerPixel]
                    : (byte)0;

                decoded[rowOffset + column] = filter switch
                {
                    0 => value,
                    1 => unchecked((byte)(value + left)),
                    2 => unchecked((byte)(value + above)),
                    3 => unchecked((byte)(value + ((left + above) / 2))),
                    4 => unchecked((byte)(value + Paeth(left, above, upperLeft))),
                    _ => throw new InvalidDataException($"Unsupported PNG filter {filter}.")
                };
            }

            for (int x = 0; x < width; x++)
                alpha[y, x] = decoded[rowOffset + x * bytesPerPixel + 3];
        }

        return new PngImage(width, height, alpha);
    }

    private static byte Paeth(byte left, byte above, byte upperLeft)
    {
        int estimate = left + above - upperLeft;
        int leftDistance = Math.Abs(estimate - left);
        int aboveDistance = Math.Abs(estimate - above);
        int upperLeftDistance = Math.Abs(estimate - upperLeft);
        return leftDistance <= aboveDistance && leftDistance <= upperLeftDistance
            ? left
            : aboveDistance <= upperLeftDistance
                ? above
                : upperLeft;
    }

    private sealed record PngImage(int Width, int Height, byte[,] Alpha);
}
