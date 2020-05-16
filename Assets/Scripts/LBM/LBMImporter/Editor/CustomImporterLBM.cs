using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEditor;

[ScriptedImporter(1, "lbm")]
public class CustomImporterLBM : ScriptedImporter
{
    [SerializeField] uint pixelsPerUnit = 16;
    [SerializeField] bool ignoreFirstAnimation = true;
    [SerializeField] bool generatePNG = true;
    public override void OnImportAsset(AssetImportContext ctx)
    {
        IndexedTextureData texture = ParseLBM.ImportFile(ctx.assetPath);
        {

            texture.pixelsPerUnit = pixelsPerUnit;
            texture.indexedImage.name = "Indexed image";
            ctx.AddObjectToAsset("Indexed image", texture.indexedImage);
            texture.pallete.name = "Pallete";
            ctx.AddObjectToAsset("Palette", texture.pallete);

            if (ignoreFirstAnimation)
            {
                texture.animations.RemoveAt(0);
            }

            texture.name = "texture data";
            ctx.AddObjectToAsset("texture data", texture);
            texture.image.name = "Color image";
            ctx.AddObjectToAsset("image", texture.image);
        }
       

        Sprite sprite = Sprite.Create(texture.image, new Rect(0, 0, texture.width, texture.height), new Vector2(1f / 2, 1f / 2), pixelsPerUnit);
        {
            sprite.name = "Sprite";
            ctx.AddObjectToAsset("Sprite", sprite);
        }

        // Atlas - image for segmentation
        if (generatePNG)
        {
            // adds a secondary image for segmentation
            string filename = Path.GetFileNameWithoutExtension(ctx.assetPath);
            string imagepath = Path.GetDirectoryName(ctx.assetPath) + "\\" + filename + "-atlas.png";
            
            File.WriteAllBytes(imagepath, texture.image.EncodeToPNG());

            // Create image metadata
            // TODO FIX: this is not called the first time, as the image might still be under creation
            TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(imagepath);
            if (ti)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spritePixelsPerUnit = pixelsPerUnit;
                ti.spriteImportMode = SpriteImportMode.Multiple;
                ti.filterMode = FilterMode.Point;
                var spriteData = new SpriteMetaData[1];
                spriteData[0] = new SpriteMetaData();
                spriteData[0].pivot = new Vector2(1f / 2, 1f / 2);
                spriteData[0].rect = new Rect(0, 0, texture.width, texture.height);
                spriteData[0].name = "sprite";
                ti.spritesheet = spriteData;

                AssetDatabase.ImportAsset(imagepath, ImportAssetOptions.ForceUpdate);
            }
        }

        var prefab = new GameObject();
        {
            var material = new Material(Shader.Find("Sprites/IndexedPallete"));
            {
                material.name = "Material";
                material.SetTexture("_Indexed", texture.indexedImage);
                material.SetTexture("_Pallete", texture.pallete);

                ctx.AddObjectToAsset("material", material);
            }
            SpriteRenderer renderer = prefab.AddComponent<SpriteRenderer>();
            {
                renderer.sprite = sprite;
                renderer.material = material;
            }
            IndexedTextureAnimator itr = prefab.AddComponent<IndexedTextureAnimator>();
            {
                itr.texture = texture;
            }
            ctx.AddObjectToAsset("prefab", prefab, texture.image);
            ctx.SetMainObject(prefab);
        }

        // Assets that are not passed into the context as import outputs must be destroyed
        //DestroyImmediate(<unused>);
    }
}
 