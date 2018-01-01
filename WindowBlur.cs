using Native;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Shell;

namespace WindowBlur
{
    public class WindowBlur
    {
        static Window Window;

        public static void Enable(Window window)
        {
            Window = window;
            Window.SourceInitialized += OnSourceInitialized;
        }

        private static void OnSourceInitialized(object sender, EventArgs e)
        {
            ((Window)sender).SourceInitialized -= OnSourceInitialized;
            try { NewBlurEnable(Window); } catch { try { OldBlurEnable(Window); } catch { } }
        }

        private static void NewBlurEnable(Window window)
        {
            AccentPolicy accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };
            IntPtr accentPolicyPointer = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy));
            Marshal.StructureToPtr(accentPolicy, accentPolicyPointer, false);
            WindowCompositionAttributeData windowCompositionAttributeData =
                new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPolicyPointer,
                    SizeOfData = Marshal.SizeOf(accentPolicy)
                };

            const int SM_CXFOCUSBORDER = 83;
            const int SM_CYFOCUSBORDER = 84;
            double WindowBorderX = NativeMethods.GetSystemMetrics(SM_CXFOCUSBORDER) /
                PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M22;
            double WindowBorderY = NativeMethods.GetSystemMetrics(SM_CYFOCUSBORDER) /
                PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M22;

                UIElement content = window.Content as UIElement;
                window.Content = new Grid()
                {
                    Margin = new Thickness
                    {
                        Bottom = WindowBorderY,
                        Left = WindowBorderX,
                        Right = WindowBorderX,
                        Top = 0
                    }
                };
                ((Grid)window.Content).RowDefinitions.Add(new RowDefinition
                { Height = new GridLength(GetWindowTitleHeight(window)) });
                ((Grid)window.Content).RowDefinitions.Add(new RowDefinition
                { Height = new GridLength(1, GridUnitType.Star) });
                ((Grid)window.Content).Children.Add(content);
                Grid.SetRow(((Grid)window.Content).Children[0], 1);
                NativeMethods.SetWindowCompositionAttribute(
                    new WindowInteropHelper(window).Handle, ref windowCompositionAttributeData);
            
            WindowChrome.SetWindowChrome(window, new WindowChrome
            {
                GlassFrameThickness = new Thickness
                {
                    Bottom = WindowBorderY,
                    Left = WindowBorderX,
                    Right = WindowBorderX,
                    Top = GetWindowTitleHeight(window)
                },
                CaptionHeight = GetCaptionHeight(window)
            });
            Marshal.FreeHGlobal(accentPolicyPointer);
        }

        private static void OldBlurEnable(Window window)
        {

        }

        private static double GetWindowTitleHeight(Window window)
        {
            const int SM_CYCAPTION = 4;
            const int SM_CYFRAME = 33;
            const int SM_CXPADDEDBORDER = 92;
            return (NativeMethods.GetSystemMetrics(SM_CYCAPTION) +
                NativeMethods.GetSystemMetrics(SM_CYFRAME) +
                NativeMethods.GetSystemMetrics(SM_CXPADDEDBORDER)) /
                PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M22;
        }

        private static double GetCaptionHeight(Window window)
        {
            const int SM_CYCAPTION = 4;
            const int SM_CXPADDEDBORDER = 92;
            return (NativeMethods.GetSystemMetrics(SM_CYCAPTION) +
                NativeMethods.GetSystemMetrics(SM_CXPADDEDBORDER)) /
                PresentationSource.FromVisual(window).CompositionTarget.TransformToDevice.M22;
        }

    }


}