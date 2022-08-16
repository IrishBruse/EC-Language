namespace IBlang;

using System.Diagnostics;

public class Command
{
    public static void Run(string exe, params string[] args)
    {
        Console.WriteLine($"> {exe} {string.Join(' ', args)}");

        Process? command = Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = string.Join(' ', args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });

        if (command == null)
        {
            return;
        }

        command.WaitForExit();

        do
        {
            string output = command.StandardOutput.ReadToEnd();
            if (!string.IsNullOrEmpty(output))
            {
                Console.Write(output);
            }

            string error = command.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(error);
                Console.ResetColor();
            }
        }
        while (!command.HasExited);
    }
}
