using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


public class ParseLBM
{
    static string GetFourCC(byte[] data, ref int index)
    {
        string fourcc = "";
        for (int c = 0; c < 4; ++c)
        {
            fourcc += (char)data[index];
            ++index;
        }
        return fourcc;
    }
    static long GetLong(byte[] data, ref int index)
    {
        long value = ToLittleEndian(data[index + 0], data[index + 1], data[index + 2], data[index + 3]);
        index += 4;
        return value;
    }
    static byte GetByte(byte[] data, ref int index)
    {
        byte value = data[index + 0];
        ++index;
        return value;
    }
    static short GetShort(byte[] data, ref int index)
    {
        short value = ToLittleEndian(data[index + 0], data[index + 1]);
        index += 2;
        return value;
    }
    static long ToLittleEndian(byte b0, byte b1, byte b2, byte b3)
    {
        return (long)(b0 * 256 + b1) * 256 * 256 + ((b2 * 256) + b3);
    }
    static short ToLittleEndian(byte b0, byte b1)
    {
        return (short)((b0 * 256) + b1);
    }

    //https://en.wikipedia.org/wiki/ILBM
    public static IndexedTextureData ImportFile(string path)
    {
        Texture2D indexedImage;
        Texture2D pallete;
        IndexedTextureData texture;

        byte[] data = File.ReadAllBytes(path);

        int i = 0;
        long chunkSize = 0;
        long chunkPos = 0;
        int chunkStart = 0;
        string chunk = "";

        texture = IndexedTextureData.CreateInstance<IndexedTextureData>(); ;
        bool success = false;
        while (i < data.Length && !success)
        {
            //read chunk
            if (chunkPos >= chunkSize)
            {
                chunkStart = i;
                chunkPos = 0;
                chunk = GetFourCC(data, ref i);
                chunkSize = GetLong(data, ref i);
                if (chunk == "FORM")
                {
                    string type = GetFourCC(data, ref i);
                    //Debug.Log(chunk + chunkSize + ":" + type);
                    chunkPos = 16;
                    chunkSize = 16;
                }
                else if (chunk == "BMHD")
                {
                    chunkSize = 20;
                    texture.width = GetShort(data, ref i);         //UINT16BE width   Image width, in pixels
                    texture.height = GetShort(data, ref i);        //UINT16BE    height Image height, in pixels
                    texture.xOrigin = GetShort(data, ref i);       //INT16BE xOrigin Where on screen, in pixels, the image's top-left corner is. Value is usually 0,0 unless image is part of a larger image or not fullscreen.
                    texture.yOrigin = GetShort(data, ref i);       //INT16BE yOrigin
                    texture.depth = GetByte(data, ref i);          //UINT8 numPlanes   Number of planes in bitmap; 1 for monochrome, 4 for 16 color, 8 for 256 color, or 0 if there is only a colormap, and no image data. (i.e., this file is just a colormap.)
                    texture.mask = GetByte(data, ref i);           //UINT8 mask    1 = masked, 2 = transparent color, 3 = lasso(for MacPaint).Mask data is not considered a bit plane.
                    texture.compression = GetByte(data, ref i);    //UINT8 compression If 0 then uncompressed. If 1 then image data is RLE compressed. If 2 "Vertical RLE" from Deluxe Paint for Atari ST. Other values are theoretically possible, representing other compression methods.
                    texture.pad1 = GetByte(data, ref i);            //UINT8   pad1    Ignore when reading, set to 0 when writing for future compatibility
                    texture.transClr = GetShort(data, ref i);       //UINT16BE    transClr    Transparent colour, useful only when mask >= 2
                    texture.xAspect = GetByte(data, ref i);         //UINT8   xAspect Pixel aspect, a ratio width:height; used for displaying the image on a variety of different screen resolutions for 320x200 5:6 or 10:11
                    texture.yAspect = GetByte(data, ref i);         //UINT8   yAspect
                    texture.pageWidth = GetShort(data, ref i);      //INT16BE pageWidth   The size of the screen the image is to be displayed on, in pixels, usually 320×200
                    texture.pageHeight = GetShort(data, ref i);     //INT16BE pageHeigh
                    chunkPos = 20;
                    //Debug.Log(chunk + ":" + chunkSize + ":" + "[" + texture.width + "," + texture.height + "]" + "(" + texture.xOrigin + "," + texture.yOrigin + ")," + texture.depth + "," + texture.mask + "," + texture.compression + "," + texture.pad1 + "," + texture.transClr + "," + texture.xAspect + "," + texture.yAspect + ",(" + texture.pageWidth + "," + texture.pageHeight + ")");
                }
                else if (chunk == "CMAP")
                {
                    int colorCount = (int)Mathf.Pow(2, texture.depth);
                    chunkSize = 4 + colorCount;
                    Color[] colors = new Color[colorCount];

                    int c = 0;
                    while (c < colorCount)
                    {

                        colors[c] = new Color32(GetByte(data, ref i), GetByte(data, ref i), GetByte(data, ref i), 255);
                        ++c;
                    }

                    pallete = new Texture2D(colorCount, 1);
                    pallete.filterMode = FilterMode.Point;
                    pallete.wrapMode = TextureWrapMode.Clamp;
                    pallete.SetPixels(colors);
                    pallete.Apply();
                    texture.pallete = pallete;
                    texture.colors = colors;

                    //Debug.Log(chunk + ":" + chunkSize);
                    chunkPos = chunkSize;
                }
                else if (chunk == "CRNG")
                {
                    short padding = GetShort(data, ref i); //INT16BE padding 0x0000
                    short rate = GetShort(data, ref i);    //INT16BE rate    Colour cycle rate.The units are such that a rate of 60 steps per second is represented as 214 = 16384.Lower rates can be obtained by linear scaling: for 30 steps / second, rate = 8192.
                    short flags = GetShort(data, ref i);   //INT16BE flags   Flags which control the cycling of colours through the palette.If bit0 is 1, the colours should cycle, otherwise this colour register range is inactive and should have no effect.If bit1 is 0, the colours cycle upwards, i.e.each colour moves into the next index position in the colour map and the uppermost colour in the range moves down to the lowest position.If bit1 is 1, the colours cycle in the opposite direction.Only those colours between the low and high entries in the colour map should cycle.
                    byte low = GetByte(data, ref i);      //UINT8   low The index of the first entry in the colour map that is part of this range.
                    byte high = GetByte(data, ref i);     //UINT8   high    The index of the last entry in the colour map that is part of this range.

                    //Debug.Log(chunk + ":" + chunkSize + "," + padding + "," + (rate / 16384f) + "," + flags + "," + low + "," + high);
                    chunkPos = chunkSize;

                    IndexedTextureData.TextureAnimation animation = new IndexedTextureData.TextureAnimation();
                    animation.flags = flags;
                    animation.low = low;
                    animation.high = high;
                    animation.rate = (rate / 16384f);
                    texture.animations.Add(animation);
                }
                else if (chunk == "BODY")
                {
                    //Debug.Log(chunk + ":" + chunkSize);
                    texture.indexes = new byte[texture.width* texture.height];
                    indexedImage = new Texture2D(texture.width, texture.height, TextureFormat.R8, false);
                    Texture2D image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    indexedImage.filterMode = FilterMode.Point;
                    indexedImage.wrapMode = TextureWrapMode.Clamp;
                    // no compression
                    if (texture.compression == 0)
                    {
                        for (int y = 0; y < texture.height; ++y)
                        {
                            for (int x = 0; x < texture.width; ++x)
                            {
                                byte color = GetByte(data, ref i);
                                indexedImage.SetPixel(x, texture.height - y, new Color32(color, 0, 0, 255));
                                image.SetPixel(x, texture.height - y, texture.colors[color]);
                                texture.indexes[x + y * texture.width] = color;
                            }
                        }
                    }
                    // RLE compression https://en.wikipedia.org/wiki/Run-length_encoding
                    else if (texture.compression == 1)
                    {
                        for (int y = 0; y < texture.height; ++y)
                        {
                            for (int x = 0; x < texture.width;)
                            {
                                byte control = GetByte(data, ref i);

                                // If[Value] > 128, then:
                                if (control > 128)
                                {
                                    // Read the next byte and output it(257 - [Value]) times.
                                    byte color = GetByte(data, ref i);
                                    int count = 257 - control;
                                    while (count > 0)
                                    {
                                        indexedImage.SetPixel(x, texture.height - y, new Color32(color, color, color, color));
                                        texture.indexes[x+y*texture.width] = color;
                                        image.SetPixel(x, texture.height - y, texture.colors[color]);
                                        ++x;
                                        --count;
                                    }
                                    // Move forward 2 bytes and return to step 1.
                                }
                                // Else if [Value] < 128, then:
                                else if (control < 128)
                                {
                                    // Read and output the next[value + 1] bytes
                                    // Move forward[Value + 2] bytes and return to step 3a.
                                    int count = control + 1;
                                    while (count > 0)
                                    {
                                        byte color = GetByte(data, ref i);
                                        indexedImage.SetPixel(x, texture.height - y, new Color32(color, color, color, color));
                                        texture.indexes[x + y * texture.width] = color;
                                        image.SetPixel(x, texture.height - y, texture.colors[color]);
                                        ++x;
                                        --count;
                                    }
                                }
                                // Else[Value] = 128, exit the loop(stop decompressing)

                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Compression mode " + texture.compression + " not supported");
                    }

                    image.Apply();
                    texture.image = image;
                    indexedImage.Apply();
                    texture.indexedImage = indexedImage;
                    chunkPos = chunkSize;
                    //Debug.Log("End of parsing:"+i+"/" +data.Length);
                    success = true;
                }
                else
                {
                    Debug.LogWarning("Unknown chunk header:" +chunk);
                }
            }
            else
            {
                ++chunkPos;
                ++i;
            }
        }
        if (success)
        {
            return texture;
        }
        else
        {
            return null;
        }
    }
}
