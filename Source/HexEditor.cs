using System.Reflection.Metadata.Ecma335;

namespace ConnectModInstaller
{
	public static class HexEditor
	{
		public static int ApplyMods(string mod_path, string exe_path)
		{
			// the directory probably exists by this point but we check it just in case
			if (!Directory.Exists(mod_path))
			{
				Log.OutputError($"Could not find the \"{Installer.MODS_HEX_DIR_NAME}\" folder.\nSkipping hex edits...\n");
				return 0;
			}
			// same with the exe path
			if (!File.Exists(exe_path))
			{
				Log.OutputError($"Could not find the \"{Installer.GAME_EXE_FILE_NAME}\" file.\nSkipping hex edits...\n");
				return 0;
			}

			// the entire list of files contained in the mod directory
			string[] mod_files = Directory.GetFiles(mod_path, "*.hex", SearchOption.AllDirectories);
			// quick escape if no files are found
			if (mod_files.Length == 0)
			{
				Log.WriteLine("Hex folder has no files\nSkipping hex edits...");
				return 0;
			}

			// we need this to be always sorted in order to have a smooth write later
			// writing usually takes more time than reading so this is important
			SortedSet<ModHeader> mods = [];
			// this reader is used for every mod file in the array
			BinaryReader mod_reader;
			// open the exe for later use
			BinaryWriter exe_writer = new(File.Open(exe_path, FileMode.Open));

			foreach (string mod_path_iter in mod_files)
			{
				// open the file in the current iteration
				mod_reader = new(File.Open(mod_path_iter, FileMode.Open));

				// iterate over the header data within this file
				while (mod_reader.BaseStream.Position < mod_reader.BaseStream.Length)
				{
					// ensure the file actually contains data
					if (mod_reader.BaseStream.Length - mod_reader.BaseStream.Position < (sizeof(int) * 2))
					{
						Log.WriteToLog($"Incorrect format in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position}\n");
						break;
					}

					// create a new instance
					ModHeader header = new()
					{
						// read the exe offset from the mod file
						exe_offset = mod_reader.ReadInt64()
					};

					// check if the mod would exceed the size of the exe file
					if (header.exe_offset >= exe_writer.BaseStream.Length)
					{
						Log.WriteToLog($"Exe file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - sizeof(int)}\n");
						break;
					}

					// read the size of the following data in the mod
					header.data_size = mod_reader.ReadInt64();

					// check if the mod would exceed the size of the exe file
					if (header.exe_offset + header.data_size >= exe_writer.BaseStream.Length)
					{
						Log.WriteToLog($"Exe file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - (sizeof(int) * 2)}\n");
						break;
					}

					// check if the size of the data would exceed the size of the mod file
					if (mod_reader.BaseStream.Position + header.data_size >= mod_reader.BaseStream.Length)
					{
						Log.WriteToLog($"Mod file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - sizeof(int)}\n");
						break;
					}

					// read the byte data from the mod file
					header.data = mod_reader.ReadBytes((int)header.data_size);

					// ensure the mod is added to the sorted set. the only reason this should fail is if the user is out of memory.
					if (!mods.Add(header))
					{
						Log.WriteToLog($"Failed to add \"{mod_path_iter}\" to the list\n");
						break;
					}
				}

				// we no longer need the mod file
				// no writing was done so we don't need to flush
				mod_reader.Close();
			}

			// iterate over the sorted set of mods
			foreach (ModHeader mod in mods)
			{
				// jump to the position declared
				exe_writer.BaseStream.Position = mod.exe_offset;
				// overwrite the data
				exe_writer.Write(mod.data);
				Log.WriteToLog($"Writing mod data to {mod.exe_offset}...");
			}

			exe_writer.Flush();
			exe_writer.Close();

			return mods.Count;
		}
	}
	struct ModHeader
	: IComparer<ModHeader>
	{
		public long exe_offset;
		public long data_size;
		public byte[] data;

		public readonly int Compare(ModHeader x, ModHeader y)
		{
			if (x.exe_offset < y.exe_offset)
			{
				return -1;
			}
			else if (x.exe_offset == y.exe_offset)
			{
				return 0;
			}
			return 1;
		}
	}
}