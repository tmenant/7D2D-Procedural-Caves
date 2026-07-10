using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class PNGFile
{
    private static readonly Logging.Logger logger = Logging.CreateLogger<PNGFile>();

    public struct ColorRGB
    {
        public byte r, g, b, a;
        public ColorRGB(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public ColorRGB[] pixels;

    public int width;

    public int height;

    public static PNGFile Load(string path)
    {
        var timer = ProfilingUtils.StartTimer();
        var pngFile = new PNGFile();
        byte[] fileBytes = File.ReadAllBytes(path);

        if (fileBytes[12] == 0x49 && fileBytes[13] == 0x48 && fileBytes[14] == 0x44 && fileBytes[15] == 0x52)
        {
            byte[] wBytes = new byte[] { fileBytes[19], fileBytes[18], fileBytes[17], fileBytes[16] };
            byte[] hBytes = new byte[] { fileBytes[23], fileBytes[22], fileBytes[21], fileBytes[20] };

            pngFile.width = BitConverter.ToInt32(wBytes, 0);
            pngFile.height = BitConverter.ToInt32(hBytes, 0);
        }
        else
        {
            throw new Exception("IHDR not found.");
        }

        List<byte> allCompressedData = new List<byte>();
        int currentIndex = 8;

        while (currentIndex < fileBytes.Length - 8)
        {
            byte[] lenBytes = new byte[] { fileBytes[currentIndex + 3], fileBytes[currentIndex + 2], fileBytes[currentIndex + 1], fileBytes[currentIndex + 0] };
            int chunkLength = BitConverter.ToInt32(lenBytes, 0);

            string chunkType = System.Text.Encoding.ASCII.GetString(fileBytes, currentIndex + 4, 4);

            if (chunkType == "IDAT")
            {
                int dataStart = currentIndex + 8;
                for (int i = 0; i < chunkLength; i++)
                {
                    allCompressedData.Add(fileBytes[dataStart + i]);
                }
            }
            else if (chunkType == "IEND")
            {
                break;
            }

            currentIndex += 4 + 4 + chunkLength + 4;
        }

        if (allCompressedData.Count == 0)
            throw new Exception("No IDAT block found.");

        byte[] decompressedBytes;

        using (MemoryStream msInput = new MemoryStream(allCompressedData.ToArray(), 2, allCompressedData.Count - 6))
        using (MemoryStream msOutput = new MemoryStream())
        {
            using (DeflateStream deflate = new DeflateStream(msInput, CompressionMode.Decompress))
            {
                deflate.CopyTo(msOutput);
            }
            decompressedBytes = msOutput.ToArray();
        }

        int bytesPerPixel = 4;
        int stride = pngFile.width * bytesPerPixel;

        int expectedLength = pngFile.height * (1 + stride);
        if (decompressedBytes.Length < expectedLength)
        {
            throw new Exception($"Expected: {expectedLength}, Received: {decompressedBytes.Length}");
        }

        byte[] scanlinePrev = new byte[stride];
        byte[] scanlineCurrent = new byte[stride];
        pngFile.pixels = new ColorRGB[pngFile.width * pngFile.height];

        int srcOffset = 0;

        for (int y = 0; y < pngFile.height; y++)
        {
            int filterType = decompressedBytes[srcOffset];
            srcOffset++;

            Array.Copy(decompressedBytes, srcOffset, scanlineCurrent, 0, stride);
            srcOffset += stride;

            for (int x = 0; x < stride; x++)
            {
                byte a = (x >= bytesPerPixel) ? scanlineCurrent[x - bytesPerPixel] : (byte)0;
                byte b = scanlinePrev[x];
                byte c = (x >= bytesPerPixel) ? scanlinePrev[x - bytesPerPixel] : (byte)0;

                switch (filterType)
                {
                    case 1: // Sub
                        scanlineCurrent[x] = (byte)((scanlineCurrent[x] + a) & 0xFF);
                        break;
                    case 2: // Up
                        scanlineCurrent[x] = (byte)((scanlineCurrent[x] + b) & 0xFF);
                        break;
                    case 3: // Average
                        scanlineCurrent[x] = (byte)((scanlineCurrent[x] + ((a + b) / 2)) & 0xFF);
                        break;
                    case 4: // Paeth
                        scanlineCurrent[x] = (byte)((scanlineCurrent[x] + pngFile.PaethPredictor(a, b, c)) & 0xFF);
                        break;
                }
            }

            int pixelRowOffset = y * pngFile.width;
            for (int i = 0; i < pngFile.width; i++)
            {
                int rIdx = i * bytesPerPixel;
                pngFile.pixels[pixelRowOffset + i] = new ColorRGB(
                    scanlineCurrent[rIdx],
                    scanlineCurrent[rIdx + 1],
                    scanlineCurrent[rIdx + 2],
                    scanlineCurrent[rIdx + 3]
                );
            }

            Array.Copy(scanlineCurrent, scanlinePrev, stride);
        }

        logger.Info($"png file loaded in {timer.ElapsedMilliseconds}ms, size:{pngFile.width}x{pngFile.height} : '{path}'");

        return pngFile;
    }

    private int PaethPredictor(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        if (pb <= pc) return b;
        return c;
    }
}
