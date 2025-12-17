using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TagNamer.Models;
using TagNamer.ViewModels;

namespace TagNamer.Views;

public partial class RenameWindow : System.Windows.Window
{
    public RenameWindow()
    {
        InitializeComponent();
    }

    private void TagItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.DataContext is TagItem tagItem)
        {
            DragDrop.DoDragDrop(element, tagItem.DisplayName, DragDropEffects.Copy);
        }
    }

    private void TagItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 좌클릭 기능 제거 (요청사항)
    }

    private void TagItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is TagItem tagItem)
        {
            // 컨텍스트 메뉴 생성
            var menu = new ContextMenu();

            //  가장 앞에 삽입
            var insertFirstItem = new MenuItem { Header = "가장 앞에 삽입" };
            insertFirstItem.Click += (s, args) =>
            {
                 RuleTextBox.Focus();
                 RuleTextBox.CaretIndex = 0;
                 RuleTextBox.SelectedText = tagItem.DisplayName;
                 RuleTextBox.CaretIndex = tagItem.DisplayName.Length;
            };
            menu.Items.Add(insertFirstItem);

            // 가장 뒤에 삽입
            var insertLastItem = new MenuItem { Header = "가장 뒤에 삽입" };
            insertLastItem.Click += (s, args) =>
            {
                RuleTextBox.Focus();
                RuleTextBox.CaretIndex = RuleTextBox.Text.Length;
                RuleTextBox.SelectedText = tagItem.DisplayName;
                RuleTextBox.CaretIndex = RuleTextBox.Text.Length; // 커서 맨 뒤로
            };
            menu.Items.Add(insertLastItem);

            // 고정 태그는 삭제 메뉴를 노출하지 않음
            if (!tagItem.IsFixed)
            {
                menu.Items.Add(new Separator());
                // 태그 삭제
                var deleteItem = new MenuItem { Header = "태그 삭제" };
                deleteItem.Click += (s, args) =>
                {
                    if (DataContext is RenameViewModel vm)
                    {
                        vm.TagManager.DeleteTagCommand.Execute(tagItem);
                    }
                };
                menu.Items.Add(deleteItem);
            }

            // 메뉴 표시
            menu.IsOpen = true;
        }
    }

    private void RuleTextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = DragDropEffects.Copy;
    }

    private void RuleTextBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(string)))
        {
            var text = (string)e.Data.GetData(typeof(string));
            var textBox = sender as TextBox;

            if (textBox != null)
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, text);
                textBox.CaretIndex = caretIndex + text.Length;
                textBox.Focus();
            }
        }
    }
}
