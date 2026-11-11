namespace SplendorFileBinder;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            string logPath = Path.Combine(
                Path.GetDirectoryName(Application.ExecutablePath) ?? ".",
                "crash.log");
            File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}");
            MessageBox.Show(ex.ToString(), "Splendor File Binder - Fatal Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
