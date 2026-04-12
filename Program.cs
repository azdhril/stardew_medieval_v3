using System;
using System.IO;

// Redirect Console to debug.log next to the exe so logs survive WinExe stdout detachment.
// View live with: Get-Content -Wait debug.log   (PowerShell)
var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
var logWriter = new StreamWriter(logStream) { AutoFlush = true };
Console.SetOut(logWriter);
Console.SetError(logWriter);
Console.WriteLine($"[Program] Log started {DateTime.Now:O}");

try
{
    using var game = new stardew_medieval_v3.Game1();
    game.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"[Program] FATAL: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[Program] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        Console.WriteLine(ex.InnerException.StackTrace);
    }
    throw;
}
