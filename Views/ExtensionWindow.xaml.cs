using System.Windows;
using TagNamer.ViewModels;

namespace TagNamer.Views;

public partial class ExtensionWindow : Window
{
    public ExtensionWindow()
    {
        InitializeComponent();

        // DataContext가 변경될 때 RequestClose 액션 연결
        DataContextChanged += (s, e) =>
        {
            if (DataContext is ExtensionViewModel vm)
            {
                vm.RequestClose = (result) =>
                {
                    DialogResult = result;
                    Close();
                };
            }
        };
    }
}
