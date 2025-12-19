using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TagNamer.Models;

namespace TagNamer.ViewModels;

public partial class TagManagerViewModel : ObservableObject
{
    public ObservableCollection<TagItem> CreatedTags { get; } = new();
    public ObservableCollection<string> TagTypes { get; } = new() { "[Number]", "[AtoZ]", "[Today]", "[Time.now]" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TagTypeDescription))]
    private string selectedTagType;

    public string TagTypeDescription => SelectedTagType switch
    {
        "[Number]" => "규칙대로 순차적으로 증가하는 수를 입력하는 태그",
        "[AtoZ]" => "규칙대로 A-Z 순서로 알파벳을 입력하는 태그",
        "[Today]" => "형식에 맞춰 오늘 날짜를 입력하는 태그\n대소문자 구분없이 년:YYYY/YY 월:MM 일:DD",
        "[Time.now]" => "형식에 맞춰 현재 시간을 입력하는 태그\n대소문자 구분없이 시:HH 분:MM 초:SS",
        _ => ""
    };

    [ObservableProperty]
    private string optionDigits = "1";

    // 자리 수 옵션 예외 처리
    partial void OnOptionDigitsChanged(string value)
    {
        // 입력값이 비어있으면 1로 변경
        if (string.IsNullOrWhiteSpace(value)){
            OptionDigits = "1";
            return;
        }

        if (int.TryParse(value, out int result))
        {
            // 0이 입력되면 1로 변경
            if (result < 1) result = 1;
            // 윈도우 파일명 제한이 확장자 255자이기 때문에 250 제한
            if (result > 100) result = 100;
            // 옵션값이 변경되었을 경우에만 변경
            if (OptionDigits != result.ToString())
                OptionDigits = result.ToString();
        }
        else
        {
            // TryParse는 실패하면 false를 반환
            // overflow가 발생한 경우 1로 변경
            OptionDigits = "1";
        }
    }

    [ObservableProperty]
    private string optionStartValue = "";

    // 시작 값 옵션 예외 처리
    partial void OnOptionStartValueChanged(string value)
    {
        if (SelectedTagType == "[Number]")
        {
            // 입력값이 비어있거나 무효하면 1로 변경
            if (string.IsNullOrWhiteSpace(value))
            {
                OptionStartValue = "1";
                return;
            }

            if (long.TryParse(value, out long result))
            {
                // Number일 경우 시작값은 0 이상
                if (result < 0) result = 0;
                if (OptionStartValue != result.ToString())
                    OptionStartValue = result.ToString();
            }
            else
            {
                OptionStartValue = "1";
            }
        }
        else if (SelectedTagType == "[AtoZ]")
        {
            // AtoZ일 경우 비어있으면 A로 변경
            if (string.IsNullOrWhiteSpace(value))
            {
                OptionStartValue = "A";
            }
        }
    }

    [ObservableProperty]
    private string optionDateFormat = "";

    [ObservableProperty]
    private string optionLowerCount = "";

    partial void OnOptionLowerCountChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int result))
        {
            if (result > 255) result = 255;
            if (result < 0) result = 0;

            if (OptionLowerCount != result.ToString())
                OptionLowerCount = result.ToString();
        }
        else
        {
            OptionLowerCount = "";
        }
    }

    private int _numTagCount = 0;
    private int _atozTagCount = 0;
    private int _todayTagCount = 0;
    private int _nowTagCount = 0;

    public IRelayCommand CreateTagCommand { get; }
    public IRelayCommand<TagItem> DeleteTagCommand { get; }

    public TagManagerViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);
        DeleteTagCommand = new RelayCommand<TagItem>(DeleteTag);

        // 초기 기본 태그 추가
        AddFixedTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[Number]";
    }

    private void DeleteTag(TagItem? item)
    {
        if (item != null && CreatedTags.Contains(item))
        {
            CreatedTags.Remove(item);
        }
    }

    private void AddFixedTags()
    {
        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.origin]",
            Code = "[Name.origin]",
            ToolTip = "파일이 추가될 당시의 파일명을 입력합니다.",
            IsFixed = true
        });

        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.prev]",
            Code = "[Name.prev]",
            ToolTip = "마지막으로 변경된 파일명을 입력합니다.\n처음엔 원본 파일명이 입력됩니다.",
            IsFixed = true
        });
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SelectedTagType)) return;

        TagItem? newItem = null;

        switch (SelectedTagType)
        {
            case "[Number]":
                _numTagCount++;
                {
                    // 자리수 계산
                    string displayDigits = OptionDigits;
                    string codeDigits = OptionDigits;

                    // 시작값 계산
                    OnOptionStartValueChanged(OptionStartValue);
                    long.TryParse(OptionStartValue, out long realStart);
                    string displayStart = OptionStartValue;
                    string codeStart = OptionStartValue;

                     newItem = new TagItem
                    {
                        DisplayName = $"[Number{_numTagCount}]",
                        Code = $"[Number:{codeStart}:{codeDigits}]",
                        ToolTip = $"시작 값 : {displayStart}\n자리 수 : {displayDigits}"
                    };
                    newItem.Options.Add($"시작 값 : {displayStart}");
                    newItem.Options.Add($"자리 수 : {displayDigits}");
                }
                break;
            case "[AtoZ]":
                _atozTagCount++;
                {
                    // 자리수 계산
                    string displayDigits = OptionDigits;
                    string codeDigits = OptionDigits;

                    // 시작값 처리
                    OnOptionStartValueChanged(OptionStartValue);
                    string realStart = OptionStartValue;
                    string displayStart = OptionStartValue;

                    // LowerCount
                    int.TryParse(OptionLowerCount, out int l);
                    int realLower = l < 0 ? 0 : l;
                    string displayLower = string.IsNullOrEmpty(OptionLowerCount) ? $"{realLower} (비었음)" : $"{realLower}";

                    string codeLower = realLower.ToString();

                    newItem = new TagItem
                    {
                        DisplayName = $"[AtoZ{_atozTagCount}]",
                        Code = $"[AtoZ:{realStart}:{codeDigits}:{codeLower}]",
                        ToolTip = $"시작 값 : {displayStart}\n자리 수 : {displayDigits}\n소문자 수 : {displayLower}"
                    };
                    newItem.Options.Add($"시작 값 : {displayStart}");
                    newItem.Options.Add($"자리 수 : {displayDigits}");
                    newItem.Options.Add($"소문자 수 : {displayLower}");
                }
                break;
            case "[Today]":
                _todayTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Today{_todayTagCount}]",
                    Code = $"[Today:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat}"
                };
                newItem.Options.Add($"형식 : {OptionDateFormat}");
                break;
            case "[Time.now]":
                _nowTagCount++;
                newItem = new TagItem
                {
                    DisplayName = $"[Time.now{_nowTagCount}]",
                    Code = $"[Time.now:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat}"
                };
                newItem.Options.Add($"형식 : {OptionDateFormat}");
                break;
        }

        if (newItem != null)
        {
            CreatedTags.Add(newItem);
        }
    }






}
