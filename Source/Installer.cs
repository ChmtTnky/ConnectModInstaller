using CriPakTools;
using System.Diagnostics;
using System.Text;

namespace ConnectModInstaller
{
    public static class Installer
	{
		public static readonly string SAVED_INSTALL_TXT = "game_exe.txt";
		public static readonly string MODS_ASSETS_DIR_NAME = "Assets";
		public static readonly string MODS_CODE_DIR_NAME = "Codes";
		public static readonly string MODS_SOUND_DIR_NAME = "Sounds";
		public static readonly string MODS_HEX_DIR_NAME = "Hex";
        public static readonly string GAME_ASSETS_DIR_NAME = "assets";
        public static readonly string GAME_EXE_FILE_NAME = "DkkStm.exe";

		public static bool[]? InstallMods(string? exe_path = null)
		{
			// get and verify the path of the game exe
			var get_exe = GetEXEPath(exe_path);
			if (get_exe.has_error)
				return null;
			exe_path = get_exe.exe_path;
			string? game_dir = Path.GetDirectoryName(exe_path);
			Log.WriteLine($"Using \"{game_dir}\" for the installation...\n");

			// run the individual install functions
			bool assets_success = InstallAssetMods(game_dir);
			bool codes_success = InstallCodeMods(game_dir);
            bool hex_success = InstallHexEdits(exe_path);
            bool sounds_success = InstallSoundMods();

            // save the game path for future use
            File.WriteAllText(SAVED_INSTALL_TXT, exe_path);

			Log.WriteLine("Done.\n");
			bool[] output = { assets_success, codes_success, hex_success, sounds_success };
			return output;
		}

		private static (string? exe_path, bool has_error) GetEXEPath(string? cli_arg)
		{
			// Verify the input path
			// First, check the arg
			string? output_path = cli_arg?.Replace("\"", "").Trim();
			if (output_path == null || !File.Exists(output_path))
			{
				// If the arg is invalid, check the text file
				if (File.Exists(SAVED_INSTALL_TXT))
					output_path = File.ReadAllText(SAVED_INSTALL_TXT).Replace("\"", "").Trim();

				// If the saved path isn't valid, ask for a path
				if (!File.Exists(output_path))
				{
					output_path = RequestEXEPathFromUser();
					Log.WriteLine("");
					// If the manual path is not valid, output an error and exit
					if (!File.Exists(output_path))
					{
						Log.OutputError($"Invalid executable path:\n{output_path}\nExiting program...\n");
						return (null, true);
					}
				}
			}
			return (output_path, false);
		}

		// output instructions for the user on how to find the exe
		// the exe is gotten over the game directory because it is easier for end users to find and get the path
		private static string? RequestEXEPathFromUser()
		{
			Log.WriteLine(
				$"Please input the path of your installation's \"{GAME_EXE_FILE_NAME}\" file.\n" +
				"To get the file path, in Steam, Right Click on the game in your Library, then click Manage, then Browse Local Files.\n" +
				$"Then, to obtain the path of the \"{GAME_EXE_FILE_NAME}\" file, click on it, then Shift + Right Click it and select Copy as Path.\n" +
				"Then, paste it into this console and press Enter.\n" +
				"Executable Path: ");
			return Log.ReadLine()?.Replace("\"", "").Trim();
		}

