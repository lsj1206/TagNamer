using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using TagNamer.ViewModels;

namespace TagNamer.Views.Controls;

public partial class FileListControl : UserControl
{
    public FileListControl()
    {
        InitializeComponent();
    }

    private void ListView_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel vm)
            {
                var list = (sender as ListView)?.SelectedItems;
                if (vm.DeleteFilesCommand.CanExecute(list))
                {
                    vm.DeleteFilesCommand.Execute(list);
                }
            }
        }
    }
}

