using System.Diagnostics;
using System.Drawing;
using Pastel;

const string LOCAL_PATH = @"F:\Media\Music";
const string ANDROID_PATH = "/sdcard/Music";

string treePath = await GenerateTree();

DFS.Directory local = ParseLocalDirectory();
DFS.Directory android = ParseAndroid(treePath);

DFS.ComparisonResults results = new();
CompareLocalToAndroid(local, android, results);
CompareAndroidToLocal(local, android, results);

results.Print();

WriteLineColor("Do you want to match files? (y/n)", Color.White);
if (Console.ReadLine() == "y")
{
    await MatchFiles(results);
}
else
{
    WriteLineColor("No files were copied.", Color.Red);
    WriteLineColor("Press any key to exit.", Color.White);
    Console.ReadKey();
}

async Task MatchFiles(DFS.ComparisonResults results)
{
    foreach (DFS.File file in results.RemovedFiles)
    {
        string command = $"shell \"rm {SanitizePathForAndroid(ANDROID_PATH + file.GetFullPath())}\"";
        await RunADB(command);
    }

    foreach (DFS.Directory dir in results.RemovedDirectories)
    {
        string command = $"shell \"rm -r {SanitizePathForAndroid(ANDROID_PATH + dir.GetFullPath())}\"";
        await RunADB(command);
    }

    foreach (DFS.Directory dir in results.AddedDirectories)
    {
        string command = $"shell \"mkdir {SanitizePathForAndroid(ANDROID_PATH + dir.GetFullPath())}\"";
        await RunADB(command);
    }

    /*foreach (DFS.File file in results.AddedFiles)
    {
        if (IsConvertableAudioFile(file.Name))
        {
            WriteLineColor($"Converting {file.Name} to mp3...", Color.White);
            string temp = Path.Combine(Path.GetTempPath(), file.Name.Replace(".flac", ".mp3"));
            await ConvertFlacToMp3(LOCAL_PATH + file.GetFullPath(), temp);
            string command = $"push \"{temp}\" \"{ANDROID_PATH + file.GetFullPath().Replace(".flac", ".mp3")}\"";
            await RunADB(command);
        }
        else
        {
            WriteLineColor($"Pushing {file.Name}...", Color.White);
            string command = $"push \"{LOCAL_PATH + file.GetFullPath()}\" \"{ANDROID_PATH + file.Parent.GetFullPath()}\"";
            await RunADB(command);
        }
    }*/

    var tasks = results.AddedFiles.Select(async file =>
    {
        if (IsConvertableAudioFile(file.Name))
        {
            WriteLineColor($"Converting {file.Name} to mp3...", Color.White);
            string temp = Path.Combine(Path.GetTempPath(), file.Name.Replace(".flac", ".mp3"));
            await ConvertFlacToMp3(LOCAL_PATH + file.GetFullPath(), temp);
            string command = $"push \"{temp}\" \"{ANDROID_PATH + file.GetFullPath().Replace(".flac", ".mp3")}\"";
            await RunADB(command);
        }
        else
        {
            WriteLineColor($"Pushing {file.Name}...", Color.White);
            string command = $"push \"{LOCAL_PATH + file.GetFullPath()}\" \"{ANDROID_PATH + file.Parent.GetFullPath()}\"";
            await RunADB(command);
        }
    });

    await Task.WhenAll(tasks);
}

string SanitizePathForAndroid(string path)
{
    path = path.Replace(" ", "\\ ").Replace("(", "\\(").Replace(")", "\\)");
    return path;
}

void CompareLocalToAndroid(DFS.Directory local, DFS.Directory? android, DFS.ComparisonResults results)
{
    foreach (var dir in local.Children)
    {
        if (!(android?.HasDirectory(dir.Name) ?? false))
        {
            results.AddedDirectories.Add(dir);
            CompareLocalToAndroid(dir, null, results);
        }
        else
            CompareLocalToAndroid(dir, android!.GetDirectory(dir.Name), results);
    }

    foreach (var file in local.Files)
    {
        string sanitizedName = file.Name.Replace(".flac", ".mp3");
        if (!(android?.HasFile(sanitizedName) ?? false) && !(android?.HasFile(file.Name) ?? false))
            results.AddedFiles.Add(file);
    }
}

