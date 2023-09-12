namespace ConnectModInstaller
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Dokapon Kingdom Connect PC Mod Installer");
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "To install a mod, drag and drop the folder of mod files onto the ConnectModInstaller.\n" +
                    "Command-line Usage:\n" +
                    "ConnectModInstaller.exe <Mods Directory> <optional: Assets Directory>");
            }
            else if (args.Length == 1)
            {
                if (Directory.Exists(args[0]))
                    Installer.InstallMods(args[0]);
            }
            else
            {
                if (Directory.Exists(args[0]))
                    Installer.InstallMods(args[0], args[1]);
            }
            Console.WriteLine("\nPress any key to close...");
            Console.ReadKey();
        }
    }
}