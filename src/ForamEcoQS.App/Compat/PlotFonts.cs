//MIT License
// PlotFonts.cs - Picks a plot font family that actually exists on the current system.
//
// The WinForms build hard-coded "Arial", and OxyPlot itself defaults to "Segoe UI". Neither is
// guaranteed outside Windows: on GTK an unknown family makes FontFamily.Create throw, and the
// whole plot renders as an exception message. Resolving the family once, against the fonts the
// platform reports, keeps the intended look on Windows and stays safe on Linux and macOS.

using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;
using OxyPlot;

namespace ForamEcoQS.Compat
{
    public static class PlotFonts
    {
        private static string _family;

        /// <summary>Preferred families, most desirable first. All are metric-compatible sans serifs.</summary>
        private static readonly string[] Preferred =
        {
            "Arial",
            "Liberation Sans",
            "Helvetica",
            "Nimbus Sans",
            "DejaVu Sans",
            "Segoe UI",
            "Noto Sans",
            "Ubuntu",
            "Cantarell"
        };

        /// <summary>A sans-serif family known to be installed on this system.</summary>
        public static string Family
        {
            get
            {
                if (_family != null)
                {
                    return _family;
                }

                HashSet<string> available;
                try
                {
                    available = new HashSet<string>(
                        Fonts.AvailableFontFamilies.Select(f => f.Name),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                _family = Preferred.FirstOrDefault(available.Contains);

                if (_family == null)
                {
                    try
                    {
                        _family = SystemFonts.Default().FamilyName;
                    }
                    catch (Exception)
                    {
                        _family = FontFamilies.SansFamilyName;
                    }
                }

                return _family;
            }
        }

        /// <summary>
        /// Makes sure a model (and any sub-model font it carries) uses a resolvable family.
        /// Safe to call repeatedly.
        /// </summary>
        public static PlotModel ApplyTo(PlotModel model)
        {
            if (model == null)
            {
                return null;
            }

            string family = Family;

            if (!IsUsable(model.DefaultFont))
            {
                model.DefaultFont = family;
            }
            if (!IsUsable(model.TitleFont))
            {
                model.TitleFont = family;
            }
            if (!IsUsable(model.SubtitleFont))
            {
                model.SubtitleFont = family;
            }

            foreach (var axis in model.Axes)
            {
                if (!IsUsable(axis.Font))
                {
                    axis.Font = family;
                }
                if (!IsUsable(axis.TitleFont))
                {
                    axis.TitleFont = family;
                }
            }

            foreach (var legend in model.Legends)
            {
                if (!IsUsable(legend.LegendFont))
                {
                    legend.LegendFont = family;
                }
                if (!IsUsable(legend.LegendTitleFont))
                {
                    legend.LegendTitleFont = family;
                }
            }

            return model;
        }

        /// <summary>Null and empty mean "inherit", which is fine; a named family must exist.</summary>
        private static bool IsUsable(string family)
        {
            if (string.IsNullOrEmpty(family))
            {
                return true;
            }

            return string.Equals(family, Family, StringComparison.OrdinalIgnoreCase);
        }
    }
}
