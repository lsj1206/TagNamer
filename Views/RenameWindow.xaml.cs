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
