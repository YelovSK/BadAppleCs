using Application;
using Raylib_cs;
using System.Numerics;
using System.Text.RegularExpressions;

internal class Program
{
    const int FPS = 30;
    const float SEEK_SECONDS = 5f;
    const float VOLUME_STEP = 0.05f;

    // Lighting resolution relative to the area the video covers in the window.
    // E.g. fullscreen on a 4K monitor, the video covers 2880x2160, so 0.5 renders the lighting at 1440x1080.
    const float LIGHTING_SCALE = 1f;
    const float WHITE_THRESHOLD = 0.35f;

    const string FRAGMENT_SHADER_PATH = "shaders/fragment.frag";
    const string JFA_SEED_SHADER_PATH = "shaders/jfa_seed.frag";
    const string JFA_STEP_SHADER_PATH = "shaders/jfa_step.frag";

    const string AUDIO_PATH = "resources/bad_apple.wav";
    const string IMAGES_PATH = "resources/image_sequence";

    private static void Main(string[] args)
    {
        Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(1440, 1080, "Bad Apple");
        int refreshRate = Raylib.GetMonitorRefreshRate(Raylib.GetCurrentMonitor());
        Raylib.SetTargetFPS(Math.Min(refreshRate, 120));
        Raylib.InitAudioDevice();

        if (!File.Exists(FRAGMENT_SHADER_PATH))
        {
            Console.WriteLine($"Error: Shader file not found at {Path.GetFullPath(FRAGMENT_SHADER_PATH)}");
            return;
        }

        BadAppleShader shader = BadAppleShader.Load(FRAGMENT_SHADER_PATH);

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

        Music? music = null;
        if (File.Exists(AUDIO_PATH))
        {
            music = Raylib.LoadMusicStream(AUDIO_PATH);
        }
        else
        {
            Console.WriteLine($"Audio file not found at {Path.GetFullPath(AUDIO_PATH)}, playing without sound");
        }

        State state = new()
        {
            CurrentFrame = 0,
            Quality = ShaderQuality.Medium
        };

        // Preload first frame
        Image nextImage = Raylib.LoadImage(frameFiles[0]);
        Texture2D currentTexture = LoadFrameTexture(nextImage);
        Raylib.UnloadImage(nextImage);

        // Created in the loop, sized to the window
        RenderTexture2D lightingTarget = default;

        shader.WhiteThreshold = WHITE_THRESHOLD;
        JumpFlood jumpFlood = new(JFA_SEED_SHADER_PATH, JFA_STEP_SHADER_PATH, currentTexture.Width, currentTexture.Height, WHITE_THRESHOLD);
        jumpFlood.Update(currentTexture);

        int prefetchedFrame = 1 % frameFiles.Length;
        Task<Image> imageLoadTask = Task.Run(() => Raylib.LoadImage(frameFiles[prefetchedFrame]));

        PlaybackClock clock = new(music, (float)frameFiles.Length / FPS);

        while (!Raylib.WindowShouldClose())
        {
            clock.Update(Raylib.GetFrameTime());

            int targetFrame = (int)(clock.Time * FPS) % frameFiles.Length;
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
                currentTexture = LoadFrameTexture(image);
                Raylib.UnloadImage(image);
                jumpFlood.Update(currentTexture);

                state.CurrentFrame = targetFrame;

                int frameToLoad = (targetFrame + 1) % frameFiles.Length;
                prefetchedFrame = frameToLoad;
                imageLoadTask = Task.Run(() => Raylib.LoadImage(frameFiles[frameToLoad]));
            }

            clock.Volume += Raylib.GetMouseWheelMove() * VOLUME_STEP;

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
                    clock.Seek(-SEEK_SECONDS);
                    break;

                case (int)KeyboardKey.Right:
                    clock.Seek(SEEK_SECONDS);
                    break;

                case (int)KeyboardKey.Space:
                    clock.TogglePause();
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

            Rectangle videoArea = RaylibUtils.GetAspectFitRect(currentTexture, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            int lightingWidth = Math.Max(1, (int)(videoArea.Width * LIGHTING_SCALE));
            int lightingHeight = Math.Max(1, (int)(videoArea.Height * LIGHTING_SCALE));
            if (lightingTarget.Texture.Width != lightingWidth || lightingTarget.Texture.Height != lightingHeight)
            {
                Raylib.UnloadRenderTexture(lightingTarget);
                lightingTarget = Raylib.LoadRenderTexture(lightingWidth, lightingHeight);
                Raylib.SetTextureFilter(lightingTarget.Texture, TextureFilter.Bilinear);
            }

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
        clock.Unload();
        Raylib.CloseWindow();
    }

    private static Texture2D LoadFrameTexture(Image image)
    {
        Texture2D texture = Raylib.LoadTextureFromImage(image);
        // Bilinear for smooth edges
        Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
        return texture;
    }

    private struct State
    {
        internal int CurrentFrame;
        internal ShaderQuality Quality;
    }
}