namespace RawrZDesktop;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Test IDE functionality first
        Console.WriteLine("Testing RawrZ IDE Core Functionality...\n");
        IDETestConsole.RunAllTests();
        Console.WriteLine("\nPress any key to start Windows Forms application...");
        Console.ReadKey();
        
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}