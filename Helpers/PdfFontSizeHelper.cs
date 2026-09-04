namespace KarzounERP.Helpers;

public static class PdfFontSizeHelper
{
    public static double PreviewNormal(double baseSize) => baseSize;

    public static double PreviewTitle(double baseSize) => baseSize + 2;

    public static double PreviewSmall(double baseSize) => Math.Max(8, baseSize - 1);

    public static double PreviewTiny(double baseSize) => Math.Max(7, baseSize - 2);

    public static float ScaleFromDefaultNine(float sizeAtDefault9, double baseSize)
    {
        var clamped = Math.Clamp(baseSize, 6, 16);
        return (float)(clamped * sizeAtDefault9 / 9.0);
    }
}