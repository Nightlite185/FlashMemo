using System.Windows.Media;
namespace FlashMemo.Helpers;

public static class ColorExtensions
{
    ///<summary>Converts a Color to an int in 0xAARRGGBB format.</summary>
    public static int ToInt(this Color color)
    {
        return (color.A << 24)
             | (color.R << 16)
             | (color.G << 8)
             |  color.B;
    }

    ///<summary>Creates a Color from an int in 0xAARRGGBB format.</summary>
    public static Color ToColor(this int argb)
    {
        byte a = (byte)((argb >> 24) & 0xFF);
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8)  & 0xFF);
        byte b = (byte)( argb        & 0xFF);

        return Color.FromArgb(a, r, g, b);
    }
}