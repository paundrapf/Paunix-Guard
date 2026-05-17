using System.Windows;
using PaunixGuard.Updater.Velopack;
using Velopack;

namespace PaunixGuard.App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .SetAutoApplyOnStartup(false)
            .Run();
        VelopackUpdateService.MarkStartupHooksRun();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