void CompareAndroidToLocal(DFS.Directory? local, DFS.Directory android, DFS.ComparisonResults results)
{
    foreach (var dir in android.Children)
    {
        if (!(local?.HasDirectory(dir.Name) ?? false))
        {
            results.RemovedDirectories.Add(dir);
            CompareAndroidToLocal(null, dir, results);
        }
        else
            CompareAndroidToLocal(local.GetDirectory(dir.Name), dir, results);
    }

    foreach (var file in android.Files)
    {
        string sanitizedName = file.Name.Replace(".mp3", ".flac");
        if (!(local?.HasFile(sanitizedName) ?? false) && !(local?.HasFile(file.Name) ?? false))
            results.RemovedFiles.Add(file);
    }
}

DFS.Directory ParseLocalDirectory()
{
    DFS.Directory root = new("", null);

    ParseDirectory(new(LOCAL_PATH), root);

    return root;

    void ParseDirectory(DirectoryInfo dir, DFS.Directory vDir)
    {
        foreach (DirectoryInfo subDir in dir.EnumerateDirectories())
        {
            if (subDir.Attributes.HasFlag(FileAttributes.Hidden))
                continue;

            ParseDirectory(subDir, vDir.AddDirectory(subDir.Name));
        }

        foreach (FileInfo file in dir.EnumerateFiles())
        {
            // random extra files i've left in my music folders
            if (file.Extension == ".zip" || file.Extension == ".svg" || file.Extension == ".txt")
                continue;

            vDir.Files.Add(new(file.Name, vDir));
        }
    }
}

DFS.Directory ParseAndroid(string treePath)
{
    StreamReader sr = new(treePath);

    DFS.Directory root = new("", null);

    // base folder
    sr.ReadLine(); // skip first line, it's just the base folder identifier (.:)

    string? line;
    while (!string.IsNullOrWhiteSpace(line = sr.ReadLine()))
    {
        if (line == "tree.txt")
            continue;

        root.AddDirectory(line);
    }

    while ((line = sr.ReadLine()) != null)
    {
        if (!line.StartsWith("./")) // we are starting a new directory entry
            continue;

        string[] parts = line[2..^1].Split('/');

        DFS.Directory dir = root;
        foreach (string part in parts)
        {
            if (!dir.HasDirectory(part))
                dir = dir.AddDirectory(part);
            else
                dir = dir.GetDirectory(part);
        }

        while (!string.IsNullOrWhiteSpace(line = sr.ReadLine()))
        {
            // likely a folder
            string ext = Path.GetExtension(line);
            if (ext != ".flac" && ext != ".mp3" && ext != ".jpg" && ext != ".png")
                continue;

            dir.Files.Add(new DFS.File(line, dir));
        }
    }

    sr.Close();
    File.Delete(treePath);

    return root;
}

async Task<string> GenerateTree()
{
    await RunADB($"shell \"cd {ANDROID_PATH} && ls -R > tree.txt\"");

    string temp = Path.GetTempFileName();

    await RunADB($"pull {ANDROID_PATH}/tree.txt \"{temp}\"");

    await RunADB($"shell rm {ANDROID_PATH}/tree.txt");

    return temp;
}

async Task RunADB(string command)
{
    Process process = new();
    process.StartInfo.FileName = "adb.exe";
    process.StartInfo.Arguments = command;
    process.Start();
    await process.WaitForExitAsync();
}

async Task ConvertFlacToMp3(string sourceFile, string destinationFile)
{
    ProcessStartInfo psi = new()
    {
        FileName = "ffmpeg",
        Arguments = $"-y -i \"{sourceFile}\" -ab 320k \"{destinationFile}\"",
        RedirectStandardOutput = false,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using Process process = new() { StartInfo = psi };
    process.Start();
    await process.WaitForExitAsync();
}

bool IsConvertableAudioFile(string file)
{
    string extension = Path.GetExtension(file)!.ToLower();
    return extension == ".flac" || extension == ".wav";
}

void WriteLineColor(string line, Color color) => Console.WriteLine(line.Pastel(color));