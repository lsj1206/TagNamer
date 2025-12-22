using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
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

    private Point _startPoint;
    private bool _isPotentialDrag;

    private void FileListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(null);
        _isPotentialDrag = false;

        ListViewItem? listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null && listViewItem.IsSelected)
        {
            // 이미 선택된 항목을 클릭한 경우, 드래그일 가능성이 큼.
            // WPF의 기본 동작(다른 선택 해제)을 방지하기 위해 이벤트를 처리함.
            _isPotentialDrag = true;
            e.Handled = true;
        }
    }

    private void FileListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPotentialDrag && sender is ListView listView)
        {
            ListViewItem? listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
            if (listViewItem != null)
            {
                // 드래그가 발생하지 않고 버튼을 뗐다면, 마우스 이벤트가 핸들링되어 선택이 안 바뀌었으므로
                // 여기서 수동으로 클릭한 하나만 선택되게 함 (WPF 기본 동작 재현)
                var item = listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                listView.SelectedItems.Clear();
                listView.SelectedItems.Add(item);
            }
        }
        _isPotentialDrag = false;
    }

    private void FileListView_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is ListView listView)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = _startPoint - mousePos;

            // 설정된 최소 드래그 거리 이상 움직였을 때만 드래그 시작
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                ListViewItem? listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (listViewItem != null)
                {
                    // 클릭한 아이템이 이미 선택되어 있다면 선택된 모든 항목을 그룹 이동 대상으로 함
                    var selectedItems = listView.SelectedItems.Cast<TagNamer.Models.FileItem>().ToList();
                    var clickedItem = listView.ItemContainerGenerator.ItemFromContainer(listViewItem) as TagNamer.Models.FileItem;

                    // 만약 단일 클릭 후 바로 드래그 중이라면 해당 아이템만 대상으로 함
                    if (clickedItem != null && !selectedItems.Contains(clickedItem))
                    {
                        selectedItems = new System.Collections.Generic.List<TagNamer.Models.FileItem> { clickedItem };
                    }

                    if (selectedItems.Count > 0)
                    {
                        _isPotentialDrag = false;
                        var dragData = new DataObject("InternalMove", selectedItems);
                        DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
                    }
                }
            }
        }
    }

    // 지정된 형식의 상위 요소를 찾는 헬퍼 메서드
    private static T? FindAnchestor<T>(DependencyObject? current) where T : DependencyObject
    {
        do
        {
            if (current is T t) return t;
            current = current != null ? System.Windows.Media.VisualTreeHelper.GetParent(current) : null;
        }
        while (current != null);
        return null;
    }

    private void FileListView_DragLeave(object sender, DragEventArgs e)
    {
        DropIndicator.Visibility = Visibility.Collapsed;
    }

    // 드래그 오버 이벤트 핸들러: 드래그 중인 데이터가 파일인 경우 복사 효과 표시
    private void FileListView_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropIndicator.Visibility = Visibility.Collapsed;
        }
        else if (e.Data.GetDataPresent("InternalMove"))
        {
            e.Effects = DragDropEffects.Move;
            UpdateDropIndicator(e);
        }
        else
        {
            e.Effects = DragDropEffects.None;
            DropIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateDropIndicator(DragEventArgs e)
    {
        ListViewItem? listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
        if (listViewItem != null)
        {
            Point point = e.GetPosition(listViewItem);
            double height = listViewItem.ActualHeight;
            Point screenPoint = listViewItem.TranslatePoint(new Point(0, 0), FileListView);

            // 마우스 위치가 아이템의 절반 이상이면 아래로, 아니면 위로 표시
            bool isBottom = point.Y > height / 2;
            double yPos = isBottom ? screenPoint.Y + height : screenPoint.Y;

            DropIndicator.Visibility = Visibility.Visible;
            Canvas.SetTop(DropIndicator, yPos - 1);
        }
        else
        {
            // 아이템 위가 아니면 맨 마지막에 표시
            if (FileListView != null && FileListView.Items.Count > 0)
            {
                var lastItem = FileListView.ItemContainerGenerator.ContainerFromIndex(FileListView.Items.Count - 1) as FrameworkElement;
                if (lastItem != null)
                {
                    Point screenPoint = lastItem.TranslatePoint(new Point(0, lastItem.ActualHeight), FileListView);
                    DropIndicator.Visibility = Visibility.Visible;
                    Canvas.SetTop(DropIndicator, screenPoint.Y - 1);
                }
            }
            else
            {
                DropIndicator.Visibility = Visibility.Collapsed;
            }
        }
    }

    // 드롭 이벤트 핸들러: 파일 데이터를 추출하여 ViewModel로 전달
    private void FileListView_Drop(object sender, DragEventArgs e)
    {
        DropIndicator.Visibility = Visibility.Collapsed;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                if (Window.GetWindow(this)?.DataContext is MainViewModel vm)
                {
                    vm.AddDroppedItems(files);
                }
            }
        }
        else if (e.Data.GetDataPresent("InternalMove"))
        {
            if (e.Data.GetData("InternalMove") is System.Collections.Generic.List<TagNamer.Models.FileItem> itemsToMove)
            {
                ListViewItem? listViewItem = FindAnchestor<ListViewItem>((DependencyObject)e.OriginalSource);
                int targetIndex = -1;
                bool isBottom = false;

                if (listViewItem != null)
                {
                    targetIndex = FileListView.ItemContainerGenerator.IndexFromContainer(listViewItem);
                    Point point = e.GetPosition(listViewItem);
                    isBottom = point.Y > listViewItem.ActualHeight / 2;
                }
                else
                {
                    targetIndex = FileListView.Items.Count;
                }

                if (targetIndex != -1)
                {
                    if (DataContext is FileListViewModel vm)
                    {
                        vm.MoveItems(itemsToMove, targetIndex, isBottom);
                    }
                }
            }
        }
    }
}

