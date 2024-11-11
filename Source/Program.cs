namespace ConnectModInstaller
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Set up logs
            Log.InitializeLogs();

            RunInstaller(args);
            // Debug(args);

            Log.WriteLine("Press any key to close...");
            Log.Shutdown();
            Console.ReadKey();
        }

        private static void RunInstaller(string[] args)
        {
            // Default message
            Log.WriteLine(
                "Dokapon Kingdom Connect PC Mod Installer\n" +
                "----------------------------------------\n" +
                $"Please place your asset mods in the \"{Installer.MODS_ASSETS_DIR_NAME}\" folder and your code mods in the \"{Installer.MODS_CODE_DIR_NAME}\" folder.\n" +
                $"If you want to reset the install location, delete the \"{Installer.SAVED_INSTALL_TXT}\" file.\n" +
                $"If you want to reset your modifications, reinstall the game or verify the integrity of the game files in Steam.\n");

            // If there are any args, assume the first one is the game exe
            if (args.Length == 0)
                Installer.InstallMods();
            else if (args.Length != 0)
                Installer.InstallMods(args[0]);
        }

        private static void Debug(string[] args)
        {
            //Installer.ExtractFileFromCPK("bgm.awb", args[0]);
            Installer.ExtractHCAFiles(args[0]);
            //Installer.ConvertWAVFile(args[0]);
        }
    }
}