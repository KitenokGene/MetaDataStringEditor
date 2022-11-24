# global-metadata.dat editor for Unity games (translated to English by kitenokgene)
&emsp;&emsp;For Android games exported by the Unity-il2cpp script backend, the strings appearing in the code will be compiled into the assets\bin\Data\Managed\Metadata\global-metadata.dat file, as part of the localization work. This is a simple tool for modifying the strings in it.
## References
- [il2cppdumper](https://github.com/Perfare/Il2CppDumper)<br>
The understanding of the content of this file is learned from the source code of this tool. This tool itself is used to export class definitions from the compiled libil2cpp.so file and global-metadata.dat file. The export form includes IDA available  It's a great tool to rename scripts, UABE and AssetStudio-available DLLs, etc.
## Modifying content
&emsp;&emsp;In global-metadata.dat, the way to save the strings in the code is that there is a list in the header to store information such as the offset and length of each string, and then there is an area in the data area to directly compact all the strings.  A string, a list with a head, so no \0 at the end.<br>
&emsp;&emsp;Because the number of character strings remains unchanged before and after modification, the modification to the list is directly overwritten on the original area.  The length of the data area may change. If the length of the data area is less than or equal to the original length after modification, it will be overwritten and written directly. If it is too long, it will be written to the end of the file.
