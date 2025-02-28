﻿#region Using Statements
using MahApps.Metro.IconPacks;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Automation;
using System.Windows.Shapes;
#endregion
namespace sShell
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    #region Icon / hWnd
    public class IconHandler
    {
        public static ImageSource GetIcon(string path, bool smallIcon, bool isDirectory)
        {
            // SHGFI_USEFILEATTRIBUTES takes the file name and attributes into account if it doesn't exist
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            if (smallIcon)
                flags |= SHGFI_SMALLICON;

            uint attributes = FILE_ATTRIBUTE_NORMAL;
            if (isDirectory)
                attributes |= FILE_ATTRIBUTE_DIRECTORY;

            if (0 != SHGetFileInfo(
                        path,
                        attributes,
                        out SHFILEINFO shfi,
                        (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
                        flags))
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            shfi.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
            }
            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32", CharSet = CharSet.Unicode)]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        private const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        private const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        private const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        private const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;

        private const uint SHGFI_ICON = 0x000000100;     // get icon
        private const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
        private const uint SHGFI_TYPENAME = 0x000000400;     // get type name
        private const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes
        private const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location
        private const uint SHGFI_EXETYPE = 0x000002000;     // return exe type
        private const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
        private const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
        private const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state
        private const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes
        private const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
        private const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
        private const uint SHGFI_OPENICON = 0x000000002;     // get open icon
        private const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon
        private const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute

    }
    #endregion



    public partial class MainWindow : Window
    {
        #region more hWnd
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
    IntPtr hWnd,
    IntPtr hWndInsertAfter,
    int X,
    int Y,
    int cx,
    int cy,
    uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        static readonly IntPtr HWND_BOTTOM = new(1);
        #endregion

        public static Classes.SettingsHandler Settings = new();

        #region more things
        public static void SendWpfWindowBack(object sender, EventArgs e)
        {
            Window window = sender as Window;
            var hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }
        public static void SendWpfWindowBackUsingObj(Window window)
        {
            var hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }
        public NotificationHandler notifHandler;
        public bool MenuOpen = false;
        public Process currproc;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            notifHandler = new NotificationHandler();
            notifHandler.setMW(this);
            DispatcherTimer timer = new(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                var now = DateTime.Now;

                static string GetDaySuffix(int day)
                {
                    return day switch
                    {
                        1 or 21 or 31 => "st",
                        2 or 22 => "nd",
                        3 or 23 => "rd",
                        _ => "th",
                    };
                }
                this.dateText.Text = now.ToString("MMMM dd") + GetDaySuffix(now.Day) + " — " + now.ToString("hh:mm tt");
            }, Dispatcher);
            #region Init
            Window w = new()
            {
                Top = -100, // Location of new window is outside of visible part of screen
                Left = -100,
                Width = 1, // size of window is enough small to avoid its appearance at the beginning
                Height = 1,

                WindowStyle = WindowStyle.ToolWindow, // Set window style as ToolWindow to avoid its icon in AltTab 
                ShowInTaskbar = false
            }; // Create helper window
            w.Show(); // We need to show window before set is as owner to our main window
            this.Owner = w; // Okay, this will result to disappear icon for main window.
            w.Hide(); // Hide helper window just in case
            #endregion

        }
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            // foreach (string value in Localization.Language.en_US.Values)
            // {
            //     MessageBox.Show(value);
            // }
            Classes.DesktopHelper helper = new();
            helper.ShowDesktop();
            // foreach (Window win in Application.Current.Windows)
            // {
            //     if (win.Tag != null)
            //     {
            //         notifHandler.ShowNotification(new Notification() { Title = "Found a window!", Description = win.Tag.ToString(), Type = NotificationType.Default });
            //     }
            //     else
            //     {
            //         notifHandler.ShowNotification(new Notification() { Title = "Found a window!", Description = win.Title, Type = NotificationType.Default });
            //     }
            //     // SendWpfWindowBack(win);
            // }
            // Below is causing a long delay, so it's commented out for now..

            //MenuApps.Children.Clear();
            //string[] files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Microsoft\Windows\Start Menu\Programs", "*", SearchOption.AllDirectories);
            //
            //foreach (string file in files)
            //{
            //    /*
            //     <Border Style="{DynamicResource TBitemPanelBdr}">
            //        <WrapPanel Style="{DynamicResource TBitemPanel}">
            //            <Image Style="{DynamicResource TBitemIcon}" Source="https://cdn.discordapp.com/attachments/767080494269333504/835050490936557568/about-logo2x.png"  />
            //            <TextBlock Style="{DynamicResource TBitemText}" Text="Firefox" />
            //        </WrapPanel>
            //     </Border> 
            //      */
            //    Border border = new()
            //    {
            //        Style = FindResource("TBitemPanelBdr") as Style
            //    };
            //    WrapPanel wrap = new()
            //    {
            //        Style = FindResource("TBitemPanel") as Style
            //    };
            //    System.Windows.Controls.Image image = new()
            //    {
            //        Style = FindResource("TBitemIcon") as Style,
            //        Source = IconHandler.GetIcon(file, false, false),
            //    };
            //    string fixedText = file.Replace(Environment.SpecialFolder.ApplicationData + @"\Microsoft\Windows\Start Menu\Programs", "YES");
            //    TextBlock text = new()
            //    {
            //        Style = FindResource("TBitemText") as Style,
            //        Text = fixedText
            //    };
            //    wrap.Children.Add(image);
            //    wrap.Children.Add(text);
            //    border.Child = wrap;
            //    MenuApps.Children.Add(border);
            //}
            Classes.ProcessHelper ph = new();
            ph.setMW(this);
            if (ph.GetCurrentProcesses())
            {
                // good
                // #if DEBUG
                //                 MessageBox.Show("good");
                // #endif
                notifHandler.ShowNotification(new Notification() { Title = "Welcome!", Type = NotificationType.Default, Description = "Welcome to sShell, a simple WPF shell created for fun.\nSome programs may ask for admin privledges." });
            }
            else
            {
                // bad
                // #if DEBUG
                //                 MessageBox.Show("bad");
                // #endif
                notifHandler.ShowNotification(new Notification() { Title = "Error", Type = NotificationType.Error, Description = "Oops, an error occured. Please report this to the developers.." });
            }
        }

        public bool AddTaskbarItem(Border itm)
        {
            try
            {
                TaskbarIcons.Items.Add(itm);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void FocusWin(object sender, MouseEventArgs e)
        {
            Border bdr = sender as Border;
            notifHandler.ShowNotification(new Notification() { Title = "Alert", Description = string.Format("Got Border with Tag {0}", bdr.Tag.ToString()), Type = NotificationType.Default });
            Process process = Process.GetProcessById(int.Parse(bdr.Tag.ToString()));
            try
            {
                Notification notif = new()
                {
                    Title = "Alert",
                    Description = string.Format("Attempting to open {1} ({0})!", bdr.Tag.ToString(), process.ProcessName),
                    Type = NotificationType.Default
                };
                notifHandler.ShowNotification(notif);
                if (!String.IsNullOrEmpty(process.MainWindowTitle))
                {
                    AutomationElement element = AutomationElement.FromHandle(process.MainWindowHandle);
                    if (element != null)
                    {
                        element.SetFocus();
                    }
                    // SetForegroundWindow(process.MainWindowHandle);
                }
            }
            catch (Exception error)
            {
                Notification notif = new()
                {
                    Title = "An error has occured!",
                    Description = string.Format("Failed to open the process: {0}", error.Message),
                    Type = NotificationType.Error
                };
                notifHandler.ShowNotification(notif);
            }
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var windowHwnd = new WindowInteropHelper(this).Handle;
            WindowsServices.SetWindowExTransparent(windowHwnd);
            var tbHwndSource = (HwndSource)HwndSource.FromVisual(tb);
            var tbHwnd = tbHwndSource.Handle;
            WindowsServices.SetWindowExNotTransparent(tbHwnd);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            QuickActions.Visibility = Visibility.Collapsed;
        }

        private void ToggleMenu(object sender, MouseButtonEventArgs e)
        {
            if (MenuOpen)
            {
                Menu.Visibility = Visibility.Collapsed;
                MenuOpen = false;
            }
            else
            {
                Menu.Visibility = Visibility.Visible;
                MenuOpen = true;
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            // hide all taskbar icons stuff
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            MenuOpen = false;
        }

        #region whdlrs / Window Handlers

        private void whdlrs_openSettings(object sender, MouseButtonEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            MenuOpen = false;
            Windows.SettingsWin settings = new();
            settings.Show();
        }

        private void whdlrs_openExplorer(object sender, MouseButtonEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            MenuOpen = false;
            Windows.Explorer explorer = new();
            explorer.setMW(this);
            explorer.Show();
        }

        private void whdlrs_openQA(object sender, MouseButtonEventArgs e)
        {
            Menu.Visibility = Visibility.Collapsed;
            MenuOpen = false;
            if (QuickActions.Visibility == Visibility.Collapsed)
            {
                QuickActions.Visibility = Visibility.Visible;
                NotifAmountText.Text = "0";
                NotifAmount.Visibility = Visibility.Visible;
                NotifAmount.Visibility = Visibility.Hidden;
            }
            else
            {
                QuickActions.Visibility = Visibility.Collapsed;
            }
        }

        // add async when uncommenting bottom
        private void whdlrs_takeScreenshot(object sender, MouseButtonEventArgs e)
        {
            screenshotCapturePopup.Visibility = Visibility.Hidden;
            Menu.Visibility = Visibility.Collapsed;
            MenuOpen = false;
            System.Drawing.Image capture = Classes.ScreenCapture.CaptureScreen();
            screenshotCapturePopup.Visibility = Visibility.Visible;
            screenshotCaptureImg.Source = Classes.MiscClasses.GetImageStream(capture);
            screenshotCaptureDate.Text = DateTime.Now.ToString("hh:mm tt");
            Clipboard.SetImage((BitmapSource)screenshotCaptureImg.Source);
            // Temporarily disabled while i fix some stuff

            // await Task.Delay(3000);
            // screenshotCapturePopup.Visibility = Visibility.Collapsed;
        }

        // clear notifications button

        private void whdlrs_QAclearNotifs(object sender, MouseButtonEventArgs e)
        {
            qaStackParent.Children.Clear();
        }

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }


    }

    public static class WindowsServices
    {
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public static void SetWindowExNotTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            _ = SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }
    }
}
