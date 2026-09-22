using System;
using ClosedXML.Excel;

namespace ForamEcoQS
{
    /// <summary>
    /// Shared colours for Ecological Quality Status classifications. Keeping the Excel palette
    /// here makes exported workbooks match the classifications displayed by the application.
    /// </summary>
    public static class EqsClassificationPalette
    {
        public static bool TryGetRgb(string classification, out int red, out int green, out int blue)
        {
            switch (classification?.Trim())
            {
                case "High":
                case "Suitable for coral growth":
                    red = 0;
                    green = 128;
                    blue = 0;
                    return true;
                case "Good":
                    red = 144;
                    green = 238;
                    blue = 144;
                    return true;
                case "Moderate":
                case "Marginal conditions":
                    red = 255;
                    green = 255;
                    blue = 0;
                    return true;
                case "Poor":
                    red = 255;
                    green = 165;
                    blue = 0;
                    return true;
                case "Bad":
                case "Unsuitable for coral growth":
                    red = 255;
                    green = 0;
                    blue = 0;
                    return true;
                default:
                    red = green = blue = 0;
                    return false;
            }
        }

        public static bool UsesLightText(string classification) =>
            string.Equals(classification, "Bad", StringComparison.Ordinal) ||
            string.Equals(classification, "Poor", StringComparison.Ordinal) ||
            string.Equals(classification, "Unsuitable for coral growth", StringComparison.Ordinal);

        public static void ApplyToExcelCell(IXLCell cell, string classification)
        {
            if (!TryGetRgb(classification, out int red, out int green, out int blue))
            {
                return;
            }

            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(red, green, blue);
            cell.Style.Font.FontColor = UsesLightText(classification) ? XLColor.White : XLColor.Black;
        }
    }
}
