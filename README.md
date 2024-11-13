# Dokapon Kingdom Connect PC Mod Installer
A simple program that installs mods for Dokapon Kingdom: Connect PC/Steam.

## Basic Usage
Place all of your desired mods in the Mods folder, then run the installer. The installer will ask you for your game EXE path, and provide instructions on how to find that. It will then output information about the installation's progress in the console. When it finishes, it will say "Done", and ask for user input before closing. If you have used the installer before, it will automatically run using a backup executable from your first use.

***If you find any bugs, please let me know as soon as possible!***

## General Notes
 - To uninstall your mods, you have to verify the integrity of your game files in Steam, or reinstall the game.
 - The installer will backup your EXE and the location of it to speed up future use.
 - The installer only checks the immediate directory for each mod when looking for files to install.
 - Duplicate asset files are reported in the console. The first of each duplicate found is patched, the rest are ignored.
 - Duplicate code mods are not reported, so they may cause hard-to-notice bugs.
 - Duplicate hex edits are reported in the console. The installer will apply all edits in spite of conflicts.
 - The CPKs are **NOT** encoded in the exact same way as the originals, so there is a chance that they will cause bugs even without any mods.
 - If you find a bug, please check the logs for more info as to what happened.

## Notes for Modders

### Asset Mods
 - All mod files made for use with this program need to have the same name as the game files they intend to overwrite.
 - Files are not patched with new data, they are completely overwritten.
 - Entire CPK files will not be installed with this program.
 - Mods do not need to emulate the original filesystem of the CPK files to work properly.
 - Assets can be anywhere within the mod folder, including subdirectories.

### Code mods
 - Mods of this kind must be in isolated directories within the Codes folder.
 - You will provide the user with a mod.bin, variables.txt, and functions.txt file.
 - This program currently uses DKCedit version 1.0, not 1.1. Version 1.1 will be supported eventually.
 - The installer will not check for conflicts between mods, so please ensure that your mod has a low chance of conflicts.
 - Please make sure each code mod is in a uniquely named folder, as the installer uses the folder names to differentiate mods of this type.

### Hex Edits
 - Hex edits are raw binary edits to the *unmodified* executable, applied before code mods.
 - Hex edits use a custom .hex format with the following specification (little endian):
```
 8 bytes: Starting offset
 8 bytes: Size of data
 X bytes: Data to write at the offset
 ... repeat ...
```
 - The installer will overwrite data starting at the offset with a size number of bytes specified by the data section.
 - It will then check the remaining bytes to find more locations to modify.
 - There is no limit to the number of bytes or locations you can edit, however you cannot edit data outside the size of the EXE.
 - The data section must have the exact same size as the amount specified, or else it will break.
 - The data section is written to the executable in the order specified in the .hex file.
 - Conflicting edits will be noted by the installer, but it will not prevent the edits from being applied.
 - Your mod file can be named anything as long as it has the .hex extension.
 - The installer will check for all .hex files in the Hex folder, even in subdirectories.

## Future Plans
 - A simple cross-platform GUI
 - Support for installing files into subarchives (texpack/pil, acb/awb, model textures, etc)
 - Support for installing sound files as WAV files

## Credits
 - CPK editing code is from https://github.com/esperknight/CriPakTools
 - DKCedit is from https://github.com/Purrygamer/DKCedit
