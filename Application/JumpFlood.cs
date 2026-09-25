using Raylib_cs;
using System.Numerics;

namespace Application;

// Jump Flooding Algorithm: computes, for every texel, the coordinates of the nearest white texel in the source.
// Coordinates are packed as 16-bit values into the RGBA8 channels (see the shaders).
internal class JumpFlood
{
    private readonly Shader seedShader;
    private readonly Shader stepShader;
    private readonly int locJumpStep;
    private RenderTexture2D front;
    private RenderTexture2D back;

    public Texture2D Result => front.Texture;

    public JumpFlood(string seedShaderPath, string stepShaderPath, int width, int height)
    {
        seedShader = Raylib.LoadShader(null, seedShaderPath);
        stepShader = Raylib.LoadShader(null, stepShaderPath);
        locJumpStep = Raylib.GetShaderLocation(stepShader, "jumpStep");
        front = Raylib.LoadRenderTexture(width, height);
        back = Raylib.LoadRenderTexture(width, height);
    }

    public void Update(Texture2D source)
    {
        RunPass(seedShader, source);

        int maxDimension = Math.Max(source.Width, source.Height);
        for (int step = (int)BitOperations.RoundUpToPowerOf2((uint)maxDimension) / 2; step >= 1; step /= 2)
        {
            Raylib.SetShaderValue(stepShader, locJumpStep, step, ShaderUniformDataType.Int);
            RunPass(stepShader, front.Texture);
        }
    }

    private void RunPass(Shader shader, Texture2D input)
    {
        Raylib.BeginTextureMode(back);
        Raylib.BeginShaderMode(shader);
        // Alpha holds data, so blending would corrupt it
        Rlgl.DisableColorBlend();
        Raylib.DrawTexture(input, 0, 0, Color.White);
        Raylib.EndShaderMode();
        Rlgl.EnableColorBlend();
        Raylib.EndTextureMode();

        (front, back) = (back, front);
    }

    public void Unload()
    {
        Raylib.UnloadShader(seedShader);
        Raylib.UnloadShader(stepShader);
        Raylib.UnloadRenderTexture(front);
        Raylib.UnloadRenderTexture(back);
    }
}
