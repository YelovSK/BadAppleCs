using Application;
using Raylib_cs;
using System.Numerics;
using System.Text.RegularExpressions;

internal class Program
{
    const int SCREEN_WIDTH = 1440;
    const int SCREEN_HEIGHT = 1080;
    const int FPS = 30;

    const string FRAGMENT_SHADER_PATH = "shaders/fragment.frag";
    const string AUDIO_PATH = "resources/bad_apple.wav";
    const string IMAGES_PATH = "../../../resources/image_sequence";

    private static void Main(string[] args)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Bad Apple");
        Raylib.SetTargetFPS(FPS);
        Raylib.InitAudioDevice();

        BadAppleShader shader = BadAppleShader.Load(FRAGMENT_SHADER_PATH);
        Sound audio = Raylib.LoadSound(AUDIO_PATH);

        string[] frameFiles = Directory
            .GetFiles(IMAGES_PATH, "*.png")
            .OrderBy(path => int.Parse(Regex.Match(path, @"\d+").Value))
            .ToArray();

        State state = new()
        {
            CurrentFrame = 0,
            IsPaused = false,
            Quality = ShaderQuality.Medium
        };

        Raylib.SetSoundVolume(audio, 0.5f);
        Raylib.PlaySound(audio);

        while (!Raylib.WindowShouldClose())
        {
            Texture2D texture = Raylib.LoadTexture(frameFiles[state.CurrentFrame]);

            int width = Raylib.GetScreenWidth();
            int height = Raylib.GetScreenHeight();

            switch (Raylib.GetKeyPressed())
            {
                case (int)KeyboardKey.S:
                    shader.ToggleSoftShadows();
                    break;

                case (int)KeyboardKey.Up:
                    shader.ShadowSamples++;
                    break;

                case (int)KeyboardKey.Down:
                    shader.ShadowSamples--;
                    break;

                case (int)KeyboardKey.Left:
                    shader.StepSize--;
                    break;

                case (int)KeyboardKey.Right:
                    shader.StepSize++;
                    break;

                case (int)KeyboardKey.Space:
                    state.IsPaused = !state.IsPaused;
                    if (state.IsPaused)
                    {
                        Raylib.PauseSound(audio);
                    }
                    else
                    {
                        Raylib.ResumeSound(audio);
                    }
                    break;

                case (int)KeyboardKey.Enter:
                    state.Quality = state.Quality switch
                    {
                        ShaderQuality.Low => ShaderQuality.Medium,
                        ShaderQuality.Medium => ShaderQuality.High,
                        ShaderQuality.High => ShaderQuality.Low,
                        _ => state.Quality
                    };
                    shader.SetQualityPreset(state.Quality);
                    break;
            }

            shader.Time = (float)Raylib.GetTime();
            shader.TexSize = new Vector2(texture.Width, texture.Height);
            shader.LightPos = RaylibUtils.GetMousePositionInTexture(texture);

            // Draw
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);
            Raylib.BeginShaderMode(shader.Shader);
            RaylibUtils.DrawTextureFit(texture);
            Raylib.EndShaderMode();
            Raylib.EndDrawing();
            Raylib.UnloadTexture(texture);

            if (!state.IsPaused)
            {
                state.CurrentFrame = (state.CurrentFrame + 1) % (ulong)frameFiles.Length;
            }

            Console.WriteLine($"Frame Time: {Raylib.GetFrameTime() * 1000}ms");
        }

        Raylib.UnloadShader(shader.Shader);
        Raylib.CloseWindow();
    }

    private struct State
    {
        internal ulong CurrentFrame;
        internal bool IsPaused;
        internal ShaderQuality Quality;
    }
}