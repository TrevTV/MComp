using Pastel;
using System.Drawing;
using System.Text;

namespace DFS;

public class Directory(string name, Directory? parent, List<Directory>? children = null, List<File>? files = null)
{
    public string Name { get; set; } = name;
    public Directory? Parent { get; set; } = parent;
    public List<Directory> Children { get; set; } = children ?? [];
    public List<File> Files { get; set; } = files ?? [];

    public Directory AddDirectory(string name)
    {
        var dir = new Directory(name, this);
        Children.Add(dir);
        return dir;
    }

    public Directory GetDirectory(string name)
    {
        return Children.First(d => d.Name == name);
    }

    public bool HasDirectory(string name)
    {
        return Children.Any(d => d.Name == name);
    }

    public bool HasFile(string name)
    {
        return Files.Any(f => f.Name == name);
    }

    public string GetFullPath()
    {
        List<string> parts = [Name];

        Directory? parent = this;
        while ((parent = parent?.Parent) != null)
            parts.Add(parent.Name);

        parts.Reverse();

        return string.Join("/", parts);
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.AppendLine($"{GetFullPath()}");
        foreach (var child in Children)
            sb.AppendLine("D - " + child.Name);
        foreach (var file in Files)
            sb.AppendLine("F - " + file.Name);

        // for more layers, easier debugging
        /*foreach (var child in Children)
            sb.AppendLine(child.ToString());*/

        return sb.ToString();
    }
}

public class File(string name, Directory parent)
{
    public string Name { get; set; } = name;
    public Directory Parent { get; set; } = parent;

    public string GetFullPath()
    {
        List<string> parts = [Name];

        Directory? parent = Parent;
        parts.Add(Parent.Name);
        while ((parent = parent?.Parent) != null)
            parts.Add(parent.Name);

        parts.Reverse();

        return string.Join("/", parts);
    }
}

public class ComparisonResults
{
    public List<Directory> AddedDirectories = [];
    public List<Directory> RemovedDirectories = [];
    public List<File> AddedFiles = [];
    public List<File> RemovedFiles = [];

    public void Print()
    {
        foreach (var dir in AddedDirectories)
            WriteLineColor("+ " + dir.GetFullPath(), Color.Green);

        Console.WriteLine();

        foreach (var file in AddedFiles)
            WriteLineColor("+ " + file.GetFullPath(), Color.Green);

        Console.WriteLine();

        foreach (var dir in RemovedDirectories)
            WriteLineColor("- " + dir.GetFullPath(), Color.Red);

        Console.WriteLine();

        foreach (var file in RemovedFiles)
            WriteLineColor("- " + file.GetFullPath(), Color.Red);
    }

    private void WriteLineColor(string line, Color color) => Console.WriteLine(line.Pastel(color));
}