using System.Text;

namespace ConnectModInstaller
{
    public static class Log
    {
        private static FileStream? log;

        public static void InitializeLogs()
        {
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");
            log = File.Open(Path.Combine("Logs", $"log-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.txt"), FileMode.Create, FileAccess.Write);
        }

        public static void Shutdown()
        {
            log?.Dispose();
        }

        public static void OutputError(string? desc)
        {
            string output = $"Error: {desc}";
            Console.WriteLine(output);
            WriteToLog(output + '\n');
        }

        public static void WriteLine(string? text)
        {
            Console.WriteLine(text);
            WriteToLog(text + '\n');
        }

        public static string? ReadLine()
        {
            string? output = Console.ReadLine();
            WriteToLog(output + '\n');
            return output;
        }

        public static void WriteToLog(string? output)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(output);
            lock (log)
                log?.Write(bytes);
        }
    }
}