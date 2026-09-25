using Raylib_cs;

namespace Application;

internal class PlaybackClock
{
    private readonly Music? music;
    private readonly float length;
    private float silentTime;

    public bool IsPaused { get; private set; }

    public float Time => music is Music m ? Raylib.GetMusicTimePlayed(m) : silentTime;

    public PlaybackClock(Music? music, float videoLength)
    {
        this.music = music;
        length = videoLength;

        if (music is Music m)
        {
            length = Raylib.GetMusicTimeLength(m);
            Raylib.SetMusicVolume(m, 0.5f);
            Raylib.PlayMusicStream(m);
        }
    }

    public void Update(float deltaTime)
    {
        if (music is Music m)
        {
            Raylib.UpdateMusicStream(m);
        }
        else if (!IsPaused)
        {
            silentTime = (silentTime + deltaTime) % length;
        }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;

        if (music is Music m)
        {
            if (IsPaused)
            {
                Raylib.PauseMusicStream(m);
            }
            else
            {
                Raylib.ResumeMusicStream(m);
            }
        }
    }

    public void Seek(float offsetSeconds)
    {
        float position = Math.Clamp(Time + offsetSeconds, 0f, length);

        if (music is Music m)
        {
            Raylib.SeekMusicStream(m, position);
        }
        else
        {
            silentTime = position % length;
        }
    }

    public void Unload()
    {
        if (music is Music m)
        {
            Raylib.UnloadMusicStream(m);
        }
    }
}
