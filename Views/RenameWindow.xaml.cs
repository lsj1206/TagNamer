using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TagNamer.Models;

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
        if (sender is FrameworkElement element &&
            element.DataContext is TagItem tagItem)
        {
            // ViewModel을 통하지 않고 View에서 직접 처리하여 Undo(Ctrl+Z) 스택 유지
            RuleTextBox.Focus();
            RuleTextBox.CaretIndex = 0;
            RuleTextBox.SelectedText = tagItem.Code;

            // 포커스 유지 및 커서 위치 조정 (삽입된 텍스트 뒤로)
            RuleTextBox.CaretIndex = tagItem.Code.Length;
        }
    }

    private void TagItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element &&
            element.DataContext is TagItem tagItem)
        {
            // ViewModel을 통하지 않고 View에서 직접 처리하여 Undo(Ctrl+Z) 스택 유지
            RuleTextBox.Focus();
            RuleTextBox.CaretIndex = RuleTextBox.Text.Length;
            RuleTextBox.SelectedText = tagItem.Code;
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
