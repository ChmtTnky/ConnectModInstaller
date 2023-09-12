# Dokapon Kingdom Connect PC Mod Installer
A simple program that installs mod files for Dokapon Kingdom Connect PC directly into the game's assets.

## Basic Usage
Put all of your mod files in the same folder, then drag-and-drop that folder onto the ConnectModInstaller executable.
A console will open to tell you the progress of your installation.
This program will try to look for Connect's assets automatically, though it will ask you to input the location of the assets folder if it cannot find it.
The location of the asset folder is saved for automatic future use.

## Notes
 - Using the command line, you can pass in the path to the assets folder directly.
 - This program will only patch existing files inside the CPK archives. It will not add new files or extract existing ones.
 - Duplicate input files are reported in the console. The first of each duplicate found is patched, the rest are ignored.
 - The CPKs are **NOT** encoded in the exact same way as the originals, so there is a chance that they will cause bugs even without any mods.

***If you find any bugs, please let me know as soon as possible!***

## Future Plans
 - A simple cross-platform GUI
 - Support for importing files into subarchives (texpack/pil, acb/awb, model textures, etc)

## Credits
CPK code is from https://github.com/esperknight/CriPakTools
