using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexedTextureData : ScriptableObject
{
    [System.Serializable]
    public class TextureAnimation
    {
        public float rate;    //INT16BE rate    Colour cycle rate.The units are such that a rate of 60 steps per second is represented as 214 = 16384.Lower rates can be obtained by linear scaling: for 30 steps / second, rate = 8192.
        public short flags;   //INT16BE flags   Flags which control the cycling of colours through the palette.If bit0 is 1, the colours should cycle, otherwise this colour register range is inactive and should have no effect.If bit1 is 0, the colours cycle upwards, i.e.each colour moves into the next index position in the colour map and the uppermost colour in the range moves down to the lowest position.If bit1 is 1, the colours cycle in the opposite direction.Only those colours between the low and high entries in the colour map should cycle.
        public byte low;      //UINT8   low The index of the first entry in the colour map that is part of this range.
        public byte high;     //UINT8   high    The index of the last entry in the colour map that is part of this range.
    }
    // Properties
    public short width;         //UINT16BE width   Image width, in pixels
    public short height;        //UINT16BE height Image height, in pixels
    public short xOrigin;       //INT16BE xOrigin Where on screen, in pixels, the image's top-left corner is. Value is usually 0,0 unless image is part of a larger image or not fullscreen.
    public short yOrigin;       //INT16BE yOrigin
    public byte depth;          //UINT8 numPlanes   Number of planes in bitmap; 1 for monochrome, 4 for 16 color, 8 for 256 color, or 0 if there is only a colormap, and no image data. (i.e., this file is just a colormap.)
    public byte mask;           //UINT8 mask    1 = masked, 2 = transparent color, 3 = lasso(for MacPaint). Mask data is not considered a bit plane.
    public byte compression;    //UINT8 compression If 0 then uncompressed. If 1 then image data is RLE compressed. If 2 "Vertical RLE" from Deluxe Paint for Atari ST. Other values are theoretically possible, representing other compression methods.
    public byte pad1;           //UINT8   pad1    Ignore when reading, set to 0 when writing for future compatibility
    public short transClr;      //UINT16BE    transClr    Transparent colour, useful only when mask >= 2
    public byte xAspect;        //UINT8   xAspect Pixel aspect, a ratio width:height; used for displaying the image on a variety of different screen resolutions for 320x200 5:6 or 10:11
    public byte yAspect;        //UINT8   yAspect
    public short pageWidth;     //INT16BE pageWidth   The size of the screen the image is to be displayed on, in pixels, usually 320×200
    public short pageHeight;    //INT16BE pageHeight
    
    public uint pixelsPerUnit;

    public byte[] indexes;          //raw indexed image as a buffer
    public Color[] colors;          //colors
    public Texture2D image;         //full color image
    public Texture2D indexedImage;  //single-channel image containing the indexes
    public Texture2D pallete;       //the initial palette

    public List<TextureAnimation> animations = new List<TextureAnimation>();

    public byte GetIndex(short x, short y)
    {
        return indexes[x+(height-y-1)*width];
    }
    // returns the color for the non-animated image
    // to obtain the animated color, perform the read via IndexedTextureRenderer
    public Color GetColor(short x, short y)
    {
        return colors[indexes[x + (height - y - 1) * width]];
    }
}