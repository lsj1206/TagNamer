# TagNamer

> **수십~수천 개의 파일을 한 번에!** 직관적인 규칙 기반 Windows 일괄 파일 리네이머

![Project Version](https://img.shields.io/badge/version-0.9.0-beta)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![Framework](https://img.shields.io/badge/.NET-10.0-green)

---

## 📖 개요

**TagNamer**는 복잡한 파일 이름 변경 작업을 미리 정의된 **태그(Tag)**와 사용자 정의 규칙을 조합하여 쉽고 빠르게 처리할 수 있도록 돕는 Windows용 유틸리티입니다. 수많은 사진, 문서, 영상 파일들을 원하는 형식으로 단 몇 초 만에 정리할 수 있습니다.

무설치 포터블(Portable) 구조로 설계되어 별도의 설치 과정 없이 어디서나 즉시 사용할 수 있습니다.

## ✨ 주요 특징

- **실시간 미리보기**: 규칙을 입력하는 즉시 변경될 파일 이름을 리스트에서 바로 확인할 수 있습니다.
- **강력한 태그 시스템**: 숫자 시퀀스, 알파벳 순서, 날짜/시간, 원본 파일명 부분 추출 등 다양한 내장 태그를 지원합니다.
- **직관적인 UI/UX**: ModernWPF 기반의 세련된 디자인과 드래그 앤 드롭을 통한 파일 추가 및 순서 변경 기능을 제공합니다.
- **안정적인 대용량 처리**: 수천 개의 파일도 끊김 없이 안정적으로 이름을 변경합니다.
- **실수 방지 기능**: 변경 적용 전 최종 확인 단계와 실행 취소(Undo)를 준비 중입니다.

## 🛠 태그 시스템 (Tag System)

TagNamer의 핵심인 태그를 조합하여 나만의 규칙을 만들 수 있습니다.

| 태그 명칭 | 설명 | 비고 |
| :--- | :--- | :--- |
| `[Number]` | 규칙대로 순차적으로 증가하는 숫자를 입력 | 시작값, 자릿수 설정 가능 |
| `[AtoZ]` | A-Z 순서로 알파벳을 입력 | 시작값, 자릿수, 대소문자 설정 가능 |
| `[Origin.split]` | 원본 파일명의 특정 범위를 잘라내거나 남김 | 앞에서/뒤에서 범위 지정 가능 |
| `[Today]` | 오늘 날짜를 원하는 형식(YYYY, MM, DD 등)으로 입력 | 구분자 자유 설정 |
| `[Time.now]` | 현재 시간을 원하는 형식(HH, MM, SS 등)으로 입력 | 구분자 자유 설정 |
| `[ToUpper]` | 파일명의 모든 알파벳을 대문자로 변환 | |
| `[ToLower]` | 파일명의 모든 알파벳을 소문자로 변환 | |

## 💻 시작하기 (How to Use)

본 프로그램은 **포터블(EXE)** 형태로 배포됩니다.

1. 배포된 `TagNamer.exe` 파일을 실행합니다.
2. 사이드바의 **'파일 추가'** 버튼을 누르거나 탐색기에서 파일을 드래그하여 목록에 넣습니다.
3. **'규칙 설정'** 버튼을 클릭하여 원하는 태그와 텍스트를 조합합니다. (예: `[Today] 여행 사진_[Number]`)
4. 미리보기로 변경될 이름을 확인한 후 **'변경 적용'** 버튼을 클릭합니다.

---

## 📝 개발 정보

- **개발자**: lsj1206
- **개발 기간**: 2025.12 ~ 진행 중
- **문의**: [GitHub Issues](https://github.com/lsj1206/TagNamer/issues)

## 🚀 기술 스택

- **Language**: C#
- **Framework**: .NET 10.0 (WPF)
- **UI Library**: [ModernWPF](https://github.com/Kinnara/ModernWPF)
- **UI Architecture**: MVVM Pattern
- **IDE**: VSCode, **Antigravity**
- **AI Assistance**: Gemini 3 Pro, Gemini 3 Flash, Claude Sonnet 4.5, GPT 5

---

## ⚖️ 라이선스 (License)

이 프로젝트는 **MIT License**를 따릅니다. 사용된 외부 라이브러리의 상세 라이선스 정보는 [LICENSE](./LICENSE) 파일에서 확인하실 수 있습니다.

© 2026 TagNamer. All rights reserved.
