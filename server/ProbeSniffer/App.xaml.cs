using System.Windows;

namespace ProbeSniffer
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Program program = new Program();
            program.Main();
        }
    }
}
