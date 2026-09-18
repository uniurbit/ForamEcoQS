using System;
using System.Windows;
using System.Windows.Threading;

namespace ForamEcoQS.Wpf
{
    public static class PlatformStyles
    {
        public static void Register()
        {
            Eto.Style.Add<Eto.Forms.TabControl>("advanced-results-tabs", tabs =>
            {
                var nativeTabs = (FrameworkElement)tabs.ControlObject;
                nativeTabs.Loaded += (sender, args) =>
                {
                    // Eto measures all pages before Loaded, but only the selected
                    // page afterwards. Discard that initial measurement: otherwise
                    // the tab content can extend below the window until a resize.
                    nativeTabs.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (!nativeTabs.IsLoaded)
                            return;
                        nativeTabs.InvalidateMeasure();
                        nativeTabs.InvalidateArrange();
                        nativeTabs.UpdateLayout();
                    }));
                };
            });
        }
    }
}
