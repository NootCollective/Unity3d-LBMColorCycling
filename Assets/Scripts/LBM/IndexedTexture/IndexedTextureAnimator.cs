using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndexedTextureAnimator : MonoBehaviour
{
    public enum AnimatorControl
    {
        Individual,     // uses its own instantiated material
        SharedMaster,   // controls the main material
        SharedSlave     // no control over the animation, only uses the main material
    }
    [Header("Links")]
    [SerializeField] new Renderer renderer;
    [SerializeField] public IndexedTextureData texture;
    [SerializeField] Texture2D currentPallete;
    [SerializeField] Material material;

    [Header("Animation parameters")]
    public bool animate = true;
    public float speed = 1;
    public bool interpolate;
    [SerializeField] AnimationCurve interpolation = AnimationCurve.Linear(0,0,1,1);

    [Header("Other Parameters")]
    public AnimatorControl animationControl = AnimatorControl.Individual;

    [Header("Status (Debug)")]
    public bool[] animateIndividual;
    [SerializeField] public Color[] frameColors;
    Color[] currentColors;
    public float[] frames;

    void OnValidate()
    {
        if (!renderer)
        {
            renderer = GetComponent<Renderer>();
        }
        if (texture != null)
        {
            if (!Application.isPlaying)
            {
                renderer.sharedMaterial.SetTexture("_Indexed", texture.indexedImage);
                renderer.sharedMaterial.SetTexture("_Pallete", texture.pallete);
            }
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        frames = new float[texture.animations.Count];

        frameColors = new Color[texture.colors.Length];
        currentColors = new Color[texture.colors.Length];

        currentPallete = texture.pallete;

        texture.colors.CopyTo(frameColors, 0);
        currentPallete = new Texture2D(frameColors.Length, 1);
        currentPallete.SetPixels(frameColors);
        currentPallete.filterMode = FilterMode.Point;
        currentPallete.Apply();

        material = renderer.sharedMaterial;
        if (animationControl == AnimatorControl.Individual)
        {
            material = new Material(material);
            renderer.material = material;
        }
        if (animationControl != AnimatorControl.SharedSlave)
        {
            material.SetTexture("_Pallete", currentPallete);

            animateIndividual = new bool[texture.animations.Count];
            for (int a = 0; a < texture.animations.Count; ++a)
            {
                animateIndividual[a] = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (animationControl != AnimatorControl.SharedSlave && animate)
        {
            frameColors.CopyTo(currentColors, 0);

            bool changed = false;
            for (int a = 0; a < frames.Length; ++a)
            {
                if (animateIndividual[a])
                {
                    IndexedTextureData.TextureAnimation animation = texture.animations[a];

                    int previousFrame = (int)frames[a];
                    frames[a] += texture.animations[a].rate * 60 * Time.deltaTime * speed;
                    int currentFrame = (int)frames[a];

                    if (previousFrame != currentFrame)
                    {

                        Color previous = frameColors[animation.high];
                        for (int c = animation.low; c <= animation.high; ++c)
                        {
                            Color tmp = frameColors[c];
                            frameColors[c] = previous;
                            previous = tmp;
                        }

                        changed = true;
                        int duration = animation.high - animation.low+1;
                        if (currentFrame >= duration)
                            frames[a] -= duration;
                    }
                    else if (interpolate)
                    {
                        float delta = frames[a] - previousFrame;

                        for (int c = animation.low; c < animation.high; ++c)
                        {
                            Color current = frameColors[c];
                            currentColors[c] = Color.Lerp(frameColors[c], frameColors[c + 1], interpolation.Evaluate(1 - delta));
                        }
                        currentColors[animation.high] = Color.Lerp(frameColors[animation.high], frameColors[animation.low], interpolation.Evaluate(1 - delta));
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                if (!interpolate)
                {
                    currentPallete.SetPixels(frameColors);
                    currentPallete.Apply();
                }
                else
                {
                    currentPallete.SetPixels(currentColors);
                    currentPallete.Apply();
                }
            }
        }
    }

    public Color GetColor(short x, short y)
    {
        uint index = index = texture.GetIndex(x, y);
        if (!interpolate)
        {
            return frameColors[index];
        }
        else
        {
            return currentColors[index];
        }
       
    }
}
