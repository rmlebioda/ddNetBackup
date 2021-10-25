using System;
using ddNetBackupGuiGtk.Views;
using Gtk;

namespace ddNetBackupGuiGtk
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.Init();

            var app = new Application("org.ddNetBackupGuiGtk.ddNetBackupGuiGtk", GLib.ApplicationFlags.None);
            app.Register(GLib.Cancellable.Current);

            MainWindow win;
            try
            {
                win = new MainWindow();
            }
            catch (Exception e)
            {
#if DEBUG
                Console.WriteLine(e);
#endif
                return;
            }
            app.AddWindow(win);

            win.Show();
            Application.Run();
        }
    }
}
