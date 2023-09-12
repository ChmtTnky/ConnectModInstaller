using CriPakTools;

namespace ConnectModInstaller
{
    public static class Installer
    {
        private static readonly string ASSET_DIR_TXT = "asset_dir.txt";
        private static readonly string DEFAULT_ASSET_DIR = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Dokapon Kingdom Connect\\assets";

        public static bool InstallMods(string mod_dir, string asset_dir = null)
        {
            // check if mod directory has any files in it
            // generate dictionary of all mod files (file name, file_path)
            if (!Directory.Exists(mod_dir))
            {
                ProgramHelpers.OutputError("Mod directory does not exist.");
                return false;
            }
            string[] mod_files = Directory.GetFiles(mod_dir, "*.*", SearchOption.AllDirectories);
            if (mod_files.Length == 0)
            {
                ProgramHelpers.OutputError("Mod directory is empty.");
                return false;
            }
            Dictionary<string, string> mods = new Dictionary<string, string>();
            foreach (string mod_file in mod_files)
            {
                // check for duplicate mod files
                if (!mods.ContainsKey(Path.GetFileName(mod_file)))
                    mods.Add(Path.GetFileName(mod_file), mod_file);
                else
                    Console.WriteLine($"Duplicate file found at {mod_file}");
            }
            Console.WriteLine($"\nInstalling {mods.Count} {(mods.Count == 1 ? "file" : "files")}...");

            // verify the asset dir
            // if the arg has been set, check if it exists
            if (asset_dir != null)
            {
                // if it doesnt exist, output an error
                if (!Directory.Exists(asset_dir))
                {
                    ProgramHelpers.OutputError("Asset directory does not exist.");
                    return false;
                }
            }
            else // if no arg has been passed
            {
                // if dir has been saved from a prior run
                if (File.Exists(ASSET_DIR_TXT))
                {
                    // read from saved file
                    asset_dir = File.ReadAllText(ASSET_DIR_TXT).Trim().Replace("\"", "");
                    // if saved dir is invalid, request a path from the user
                    if (!Directory.Exists(asset_dir))
                        asset_dir = RequestAssetDir();
                    // if requested path is still invalid, output an error and exit
                    if (!Directory.Exists(asset_dir))
                    {
                        ProgramHelpers.OutputError("Asset directory does not exist.");
                        // delete saved dir
                        File.Delete(ASSET_DIR_TXT);
                        return false;
                    }
                }
                // if dir hasnt been saved
                else
                {
                    // check if the default path is valid
                    if (!Directory.Exists(DEFAULT_ASSET_DIR))
                    {
                        // if default is invalid, request path from the user
                        asset_dir = RequestAssetDir();
                        // if requested path is still invalid, output an error and exit
                        if (!Directory.Exists(asset_dir))
                        {
                            ProgramHelpers.OutputError("Asset directory does not exist.");
                            return false;
                        }
                    }
                    else // if it is valid
                        asset_dir = DEFAULT_ASSET_DIR;
                }
            }
            // if the dir is valid, save it for future runs
            Console.WriteLine("Using " + asset_dir + " for the installation...\n");
            File.WriteAllText(ASSET_DIR_TXT, asset_dir);

            // Check if the asset directory has any cpk files in it
            string[] cpk_paths = Directory.GetFiles(asset_dir, "*.cpk");
            if (cpk_paths.Length == 0)
            {
                ProgramHelpers.OutputError("Asset directory is empty.");
                return false;
            }

            // patch all cpks in the asset directory
            foreach (string cpk_path in cpk_paths)
            {
                int patched = PatchCPK(cpk_path, mods);
                Console.WriteLine($"Patched {patched} {(patched == 1 ? "file" : "files")} in {Path.GetFileName(cpk_path)}");
            }

            // verify each cpk creation
            // then move cpks into the assets directory
            foreach (string cpk_path in cpk_paths)
            {
                if (File.Exists(Path.GetFileName(cpk_path)))
                {
                    File.Move(Path.GetFileName(cpk_path), cpk_path, true);
                }
            }

            Console.WriteLine("\nSuccess!");
            return true;
        }

        // ask the user for their asset directory
        // ask to save the path to speed up future installs
        private static string? RequestAssetDir()
        {
            Console.WriteLine(
                "Please input the path of your game installation's \"assets\" directory.\n" +
                "To find the path, in Steam, right click on the game in your library, then click Manage, then Browse Local Files.\n" +
                "Then, to obtain the path of the \"assets\" folder, click on it, then shift + right click it and select Copy as Path.\n" +
                "Then paste and enter it into this console.\n" +
                "Assets Path: ");
            string asset_dir = Console.ReadLine().Trim().Replace("\"", "");
            if (Directory.Exists(asset_dir))
                return asset_dir;
            return null;
        }

        // iterate over every entry in the given cpk
        // if an entry matches the name of one of the mod files, replace that entry with the mod file
        // if any game files share names, this will break (have yet to verify that none are the same, but it is likely)
        // after checki every entry, output the cpk to the active directory
        public static int PatchCPK(string cpk_file, Dictionary<string, string> mods)
        {
            int patch_count = 0;

            //load cpk
            string cpk_name = cpk_file;
            CPK cpk = new CPK(new Tools());
            cpk.ReadCPK(cpk_name);
            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));

            // setup i/o strings
            string outputName = Path.GetFileName(cpk_file);

            // prepare new cpk and get all file entries
            BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));
            List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

            // replace a single entry with another file
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].FileType != "CONTENT")
                {

                    if (entries[i].FileType == "FILE")
                    {
                        // I'm too lazy to figure out how to update the ContextOffset position so this works :) {original comment}
                        if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                        {
                            ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                            for (ulong z = 0; z < padLength; z++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }

                    // if the current entry has a corresponding mod file, replace it
                    // if not, write it into the cpk with no changes
                    if (!mods.ContainsKey(Path.GetFileName(entries[i].FileName.ToString())))
                    {
                        oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                        cpk.UpdateFileEntry(entries[i]);

                        byte[] chunk = oldFile.ReadBytes(int.Parse(entries[i].FileSize.ToString()));
                        newCPK.Write(chunk);
                    }
                    else
                    {
                        string new_file_path;
                        mods.TryGetValue(Path.GetFileName(entries[i].FileName.ToString()), out new_file_path);
                        byte[] newbie = File.ReadAllBytes(new_file_path);
                        entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                        entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                        entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                        cpk.UpdateFileEntry(entries[i]);
                        newCPK.Write(newbie);
                        patch_count++;
                    }

                    if ((newCPK.BaseStream.Position % 0x800) > 0)
                    {
                        long cur_pos = newCPK.BaseStream.Position;
                        for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                        {
                            newCPK.Write((byte)0);
                        }
                    }
                }
                else
                {
                    // Content is special.... just update the position {original comment}
                    cpk.UpdateFileEntry(entries[i]);
                }
            }

            // write out other cpk data
            cpk.WriteCPK(newCPK);
            cpk.WriteITOC(newCPK);
            cpk.WriteTOC(newCPK);
            cpk.WriteETOC(newCPK);
            cpk.WriteGTOC(newCPK);

            // cleanup
            newCPK.Close();
            oldFile.Close();

            return patch_count;
        }
    }
}