		private static bool InstallAssetMods(string? game_dir)
		{
			// Verify all prereqs
			// Check if mods folder exists
			if (!Directory.Exists(MODS_ASSETS_DIR_NAME))
			{
				Log.OutputError($"Could not find the \"{MODS_ASSETS_DIR_NAME}\" folder.\nSkipping asset mods...\n");
				return false;
			}
			// Check if game assets folder exists
			string? game_asset_dir = Path.Combine(game_dir, GAME_ASSETS_DIR_NAME);
			if (!Directory.Exists(game_asset_dir))
			{
				Log.OutputError($"Game asset folder could not be found at:\n\"{game_asset_dir}\"\nSkipping asset mods...\n");
				return false;
			}
			// Check if the game assets folder has any cpk files in it
			string[] cpk_paths = Directory.GetFiles(game_asset_dir, "*.cpk");
			if (cpk_paths.Length == 0)
			{
				Log.OutputError($"Game asset folder has no CPK files:\n\"{game_asset_dir}\"\nSkipping asset mods...\n");
				return false;
			}
			Log.WriteLine($"Installing asset mods at \"{game_asset_dir}\"...");

			// Generate dictionary of all mod files (file name, file_path)
			Dictionary<string, string> mods = new();
			IEnumerable<string> mod_files = Directory.EnumerateFiles(MODS_ASSETS_DIR_NAME, "*", SearchOption.AllDirectories);
			foreach (string mod_file in mod_files)
			{
				// check for duplicate mod files
				if (!mods.ContainsKey(Path.GetFileName(mod_file)))
					mods.Add(Path.GetFileName(mod_file), mod_file);
				else
					Log.WriteLine($"Skipping duplicate asset file found at:\n\"{mod_file}\"");
			}
			if (mods.Count == 0)
			{
				Log.OutputError($"\"{MODS_ASSETS_DIR_NAME}\" is empty.\nSkipping asset mods...\n");
				return false;
			}
			Log.WriteLine($"\nInstalling {mods.Count} asset {(mods.Count == 1 ? "file" : "files")}...\n");

			// patch all cpks in the asset directory
			Parallel.For(0, cpk_paths.Length, i =>
			{
				int patched = PatchCPK(cpk_paths[i], mods);
				// assume a patch count of 0 means the CPK got skipped
				if (patched != 0)
				{
					Log.WriteLine($"Patched {patched} {(patched == 1 ? "file" : "files")} in {Path.GetFileName(cpk_paths[i])}...");
					// move from local dir to output dir
					if (File.Exists(Path.GetFileName(cpk_paths[i])))
						File.Move(Path.GetFileName(cpk_paths[i]), cpk_paths[i], true);
					else
						Log.OutputError($"Missing patched CPK:\n{Path.GetFileName(cpk_paths[i])}\n");
				}
				else
					Log.WriteLine($"Skipping {Path.GetFileName(cpk_paths[i])}...");
			});

			Log.WriteLine("\nFinished installing asset mods...\n");
			return true;
		}

		private static bool InstallCodeMods(string game_dir)
		{
			// get mod folders
            string[] edit_folder_paths = Directory.GetDirectories(MODS_CODE_DIR_NAME);

			// check for mods
			if (edit_folder_paths.Length == 0)
			{
                Log.WriteLine($"Codes folder has no subdirectories\nSkipping code mods...\n");
                return false;
            }
			Log.WriteLine($"Installing code mods...");

            // set up args
            int total_mods = 0;
            string args = $"\"{game_dir}\"";
			foreach (string folder in edit_folder_paths)
			{
                // check for mod files
                if (!File.Exists(Path.Combine(folder, "mod.bin")))
                {
                    Log.WriteLine($"{folder} does not have a mod.bin file\nSkipping mod...");
                    continue;
                }
                if (!File.Exists(Path.Combine(folder, "functions.txt")))
				{
					Log.WriteLine($"{folder} does not have a functions.txt file\nSkipping mod...");
					continue;
				}
                if (!File.Exists(Path.Combine(folder, "variables.txt")))
                {
                    Log.WriteLine($"{folder} does not have a variables.txt file\nSkipping mod...");
                    continue;
                }
				Log.WriteToLog($"Applying the {Path.GetFileName(folder)} mod...\n");
                args += $" \"{Path.GetFullPath(folder)}\"";
				total_mods++;
			}
			// run command
			Log.WriteLine($"Applying {total_mods} code {(total_mods == 1 ? "mod" : "mods")}...");
			Log.WriteToLog($"Running DKCedit with: {args}\n");
			Process? apply_codes = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine("dkcedit", "DKCedit.exe"),
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            string log_output = apply_codes.StandardOutput.ReadToEnd() + "\n" + apply_codes.StandardError.ReadToEnd();
            Log.WriteToLog(log_output);
            apply_codes?.WaitForExit();
            apply_codes?.Close();
            return true;
		}

