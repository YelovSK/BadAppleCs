using Raylib_cs;
using System.Numerics;

namespace Application;

internal class BadAppleShader
{
    public Shader Shader { get; }

    // Uniform locations
    private readonly int locTime;
    private readonly int locTexSize;
    private readonly int locLightPos;
    private readonly int locWhiteThreshold;
    private readonly int locShadowSamples;
    private readonly int locSoftShadows;
    private readonly int locSeedTex;

    // Properties
    public float Time
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locTime, value, ShaderUniformDataType.Float);
        }
    }

    public Vector2 TexSize
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locTexSize, value, ShaderUniformDataType.Vec2);
        }
    }

    public Vector2 LightPos
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locLightPos, value, ShaderUniformDataType.Vec2);
        }
    }

    public float WhiteThreshold
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locWhiteThreshold, value, ShaderUniformDataType.Float);
        }
    }

    public bool SoftShadows
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locSoftShadows, value, ShaderUniformDataType.Int);
        }
    }

    public int ShadowSamples
    {
        get;
        set
        {
            field = Math.Max(1, value);
            Raylib.SetShaderValue(Shader, locShadowSamples, field, ShaderUniformDataType.Int);
        }
    }

    public static BadAppleShader Load(string path) => new(path);

    private BadAppleShader(string path)
    {
        Shader = Raylib.LoadShader(null, path);

        locTime = Raylib.GetShaderLocation(Shader, "time");
        locTexSize = Raylib.GetShaderLocation(Shader, "texSize");
        locLightPos = Raylib.GetShaderLocation(Shader, "lightPos");
        locWhiteThreshold = Raylib.GetShaderLocation(Shader, "whiteThreshold");
        locShadowSamples = Raylib.GetShaderLocation(Shader, "softShadowSamples");
        locSoftShadows = Raylib.GetShaderLocation(Shader, "softShadows");
        locSeedTex = Raylib.GetShaderLocation(Shader, "seedTex");

        // Defaults
        SoftShadows = true;
        ShadowSamples = 10;
    }

    public void ToggleSoftShadows() => SoftShadows = !SoftShadows;

    // raylib forgets extra sampler bindings after each draw batch, so call this after BeginShaderMode
    public void BindSeedTexture(Texture2D texture) => Raylib.SetShaderValueTexture(Shader, locSeedTex, texture);

    public void SetQualityPreset(ShaderQuality quality)
    {
        switch (quality)
        {
            case ShaderQuality.Low:
                SoftShadows = false;
                break;

            case ShaderQuality.Medium:
                ShadowSamples = 5;
                SoftShadows = true;
                break;

            case ShaderQuality.High:
                ShadowSamples = 10;
                SoftShadows = true;
                break;
        }
    }
}

internal enum ShaderQuality
{
    Low = 1,
    Medium = 2,
    High = 3,
}
