using System;
using System.IO;
using System.Text;

// Tee Console output to BOTH the real stdout (terminal where `dotnet run`
// was launched) AND debug.log next to the exe. AutoFlush on both so Git Bash
// / mintty doesn't block-buffer the stdout stream.
var logPath = Path.Combine(AppContext.BaseDirectory, "debug.log");
var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
var logWriter = new StreamWriter(logStream) { AutoFlush = true };

var stdoutWriter = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
var teeWriter = new TeeWriter(stdoutWriter, logWriter);
Console.SetOut(teeWriter);
Console.SetError(teeWriter);
Console.WriteLine($"[Program] Log started {DateTime.Now:O}  (tee -> stdout + {logPath})");

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

/// <summary>Writes to two TextWriters at once so Console output reaches both
/// the terminal and debug.log. AutoFlush on both underlying writers keeps Git
/// Bash / mintty from block-buffering stdout.</summary>
file sealed class TeeWriter : TextWriter
{
    private readonly TextWriter _a;
    private readonly TextWriter _b;
    public TeeWriter(TextWriter a, TextWriter b) { _a = a; _b = b; }
    public override Encoding Encoding => _a.Encoding;
    public override void Write(char value) { _a.Write(value); _b.Write(value); }
    public override void Write(string? value) { _a.Write(value); _b.Write(value); }
    public override void WriteLine(string? value) { _a.WriteLine(value); _b.WriteLine(value); }
    public override void Flush() { _a.Flush(); _b.Flush(); }
}
