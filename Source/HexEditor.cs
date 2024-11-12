﻿using System.Text.RegularExpressions;

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
			List<ModHeader> mods = [];
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

					byte[] offset_bytes = mod_reader.ReadBytes(8);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(offset_bytes);
                    Int64 hex_exe_offset = BitConverter.ToInt64(offset_bytes);

					// create a new instance
					ModHeader header = new()
					{
						exe_offset = hex_exe_offset
					};

					// check if the mod would exceed the size of the exe file
					if (header.exe_offset >= exe_writer.BaseStream.Length)
					{
						Log.WriteToLog($"Exe file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - sizeof(int)}\n");
						break;
					}

                    // read the size of the following data in the mod
                    byte[] size_bytes = mod_reader.ReadBytes(8);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(size_bytes);
                    header.data_size = BitConverter.ToInt64(size_bytes);

                    // check if the mod would exceed the size of the exe file
                    if (header.exe_offset + header.data_size >= exe_writer.BaseStream.Length)
					{
						Log.WriteToLog($"Exe file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - (sizeof(int) * 2)}\n");
						break;
					}

					// check if the size of the data would exceed the size of the mod file
					if (mod_reader.BaseStream.Position + header.data_size > mod_reader.BaseStream.Length)
					{
						Log.WriteToLog($"Mod file length would be exceeded in \"{mod_path_iter}\" at {mod_reader.BaseStream.Position - sizeof(int)}\n");
						break;
					}

					// read the byte data from the mod file
					header.data = mod_reader.ReadBytes((int)header.data_size);

					// set name
					header.file_path = mod_path_iter;

					// add the mod to the list of edits
					mods.Add(header);
				}

				// we no longer need the mod file
				// no writing was done so we don't need to flush
				mod_reader.Close();
			}

			// iterate over the sorted list of mods
			mods = mods.OrderBy(x => x.exe_offset).ToList();
			foreach (ModHeader mod in mods)
			{
				mods.AsParallel().ForAll(other_mod =>
				{
					if (mod.file_path == other_mod.file_path)
						return;
					if (mod.exe_offset < other_mod.exe_offset && mod.exe_offset + mod.data_size > other_mod.exe_offset)
					{
						Log.OutputError($"Conflict found between {mod.file_path} and {other_mod.file_path}");
					}
					else if (mod.exe_offset == other_mod.exe_offset)
					{
						Log.OutputError($"Conflict found between {mod.file_path} and {other_mod.file_path}");
					}
				});

				// jump to the position declared
				exe_writer.BaseStream.Position = mod.exe_offset;
				// overwrite the data
				exe_writer.Write(mod.data);
				Log.WriteToLog($"Writing {Path.GetFileName(mod.file_path)}:\n{mod.ToString()}\n");
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
		public string file_path;

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

		static string HexView(long value) => Regex.Replace(value.ToString("X").PadLeft(sizeof(long) * 2, '0'), ".{2}", "$0 ");

		public override readonly string ToString()
		{
			string result = $"{this.file_path}\n{HexView(this.exe_offset)}\n{HexView(this.data_size)}\n";
			result += data.Select(a => a.ToString("X").PadLeft(2, '0')).Aggregate((a, b) => $"{a} {b}");
			return result;
		}
	}
}