using System;

namespace ProgrWPF
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.Run(new MainWindow());
        }
    }

    public class App : System.Windows.Application
    {
        // This is a simplified App class since we are not using XAML
    }
}
