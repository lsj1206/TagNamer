using System;
using System.Collections.ObjectModel;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public class FileListViewModel
{
    public ObservableCollection<FileItem> Items { get; } = new();

    public FileListViewModel()
    {
        LoadSampleData();
    }

    private void LoadSampleData()
    {
        Items.Clear();

        Items.Add(new FileItem
        {
            OriginalName = "FolderName",
            Path = @"C:\Projects\TagNamer\FolderName",
            IsFolder = true
        });

        Items.Add(new FileItem
        {
            AddIndex = 1,
            OriginalName = "FileName 001",
            NewName = "FileName 001",
            Path = @"C:\Projects\TagNamer\FolderName\FileName 001.txt",
            Size = 1 * 1024 * 1024,
            CreatedDate = DateTime.Now.AddDays(-2),
            ModifiedDate = DateTime.Now.AddDays(-1)
        });

        Items.Add(new FileItem
        {
            AddIndex = 2,
            OriginalName = "FileName 002",
            NewName = "FileName 002",
            Path = @"C:\Projects\TagNamer\FolderName\FileName 002.txt",
            Size = 12 * 1024 * 1024,
            CreatedDate = DateTime.Now.AddDays(-4),
            ModifiedDate = DateTime.Now.AddDays(-2)
        });

        Items.Add(new FileItem
        {
            AddIndex = 3,
            OriginalName = "FileName 003",
            NewName = "FileName 003",
            Path = @"C:\Projects\TagNamer\FolderName\FileName 003.txt",
            Size = 34 * 1024 * 1024,
            CreatedDate = DateTime.Now.AddDays(-5),
            ModifiedDate = DateTime.Now.AddDays(-3)
        });

        Items.Add(new FileItem
        {
            AddIndex = 4,
            OriginalName = "FileName 004",
            NewName = "FileName 004",
            Path = @"C:\Projects\TagNamer\FolderName\FileName 004.txt",
            Size = (long)(766.1 * 1024),
            CreatedDate = DateTime.Now.AddDays(-6),
            ModifiedDate = DateTime.Now.AddDays(-4)
        });

        Items.Add(new FileItem
        {
            AddIndex = 5,
            OriginalName = "FileName 005",
            NewName = "FileName 005",
            Path = @"C:\Projects\TagNamer\FolderName\FileName 005.txt",
            Size = (long)(6.1 * 1024 * 1024),
            CreatedDate = DateTime.Now.AddDays(-7),
            ModifiedDate = DateTime.Now.AddDays(-5)
        });

        Items.Add(new FileItem
        {
            OriginalName = "Another Folder",
            Path = @"C:\Projects\TagNamer\Another Folder",
            IsFolder = true
        });

        Items.Add(new FileItem
        {
            AddIndex = 6,
            OriginalName = "FileName 001",
            NewName = "FileName 001",
            Path = @"C:\Projects\TagNamer\Another Folder\FileName 001.txt",
            Size = 34 * 1024 * 1024,
            CreatedDate = DateTime.Now.AddDays(-8),
            ModifiedDate = DateTime.Now.AddDays(-6)
        });

        Items.Add(new FileItem
        {
            AddIndex = 7,
            OriginalName = "FileName 002",
            NewName = "FileName 002",
            Path = @"C:\Projects\TagNamer\Another Folder\FileName 002.txt",
            Size = (long)(766.1 * 1024),
            CreatedDate = DateTime.Now.AddDays(-9),
            ModifiedDate = DateTime.Now.AddDays(-7)
        });

        Items.Add(new FileItem
        {
            AddIndex = 8,
            OriginalName = "FileName 003",
            NewName = "FileName 003",
            Path = @"C:\Projects\TagNamer\Another Folder\FileName 003.txt",
            Size = (long)(6.1 * 1024 * 1024),
            CreatedDate = DateTime.Now.AddDays(-10),
            ModifiedDate = DateTime.Now.AddDays(-8)
        });
    }
}
