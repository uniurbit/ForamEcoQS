//MIT License
// PlotExport.cs - Rendering and export helpers shared by the plotting windows.
//
// The WinForms original captured composite panels with Control.DrawToBitmap, which has no
// cross-platform equivalent. Here the plot models are re-rendered off-screen instead, which
// also gives a resolution-independent export.

using System;
using System.Collections.Generic;
using System.IO;
using Eto.Drawing;
using OxyPlot;
using EtoPngExporter = OxyPlot.Eto.PngExporter;
using ForamEcoQS.Compat;
using ImageFormat = Eto.Drawing.ImageFormat;

namespace ForamEcoQS
{
    public static class PlotExport
    {
        /// <summary>Renders a single plot model to an Eto bitmap at the requested pixel size.</summary>
        public static Bitmap RenderToBitmap(PlotModel model, int width, int height, double resolution = 96)
        {
            PlotFonts.ApplyTo(model);

            var exporter = new EtoPngExporter
            {
                Width = Math.Max(1, width),
                Height = Math.Max(1, height),
                Resolution = resolution
            };
            return exporter.ExportToBitmap(model);
        }

        /// <summary>
        /// Lays several plot models out on a grid and renders them into a single bitmap,
        /// replacing the WinForms "screenshot the TableLayoutPanel" export.
        /// </summary>
        public static Bitmap ComposeToBitmap(IReadOnlyList<PlotModel> models, int columns, int cellWidth, int cellHeight)
        {
            if (models == null || models.Count == 0)
            {
                throw new ArgumentException("At least one plot is required.", nameof(models));
            }

            columns = Math.Max(1, Math.Min(columns, models.Count));
            int rows = (int)Math.Ceiling(models.Count / (double)columns);

            var composite = new Bitmap(columns * cellWidth, rows * cellHeight, PixelFormat.Format32bppRgb);
            using (var graphics = new Graphics(composite))
            {
                graphics.Clear(Colors.White);

                for (int i = 0; i < models.Count; i++)
                {
                    int column = i % columns;
                    int row = i / columns;

                    using var tile = RenderToBitmap(models[i], cellWidth, cellHeight);
                    graphics.DrawImage(tile, column * cellWidth, row * cellHeight);
                }

                graphics.Flush();
            }

            return composite;
        }

        /// <summary>Saves a plot as PNG or JPEG at the requested DPI.</summary>
        public static void SaveAsImage(PlotModel model, string filePath, int dpi = 300)
        {
            int width = 800 * dpi / 96;
            int height = 600 * dpi / 96;

            using var bitmap = RenderToBitmap(model, width, height, dpi);
            bitmap.Save(filePath, FormatFor(filePath));
        }

        /// <summary>Saves several plots laid out on a grid as a single image.</summary>
        public static void SaveCompositeAsImage(IReadOnlyList<PlotModel> models, int columns, string filePath, int dpi = 300)
        {
            int cellWidth = 800 * dpi / 96;
            int cellHeight = 600 * dpi / 96;

            using var bitmap = ComposeToBitmap(models, columns, cellWidth, cellHeight);
            bitmap.Save(filePath, FormatFor(filePath));
        }

        /// <summary>Writes a plot to PDF using OxyPlot's built-in vector exporter.</summary>
        public static void SaveAsPdf(PlotModel model, string filePath, int width = 800, int height = 600)
        {
            PlotFonts.ApplyTo(model);

            using var stream = File.Create(filePath);
            var exporter = new PdfExporter { Width = width, Height = height };
            exporter.Export(model, stream);
        }

        /// <summary>Writes a plot to SVG using OxyPlot's built-in vector exporter.</summary>
        public static void SaveAsSvg(PlotModel model, string filePath, int width = 800, int height = 600)
        {
            PlotFonts.ApplyTo(model);

            using var stream = File.Create(filePath);
            var exporter = new SvgExporter { Width = width, Height = height };
            exporter.Export(model, stream);
        }

        private static ImageFormat FormatFor(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" ? ImageFormat.Jpeg : ImageFormat.Png;
        }
    }
}
