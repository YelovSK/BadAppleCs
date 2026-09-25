using Application;
using Raylib_cs;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal class Program
{
    const int SCREEN_WIDTH = 1440;
    const int SCREEN_HEIGHT = 1080;
    const int FPS = 30;
    const float SEEK_SECONDS = 5f;
    const int LIGHTING_SCALE = 1;

    const string FRAGMENT_SHADER_PATH = "shaders/fragment.frag";
    const string JFA_SEED_SHADER_PATH = "shaders/jfa_seed.frag";
    const string JFA_STEP_SHADER_PATH = "shaders/jfa_step.frag";
    const string AUDIO_PATH = "resources/bad_apple.wav";
    const string IMAGES_PATH = "resources/image_sequence";

    private static void Main(string[] args)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(SCREEN_WIDTH, SCREEN_HEIGHT, "Bad Apple");
        // Raylib.SetTargetFPS(Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor()));
        Raylib.SetTargetFPS(120);
        Raylib.InitAudioDevice();

        if (!File.Exists(FRAGMENT_SHADER_PATH))
        {
            Console.WriteLine($"Error: Shader file not found at {Path.GetFullPath(FRAGMENT_SHADER_PATH)}");
            return;
        }

        BadAppleShader shader = BadAppleShader.Load(FRAGMENT_SHADER_PATH);

        if (!File.Exists(AUDIO_PATH))
        {
            Console.WriteLine($"Error: Audio file not found at {Path.GetFullPath(AUDIO_PATH)}");
            return;
        }

        Music music = Raylib.LoadMusicStream(AUDIO_PATH);

        if (!Directory.Exists(IMAGES_PATH))
        {
            Console.WriteLine($"Error: Images directory not found at {Path.GetFullPath(IMAGES_PATH)}");
            return;
        }

        string[] frameFiles = Directory
            .GetFiles(IMAGES_PATH, "*.png")
            .OrderBy(path => int.Parse(Regex.Match(path, @"\d+").Value))
            .ToArray();

        if (frameFiles.Length == 0)
        {
            Console.WriteLine("Error: No PNG images found in the images directory.");
            return;
        }

        State state = new()
        {
            CurrentFrame = 0,
            IsPaused = false,
            Quality = ShaderQuality.Medium
        };

        // Preload first frame
        Image nextImage = Raylib.LoadImage(frameFiles[0]);
        Texture2D currentTexture = Raylib.LoadTextureFromImage(nextImage);
        Raylib.UnloadImage(nextImage);

        RenderTexture2D lightingTarget = Raylib.LoadRenderTexture(
            currentTexture.Width * LIGHTING_SCALE,
            currentTexture.Height * LIGHTING_SCALE);
        Raylib.SetTextureFilter(lightingTarget.Texture, TextureFilter.Bilinear);

        JumpFlood jumpFlood = new(JFA_SEED_SHADER_PATH, JFA_STEP_SHADER_PATH, currentTexture.Width, currentTexture.Height);
        jumpFlood.Update(currentTexture);

        int prefetchedFrame = 1 % frameFiles.Length;
        Task<Image> imageLoadTask = Task.Run(() => Raylib.LoadImage(frameFiles[prefetchedFrame]));

        Raylib.SetMusicVolume(music, 0.5f);
        Raylib.PlayMusicStream(music);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateMusicStream(music);

            // The video follows the audio clock, which also takes care of pausing and seeking
            int targetFrame = (int)(Raylib.GetMusicTimePlayed(music) * FPS) % frameFiles.Length;
            if (targetFrame != state.CurrentFrame)
            {
                Image image = imageLoadTask.Result;
                if (prefetchedFrame != targetFrame)
                {
                    // Frames were skipped (seek or lag), so the prefetched one is useless
                    Raylib.UnloadImage(image);
                    image = Raylib.LoadImage(frameFiles[targetFrame]);
                }

                Raylib.UnloadTexture(currentTexture);
                currentTexture = Raylib.LoadTextureFromImage(image);
                Raylib.UnloadImage(image);
                jumpFlood.Update(currentTexture);

                state.CurrentFrame = targetFrame;

                int frameToLoad = (targetFrame + 1) % frameFiles.Length;
                prefetchedFrame = frameToLoad;
                imageLoadTask = Task.Run(() => Raylib.LoadImage(frameFiles[frameToLoad]));
            }

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
                    Seek(music, -SEEK_SECONDS);
                    break;

                case (int)KeyboardKey.Right:
                    Seek(music, SEEK_SECONDS);
                    break;

                case (int)KeyboardKey.Space:
                    state.IsPaused = !state.IsPaused;
                    if (state.IsPaused)
                    {
                        Raylib.PauseMusicStream(music);
                    }
                    else
                    {
                        Raylib.ResumeMusicStream(music);
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
            shader.TexSize = new Vector2(currentTexture.Width, currentTexture.Height);
            shader.LightPos = RaylibUtils.GetMousePositionInTexture(currentTexture);

            Raylib.BeginTextureMode(lightingTarget);
            Raylib.BeginShaderMode(shader.Shader);
            shader.BindSeedTexture(jumpFlood.Result);
            Raylib.DrawTexturePro(
                currentTexture,
                new Rectangle(0, 0, currentTexture.Width, currentTexture.Height),
                new Rectangle(0, 0, lightingTarget.Texture.Width, lightingTarget.Texture.Height),
                Vector2.Zero, 0f, Color.Black);
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            RaylibUtils.DrawRenderTextureFit(lightingTarget);
            Raylib.EndDrawing();
        }

        Raylib.UnloadTexture(currentTexture);
        Raylib.UnloadRenderTexture(lightingTarget);
        jumpFlood.Unload();
        Raylib.UnloadShader(shader.Shader);
        Raylib.UnloadMusicStream(music);
        Raylib.CloseWindow();
    }

    private static void Seek(Music music, float offsetSeconds)
    {
        float position = Raylib.GetMusicTimePlayed(music) + offsetSeconds;
        Raylib.SeekMusicStream(music, Math.Clamp(position, 0f, Raylib.GetMusicTimeLength(music)));
    }

    private struct State
    {
        internal int CurrentFrame;
        internal bool IsPaused;
        internal ShaderQuality Quality;
    }
}