# gnosia-save-decrypt

A tool for decrypting and encrypting Gnosia save files. Drag-and-drop the save file on top of the .exe to convert to/from .json and .data format.

Please make backups of any save files you care about before modifying them! 🙏

## Features

- **Decrypt** `.data` save files to `.json`
- **Encrypt** `.json` files back to `.data`

## Usage

Gnosia save files are in the LocalLow directory. For example: `%USERPROFILE%\AppData\LocalLow\Playism\Gnosia\save\`. Note that save/slot0 is the first save slot, save/slot1 is the second, and save/slot2 is the third.


### Decrypt a Save File

Drag-and-drop an `auto.data` save file on top of `gnosia-save-decrypt.exe`, or run in the command line with the file path as the first argument. Example:

```sh
gnosia-save-decrypt.exe %USERPROFILE%\AppData\LocalLow\Playism\Gnosia\save\slot0\auto.data
```

This will create a human-readable file in the same directory called `auto.json`, which can be modified with a text editor.

### Encrypt a JSON File

Same as decryption. Drag-and-drop an `auto.json` file on top of `gnosia-save-decrypt.exe`, or run in the command line with the file path as the first argument. Example:

```sh
gnosia-save-decrypt.exe %USERPROFILE%\AppData\LocalLow\Playism\Gnosia\save\slot0\auto.json
```

This will create a file called `auto.data` in the same directory.

If this would overwrite an existing file called `auto.data` and no auto-generated backup exists, the script will ask if you want to create a backup first. To skip creating the backup, enter 'S' at the prompt.

## Building
To build and publish as a single-file executable:

```sh
dotnet publish -c Release -r win-x64 --self-contained true
```

The executable will be located in `bin\Release\net8.0\win-x64\publish\`.

Note: This requires specifically .NET 8.0. We cannot use .NET 9.0 or later as the BinaryFormatter class used by the game application for serialization is no longer usable in that version.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.