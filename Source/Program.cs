namespace ConnectModInstaller
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("\n" +
                    "Dokapon Kingdom Connect PC Mod Installer\n" +
                    "To install a mod, drag and drop the folder of mod files onto the ConnectModInstaller.\n" +
                    "Command-line Usage:\n" +
                    "ConnectModInstaller.exe <Mods Directory> <optional: Assets Directory>\n");
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
            Console.WriteLine("\n" + "Press any key to close...");
            Console.ReadKey();
        }
    }
}