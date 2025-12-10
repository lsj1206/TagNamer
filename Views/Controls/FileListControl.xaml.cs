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

    // 드래그 오버 이벤트 핸들러: 드래그 중인 데이터가 파일인 경우 복사 효과 표시
    private void FileListView_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    // 드롭 이벤트 핸들러: 파일 데이터를 추출하여 ViewModel로 전달
    private void FileListView_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (Window.GetWindow(this)?.DataContext is MainViewModel vm)
            {
                vm.AddDroppedItems(files);
            }
        }
    }

}

