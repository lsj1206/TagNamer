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
    // 태그 타입
    public ObservableCollection<string> TagTypes { get; } = new() { "[Number]", "[AtoZ]", "[Today]", "[Time.now]" };
    // 태그 생성 카운트 관리
    private readonly Dictionary<string, int> _tagCounts = new();

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
    private string optionStartValue = "";

    // 시작 값 옵션 예외 처리
    partial void OnOptionStartValueChanged(string value)
    {
        if (SelectedTagType == "[Number]")
        {
            // 입력값이 비어있거나 무효일때 0으로 변경
            if (string.IsNullOrWhiteSpace(value))
            {
                OptionStartValue = "0";
                return;
            }

            if (long.TryParse(value, out long result))
            {
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
            // AtoZ일 경우 비어있거나 알파벳이 아니면 A로 변경
            if (string.IsNullOrWhiteSpace(value) || !value.All(char.IsLetter))
            {
                OptionStartValue = "A";
                return;
            }
            if (OptionStartValue != value)
                OptionStartValue = value;
        }
    }

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
            // 윈도우 파일명 제한이 확장자 255자이기 때문에 100자 제한
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
    private string optionLowerCount = "";

    // 소문자 수 옵션 예외 처리
    partial void OnOptionLowerCountChanged(string value)
    {
        // 입력값이 비어있으면 0으로 변경
        if (string.IsNullOrWhiteSpace(value)){
            OptionLowerCount = "0";
            return;
        }

        if (int.TryParse(value, out int result))
        {
            // 자리 수가 최대 100자이기 때문에 100자 제한
            if (result > 100) result = 100;
            // 옵션값이 변경되었을 경우에만 변경
            if (OptionLowerCount != result.ToString())
                OptionLowerCount = result.ToString();
        }
        else
        {
            // TryParse는 실패하면 false를 반환
            // overflow가 발생한 경우 100으로 변경
            OptionLowerCount = "100";
        }
    }

    [ObservableProperty]
    private string optionDateFormat = "";

    public IRelayCommand CreateTagCommand { get; }
    public IRelayCommand<TagItem> DeleteTagCommand { get; }

    public TagManagerViewModel()
    {
        CreateTagCommand = new RelayCommand(CreateTag);
        DeleteTagCommand = new RelayCommand<TagItem>(DeleteTag);

        // 초기 기본 태그 추가
        AddStandardTags();
        SelectedTagType = TagTypes.FirstOrDefault() ?? "[Number]";
    }

    private void AddStandardTags()
    {
        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.origin]",
            Code = "[Name.origin]",
            ToolTip = "파일이 추가될 당시의 파일명을 입력합니다.",
            IsStandard = true
        });

        CreatedTags.Add(new TagItem
        {
            DisplayName = "[Name.prev]",
            Code = "[Name.prev]",
            ToolTip = "마지막으로 변경된 파일명을 입력합니다.\n처음엔 원본 파일명이 입력됩니다.",
            IsStandard = true
        });
    }

    private void CreateTag()
    {
        if (string.IsNullOrEmpty(SelectedTagType)) return;

        // DisplayName Counter
        if (!_tagCounts.ContainsKey(SelectedTagType))
            _tagCounts[SelectedTagType] = 0;

        _tagCounts[SelectedTagType]++;
        int currentCount = _tagCounts[SelectedTagType];

        TagItem? newItem = null;

        switch (SelectedTagType)
        {
            case "[Number]":
                {
                    // 생성 전 최종 검역
                    OnOptionStartValueChanged(OptionStartValue);
                    OnOptionDigitsChanged(OptionDigits);

                     newItem = new TagItem
                    {
                        DisplayName = $"[Number{currentCount}]",
                        Code = $"[Number:{OptionStartValue}:{OptionDigits}]",
                        ToolTip = $"시작 값 : {OptionStartValue}\n자리 수 : {OptionDigits}"
                    };
                    newItem.Options.Add($"시작 값 : {OptionStartValue}");
                    newItem.Options.Add($"자리 수 : {OptionDigits}");
                }
                break;
            case "[AtoZ]":
                {
                    // 생성 전 최종 검역
                    OnOptionStartValueChanged(OptionStartValue);
                    OnOptionDigitsChanged(OptionDigits);
                    OnOptionLowerCountChanged(OptionLowerCount);

                    newItem = new TagItem
                    {
                        DisplayName = $"[AtoZ{currentCount}]",
                        Code = $"[AtoZ:{OptionStartValue}:{OptionDigits}:{OptionLowerCount}]",
                        ToolTip = $"시작 값 : {OptionStartValue}\n자리 수 : {OptionDigits}\n소문자 수 : {OptionLowerCount}"
                    };
                    newItem.Options.Add($"시작 값 : {OptionStartValue}");
                    newItem.Options.Add($"자리 수 : {OptionDigits}");
                    newItem.Options.Add($"소문자 수 : {OptionLowerCount}");
                }
                break;
            case "[Today]":
                newItem = new TagItem
                {
                    DisplayName = $"[Today{currentCount}]",
                    Code = $"[Today:{OptionDateFormat}]",
                    ToolTip = $"{OptionDateFormat}"
                };
                newItem.Options.Add($"형식 : {OptionDateFormat}");
                break;
            case "[Time.now]":
                newItem = new TagItem
                {
                    DisplayName = $"[Time.now{currentCount}]",
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

    private void DeleteTag(TagItem? item)
    {
        if (item != null && CreatedTags.Contains(item))
        {
            CreatedTags.Remove(item);
        }
    }
}