		private static bool InstallHexEdits(string exe_path)
		{
			int total_mods = HexEditor.ApplyMods(MODS_HEX_DIR_NAME, exe_path);
			return (total_mods > 0);
		}

        // Modified version of patching function. Now does batch patching rather than single files.
        public static int PatchCPK(string cpk_file, in Dictionary<string, string> mods)
		{
			int patch_count = 0;

			//load cpk
			string cpk_name = cpk_file;
			CPK cpk = new CPK(new Tools());
			cpk.ReadCPK(cpk_name);
			List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

			// check if any mod files are in the CPK
			// if no mod file matches an entry in the CPK, skip the CPK
			bool has_mods = false;
			HashSet<string?> hash_entries = entries.Select(x => x.FileName.ToString()).ToHashSet();
			for (int i = 0; i < mods.Count; i++)
			{
				if (hash_entries.TryGetValue(mods.Keys.ElementAt(i), out var temp))
				{
					has_mods = true;
					break;
				}
			}
			if (!has_mods)
				return 0;

			// setup i/o
			string outputName = Path.GetFileName(cpk_file);
			BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));
			BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

			// iterate over every entry in the given cpk
			// if an entry matches the name of one of the mod files, replace that entry with the mod file
			// after checking every entry, output the cpk to the active directory
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
					if (!mods.TryGetValue(Path.GetFileName(entries[i].FileName.ToString()), out string new_file_path))
					{
						oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

						entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
						cpk.UpdateFileEntry(entries[i]);

						byte[] chunk = oldFile.ReadBytes(int.Parse(entries[i].FileSize.ToString()));
						newCPK.Write(chunk);
					}
					else
					{
						Log.WriteToLog($"Writing \"{new_file_path}\" to {Path.GetFileName(cpk_file)}...\n");
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

        // IN PROGRESS
        private static bool InstallSoundMods()
        {
            return false;
        }

        // corresponding acb file has to be next to the awb
        public static string? ExtractHCAFiles(string file_path)
        {
            // check if input exists
            if (!File.Exists(file_path))
            {
                Log.OutputError($"Could not find {file_path}");
                return null;
            }

            // create output directory for the extracted files
            string output_folder = Path.GetFileNameWithoutExtension(file_path);
            if (Directory.Exists(output_folder))
                Directory.Delete(output_folder, true);
            Directory.CreateDirectory(output_folder);

            // run command
            Process? extract = Process.Start(new ProcessStartInfo()
            {
                FileName = Path.Combine("vgmstream", "vgmstream-cli.exe"),
                Arguments = $"-o \"{output_folder}\\?s-?n.hca\" -L -S 0 {file_path}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            string log_output = extract.StandardOutput.ReadToEnd() + "\n" + extract.StandardError.ReadToEnd();
            Log.WriteToLog(log_output);
            extract?.WaitForExit();
            extract?.Close();
            return output_folder;
        }

        public static string? ConvertWAVFile(string file_path)
        {
            if (!File.Exists(file_path))
            {
                Log.OutputError($"Could not find WAV file to convert at: {file_path}");
                return null;
            }
            string input_name = Path.GetFileNameWithoutExtension(file_path);
            string output_file = input_name + ".hca";

            // get loop data if it exists
            bool loop = false;
            int loop_start = 0;
            int loop_end = 0;
            string loop_data_file = Path.Combine(Path.GetDirectoryName(file_path), input_name + ".loop");
            if (File.Exists(loop_data_file))
            {
                string[] loop_data_string = File.ReadAllText(loop_data_file).Trim().Split(',');
                try
                {
                    loop_start = int.Parse(loop_data_string[0]);
                    loop_end = int.Parse(loop_data_string[1]);
                    loop = true;
                }
                catch
                {
                    // inform user there was an error with a loop file
                    Log.OutputError($"Invalid loop data found for: {file_path}\nContinuing conversion with no loop...");
                }
            }

            // run command
            if (loop)
            {
                Process? convert = Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine("criatomencd", "criatomencd.exe"),
                    Arguments = $"\"{file_path.Trim('\"').Trim()}\" -lps={loop_start} -lpe={loop_end} -id=1 -keycode=104863924750642073 {output_file}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                string log_output = convert.StandardOutput.ReadToEnd() + "\n" + convert.StandardError.ReadToEnd();
                Log.WriteToLog(log_output);
                convert?.WaitForExit();
                convert?.Close();
            }
            else
            {
                Process? convert = Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine("criatomencd", "criatomencd.exe"),
                    Arguments = $"\"{file_path.Trim('\"').Trim()}\" -id=1 -keycode=104863924750642073 {output_file}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                string log_output = convert.StandardOutput.ReadToEnd() + "\n" + convert.StandardError.ReadToEnd();
                Log.WriteToLog(log_output);
                convert?.WaitForExit();
                convert?.Close();
            }
            return output_file;
        }

        public static (string? awb, string? acb) EncodeSoundArchive(string sound_dir_path, bool acb = false)
        {
            string? output_file_prefix = Path.GetDirectoryName(sound_dir_path);
            (string? awb, string? acb) output_files = (null, null);

            if (acb)
            {
                Process? encode = Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine("criatomencd", "criatomencd.exe"),
                    Arguments = $"\"{sound_dir_path.Replace('\"', ' ').Trim()}\" -acb -keycode=104863924750642073 -id=1 {output_file_prefix}.awb",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                string log_output = encode.StandardOutput.ReadToEnd() + "\n" + encode.StandardError.ReadToEnd();
                Log.WriteToLog(log_output);
                encode?.WaitForExit();
                encode?.Close();
                output_files = (output_file_prefix + ".awb", output_file_prefix + ".acb");
            }
            else
            {
                Process? encode = Process.Start(new ProcessStartInfo()
                {
                    FileName = Path.Combine("criatomencd", "criatomencd.exe"),
                    Arguments = $"\"{sound_dir_path.Replace('\"', ' ').Trim()}\" -keycode=104863924750642073 -id=1 {output_file_prefix}.awb",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                string log_output = encode.StandardOutput.ReadToEnd() + "\n" + encode.StandardError.ReadToEnd();
                Log.WriteToLog(log_output);
                encode?.WaitForExit();
                encode?.Close();
                output_files = (output_file_prefix + ".awb", null);
            }
            return output_files;
        }

        // Modifed version of the extraction function. Only does single files.
        public static string? ExtractFileFromCPK(string file_name, string cpk_path)
        {
            if (!File.Exists(cpk_path))
            {
                Log.OutputError($"Cannot find {cpk_path}\nCanceling extraction of {file_name} from {cpk_path}");
                return null;
            }

            string cpk_name = cpk_path;

            CPK cpk = new CPK(new Tools());
            cpk.ReadCPK(cpk_name);

            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));
            string extractMe = Path.GetFileName(file_name);

            FileEntry? entry = cpk.FileTable.Find(x => Path.GetFileName(x.FileName.ToString()) == extractMe);

            if (entry == null)
            {
                Log.OutputError($"Cannot find {entry.FileName}\nCanceling extraction of {entry.FileName} from {cpk_name}");
                return null;
            }

            string output_file_name = null;
            oldFile.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);
            string isComp = Encoding.ASCII.GetString(oldFile.ReadBytes(8));
            oldFile.BaseStream.Seek((long)entry.FileOffset, SeekOrigin.Begin);

            byte[] chunk = oldFile.ReadBytes(Int32.Parse(entry.FileSize.ToString()));
            if (isComp == "CRILAYLA")
            {
                int size = Int32.Parse((entry.ExtractSize ?? entry.FileSize).ToString());
                chunk = cpk.DecompressCRILAYLA(chunk, size);
            }

            Log.WriteToLog("Extracting: " + ((entry.DirName != null) ? entry.DirName + "/" : "") + entry.FileName.ToString() + "\n");
            File.WriteAllBytes(entry.FileName.ToString(), chunk);
            output_file_name = entry.FileName.ToString();
            return output_file_name;
        }
    }
}
