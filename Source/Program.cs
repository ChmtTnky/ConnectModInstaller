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
                $"Please place your mods in the \"{Installer.MODS_DIR_NAME}\" folder.\n" +
                $"If you want to reset the install location, delete the \"{Installer.SAVED_CONTENT_DIR}\" folder.\n" +
                $"If you want to reset or remove your mods, reinstall the game or verify the integrity of the game files in Steam.\n");

            Installer.InstallMods();
        }

        private static void Debug(string[] args)
        {
            //Installer.ExtractFileFromCPK("bgm.awb", args[0]);
            Installer.ExtractHCAFiles(args[0]);
            //Installer.ConvertWAVFile(args[0]);
        }
    }
}