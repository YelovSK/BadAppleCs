using Raylib_cs;
using System.Numerics;

namespace Application;

internal static class RaylibUtils
{
    public static Rectangle GetAspectFitRect(Texture2D texture, int screenW, int screenH)
    {
        float texAspect = (float)texture.Width / texture.Height;
        float screenAspect = (float)screenW / screenH;

        if (screenAspect > texAspect)
        {
            float scaledWidth = screenH * texAspect;
            return new Rectangle((screenW - scaledWidth) / 2, 0, scaledWidth, screenH);
        }
        else
        {
            float scaledHeight = screenW / texAspect;
            return new Rectangle(0, (screenH - scaledHeight) / 2, screenW, scaledHeight);
        }
    }

    public static void DrawTextureFit(Texture2D texture)
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        Rectangle src = new(0, 0, texture.Width, texture.Height);
        Rectangle dest = GetAspectFitRect(texture, screenW, screenH);

        Raylib.DrawTexturePro(texture, src, dest, Vector2.Zero, 0f, Color.Black);
    }

    public static Vector2 GetMousePositionInTexture(Texture2D texture)
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();
        Rectangle dest = GetAspectFitRect(texture, screenW, screenH);

        Vector2 mouse = Raylib.GetMousePosition();
        float u = (mouse.X - dest.X) / dest.Width;
        float v = (mouse.Y - dest.Y) / dest.Height;

        return new Vector2(u * texture.Width, v * texture.Height);
    }
}
