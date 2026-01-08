using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
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
            DragDrop.DoDragDrop(element, tagItem.TagName, DragDropEffects.Copy);
        }
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
                if (DataContext is RenameViewModel vm)
                {
                    vm.AddTagToRule(tagItem.TagName, true);
                    RuleTextBox.Focus();
                    RuleTextBox.CaretIndex = tagItem.TagName.Length;
                }
            };
            menu.Items.Add(insertFirstItem);

            // 가장 뒤에 삽입
            var insertLastItem = new MenuItem { Header = "가장 뒤에 삽입" };
            insertLastItem.Click += (s, args) =>
            {
                if (DataContext is RenameViewModel vm)
                {
                    vm.AddTagToRule(tagItem.TagName, false);
                    RuleTextBox.Focus();
                    RuleTextBox.CaretIndex = RuleTextBox.Text.Length;
                }
            };
            menu.Items.Add(insertLastItem);

            // 고정 태그는 삭제 메뉴를 노출하지 않음
            if (!tagItem.IsStandard)
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

            if (textBox != null && DataContext is RenameViewModel vm)
            {
                e.Handled = true; // 기본 Drop 동작 차단 (중복 삽입 방지)
                int caretIndex = textBox.CaretIndex;
                string currentText = textBox.Text;

                // 대소문자 변환 태그가 삽입될 경우 기존 태그 제거 및 인덱스 보정
                if (text.Equals("[ToUpper]", System.StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("[ToLower]", System.StringComparison.OrdinalIgnoreCase))
                {
                    int upperIdx = currentText.IndexOf("[ToUpper]", System.StringComparison.OrdinalIgnoreCase);
                    int lowerIdx = currentText.IndexOf("[ToLower]", System.StringComparison.OrdinalIgnoreCase);
                    int existingIdx = upperIdx != -1 ? upperIdx : lowerIdx;

                    if (existingIdx != -1)
                    {
                        if (existingIdx < caretIndex)
                        {
                            caretIndex -= "[ToUpper]".Length;
                        }
                        currentText = vm.RemoveCaseTags(currentText);
                    }
                }

                if (caretIndex < 0) caretIndex = 0;
                if (caretIndex > currentText.Length) caretIndex = currentText.Length;

                textBox.Text = currentText.Insert(caretIndex, text);
                textBox.CaretIndex = caretIndex + text.Length;
                textBox.Focus();
            }
        }
    }
}
