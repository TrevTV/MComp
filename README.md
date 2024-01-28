# MComp

A simple C# console application for syncing music files between a local directory and an Android device.
FLAC files are converted to 320kbps MP3 files when transfering for the smaller size.

**Disclaimer:** Use this tool at your own risk. Ensure you have backups before making any changes to your data.

## Prerequisites
- ADB installed on your machine
- ffmpeg installed on your machine (for audio conversion)

## File Operations
- Removes files and directories on the Android device that are not present locally.
- Creates directories on the Android device that are present locally.
- Converts and pushes audio files to the Android device if needed.

## How to Run
1. Connect your Android device to your machine.
2. Ensure ADB is properly set up.
3. Set the local music directory path (`LOCAL_PATH`) and the Android music directory path (`ANDROID_PATH`) in the code.
4. Compile and run the application.

## Notes
- ADB executable (`adb.exe`) should be in the system path.
- ffmpeg executable should be in the system path for audio conversion.