namespace SharpForge;

internal static class Program
{
    /// <summary>
    /// Configures WinForms defaults and starts the main application window.
    /// </summary>
    /// <param name="args">Optional image or directory paths to queue at startup.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormMain(args));
    }
}
