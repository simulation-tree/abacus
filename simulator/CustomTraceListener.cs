using System;
using System.Diagnostics;

public class CustomTraceListener : TextWriterTraceListener
{
    public CustomTraceListener(string? fileName) : base(fileName, "listener")
    {
    }

    public override void WriteLine(string? message)
    {
        base.WriteLine(BuildMessage("INFO", message));
    }

    public override void Fail(string? message)
    {
        base.WriteLine(BuildMessage("ERROR", message));
    }

    public override void Fail(string? message, string? detailMessage)
    {
        base.WriteLine(BuildMessage("ERROR", $"{message}: {detailMessage}"));
    }

    private static string BuildMessage(string category, string? message)
    {
        DateTime now = DateTime.Now;
        const string Format = "{0:yyyy-dd-M--HH-mm-ss} [{1}] {2}";
        return string.Format(Format, now, category, message);
    }
}