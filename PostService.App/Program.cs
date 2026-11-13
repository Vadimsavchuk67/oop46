using System.Diagnostics;
using System.Reflection;
namespace PostService.App;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        ApplicationConfiguration.Initialize();
        Application.Run(new PostingList());
    }
    private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
    {
        var error = e.ExceptionObject as Exception;
        while (error is TargetInvocationException && error.InnerException != null)
            error = error.InnerException;
        Debug.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {error}");
        MessageBox.Show("Сталася непередбачена помилка. Подробиці передано у вікно Debug. Застосунок буде закрито.",
            "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
