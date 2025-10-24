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
    private readonly int locStepSize;
    private readonly int locShadowSamples;
    private readonly int locSoftShadows;

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

    public bool SoftShadows
    {
        get;
        set
        {
            field = value;
            Raylib.SetShaderValue(Shader, locSoftShadows, value, ShaderUniformDataType.Int);
        }
    }

    public int StepSize
    {
        get;
        set
        {
            field = Math.Max(0, value);
            Raylib.SetShaderValue(Shader, locStepSize, value, ShaderUniformDataType.Int);
        }
    }

    public int ShadowSamples
    {
        get;
        set
        {
            field = Math.Max(0, value);
            Raylib.SetShaderValue(Shader, locShadowSamples, value, ShaderUniformDataType.Int);
        }
    }

    public static BadAppleShader Load(string path) => new(path);

    private BadAppleShader(string path)
    {
        Shader = Raylib.LoadShader(null, path);

        locTime = Raylib.GetShaderLocation(Shader, "time");
        locTexSize = Raylib.GetShaderLocation(Shader, "texSize");
        locLightPos = Raylib.GetShaderLocation(Shader, "lightPos");
        locStepSize = Raylib.GetShaderLocation(Shader, "raymarchStepSizePx");
        locShadowSamples = Raylib.GetShaderLocation(Shader, "softShadowSamples");
        locSoftShadows = Raylib.GetShaderLocation(Shader, "softShadows");

        // Defaults
        SoftShadows = true;
        StepSize = 3;
        ShadowSamples = 10;
    }

    public void ToggleSoftShadows() => SoftShadows = !SoftShadows;

    public void SetQualityPreset(ShaderQuality quality)
    {
        switch (quality)
        {
            case ShaderQuality.Low:
                StepSize = 1;
                SoftShadows = false;
                break;

            case ShaderQuality.Medium:
                StepSize = 2;
                ShadowSamples = 5;
                SoftShadows = true;
                break;

            case ShaderQuality.High:
                StepSize = 1;
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
