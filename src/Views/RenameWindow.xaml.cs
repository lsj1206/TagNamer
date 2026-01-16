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
                e.Handled = true;
                int caretIndex = textBox.CaretIndex;

                if (caretIndex < 0) caretIndex = 0;
                if (caretIndex > vm.RuleFormat.Length) caretIndex = vm.RuleFormat.Length;

                // ViewModel의 RuleFormat을 직접 수정 - OnRuleFormatChanged가 IsUnique 처리
                vm.RuleFormat = vm.RuleFormat.Insert(caretIndex, text);

                // 커서 위치 조정 (OnRuleFormatChanged에서 태그가 제거될 수 있으므로 최종 길이 기준)
                textBox.Focus();
                textBox.CaretIndex = Math.Min(caretIndex + text.Length, vm.RuleFormat.Length);
            }
        }
    }
}
