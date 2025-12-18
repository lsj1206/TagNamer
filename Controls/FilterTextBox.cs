using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace TagNamer.Controls;

// 파일명 / 태그 옵션 입력을 위한 FilterTextBox
// - 허용 문자(AllowedCharacters) 기반 필터링
// - 차단 문자(BlockedCharacters) 기반 제거
// - 최대 길이 제한(FilterMaxLength)
// - ViewModel과 분리

public class FilterTextBox : TextBox
{
    #region 내부 상태

    // TextChanged 재귀 호출 방지용 플래그
    // Text 변경 → TextChanged → Text 변경 무한 루프 방지
    private bool _isFiltering;

    #endregion

    #region Dependency Properties

    // 화이트리스트
    // null 또는 빈 문자열이면 사용하지 않음
    public string? AllowedCharacters
    {
        get => (string?)GetValue(AllowedCharactersProperty);
        set => SetValue(AllowedCharactersProperty, value);
    }

    public static readonly DependencyProperty AllowedCharactersProperty =
        DependencyProperty.Register(
            nameof(AllowedCharacters),
            typeof(string),
            typeof(FilterTextBox),
            new PropertyMetadata(null, OnFilterPropertyChanged));

    // 블랙리스트
    // 여기에 포함된 문자는 무조건 제거됨
    public string? BlockedCharacters
    {
        get => (string?)GetValue(BlockedCharactersProperty);
        set => SetValue(BlockedCharactersProperty, value);
    }

    public static readonly DependencyProperty BlockedCharactersProperty =
        DependencyProperty.Register(
            nameof(BlockedCharacters),
            typeof(string),
            typeof(FilterTextBox),
            new PropertyMetadata(null, OnFilterPropertyChanged));

    // 필터링 후 적용되는 최대 길이
    // 0 이하이면 제한 없음
    public int FilterMaxLength
    {
        get => (int)GetValue(FilterMaxLengthProperty);
        set => SetValue(FilterMaxLengthProperty, value);
    }

    public static readonly DependencyProperty FilterMaxLengthProperty =
        DependencyProperty.Register(
            nameof(FilterMaxLength),
            typeof(int),
            typeof(FilterTextBox),
            new PropertyMetadata(0, OnFilterPropertyChanged));

    #endregion

    #region 생성자

    static FilterTextBox()
    {
        // Custom Control 필수 코드
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(FilterTextBox),
            new FrameworkPropertyMetadata(typeof(FilterTextBox)));
    }

    public FilterTextBox()
    {
        TextChanged += OnTextChanged;
    }

    #endregion

    #region 이벤트 처리

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFiltering)
        return;

        _isFiltering = true;

        // 1프레임 딜레이를 통해서 입력이 막힌다는 느낌을 제공
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                ApplyFilter();
            }
            finally
            {
                _isFiltering = false;
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private static void OnFilterPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterTextBox tb && !string.IsNullOrEmpty(tb.Text))
        {
            tb.ApplyFilter();
        }
    }

    #endregion

    #region 필터 핵심 로직

    // 입력 텍스트에 필터 규칙을 적용한다
    private void ApplyFilter()
    {
        int caretIndex = CaretIndex;
        string original = Text ?? string.Empty;

        var builder = new StringBuilder(original.Length);

        foreach (char c in original)
        {
            // BlockedCharacters 우선 적용
            if (!string.IsNullOrEmpty(BlockedCharacters) &&
                BlockedCharacters.Contains(c))
            {
                continue;
            }

            // AllowedCharacters 적용
            if (!string.IsNullOrEmpty(AllowedCharacters) &&
                !AllowedCharacters.Contains(c))
            {
                continue;
            }

            builder.Append(c);
        }

        string filtered = builder.ToString();

        // 길이 제한
        if (FilterMaxLength > 0 && filtered.Length > FilterMaxLength)
        {
            filtered = filtered.Substring(0, FilterMaxLength);
        }

        if (filtered != original)
        {
            Text = filtered;

            // 커서 위치 보정
            CaretIndex = caretIndex > filtered.Length
                ? filtered.Length
                : caretIndex;
        }
    }

    #endregion
}
