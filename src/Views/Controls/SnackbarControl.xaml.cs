using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System;

namespace TagNamer.Views.Controls;

public partial class SnackbarControl : UserControl
{
    public SnackbarControl()
    {
        InitializeComponent();

        // DataContext의 IsAnimating 속성 변경 감지
        DataContextChanged += (s, e) =>
        {
            if (DataContext is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged -= OnViewModelPropertyChanged; // 중복 방지
                npc.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsAnimating")
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        // UI 스레드에서 실행 보장
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (DataContext is ViewModels.SnackbarViewModel vm)
            {
                VisualStateManager.GoToState(this, vm.IsAnimating ? "Visible" : "Hidden", true);
            }
        }));
    }
}
