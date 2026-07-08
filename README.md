# <img src="./src/Assets/App.ico" width="32" height="32" /> TagNamer

> **태그로 간편하게 사용하는** 윈도우 파일명 일괄 변경 프로그램

> A simple Windows bulk file renaming tool **using tags**

![Platform](https://img.shields.io/badge/platform-Windows-lightgrey?style=for-the-badge)
![Framework](https://img.shields.io/badge/.NET-10.0-green?style=for-the-badge)
![Language](https://img.shields.io/badge/language-Korean-blue?style=for-the-badge)
![Language](https://img.shields.io/badge/language-English-blue?style=for-the-badge)
![Project Version](https://img.shields.io/badge/version-v1.2.0-blue?style=for-the-badge)

---

## 💻 시작하기 (How to Use)

해당 프로그램은 **EXE** 파일로 배포됩니다.

1. 다운로드 : [GitHub Releases](https://github.com/lsj1206/TagNamer/releases)
2. 파일을 실행하여 프로그램을 시작합니다.
3. 사이드바의 **'파일/폴더 추가'** 버튼을 사용하거나 파일 및 폴더를 드래그하여 목록에 추가합니다.
4. **'규칙 설정'** 버튼을 클릭하여 원하는 태그와 텍스트를 조합합니다. (예: `[Today] 여행 사진_[Number]`)
5. 목록에서 미리보기를 확인한 후 **'변경 적용'** 버튼을 클릭합니다.

This application is portable **EXE** file.

1. Download: [GitHub Releases](https://github.com/lsj1206/TagNamer/releases)
2. Run EXE file.
3. Add files or folders using the **"Add File / Add Folder"** or **Drag & Drop**
4. Click **"Rename Rule"** and combine text and tags. (e.g. `[Today] Travel_Photo_[Number]`)
5. Click **"Apply Changes"**

## 📖 개요 (Overview)

**TagNamer**는 **태그**와 텍스트를 조합하여 쉽고 빠르게 이름을 변경할 수 있는 Windows용 유틸리티입니다.

별도의 설치 과정 없이 사용할 수 있습니다.

**TagNamer** is a Windows utility that allows you to quickly rename files and folders
by combining **tags** and **custom text**.

No installation is required — just download and run.

## 🛠️ 기능 설명 (Features)

- **파일/폴더 추가** : 여러 파일/폴더를 _파일 추가_, _폴더 추가_ 버튼과 드래그&드롭으로 한번에 추가할 수 있습니다.
- **규칙 설정** : 규칙 텍스트 박스에 텍스트와 태그를 조합하여 규칙을 설정 가능하고 실시간으로 적용됩니다.
  _변경 적용_, _변경 취소_ 버튼으로 규칙을 적용과 되돌리기가 가능합니다.
- **정렬** : 파일 목록을 다양한 기준으로 정렬하고, _번호 재정렬_ 버튼과 파일 위치를 직접 옮겨서 순서를 변경할 수 있습니다.
- **개별 변경 모드** : 해당 옵션을 활성화하면 파일 이름을 개별적으로 수정할 수 있습니다.
- **확장자 표시** : 해당 옵션으로 확장자를 표시를 ON/OFF 할 수 있습니다.
- **삭제 확인 표시** : 해당 옵션으로 파일 삭제 확인창을 ON/OFF 할 수 있습니다.
- **한/영 언어 전환** : 버튼 클릭으로 한/영 언어를 전환할 수 있습니다.

### 🏷️ 태그 시스템 (Tag System)

| 태그 명칭      | 분류 | 설명                                              | 옵션                           |
| :------------- | :--- | :------------------------------------------------ | :----------------------------- |
| `[Origin]`     | 기본 | 원본 파일명을 입력합니다.                         |                                |
| `[Name]`       | 기본 | 현재 파일명을 입력합니다.                         |                                |
| `[OnlyNumber]` | 기본 | 파일명에서 숫자만 남김                            |                                |
| `[OnlyLetter]` | 기본 | 파일명에서 숫자, 특수문제를 제거                  |                                |
| `[ToUpper]`    | 기본 | 파일명의 모든 알파벳을 대문자로 변환              |                                |
| `[ToLower]`    | 기본 | 파일명의 모든 알파벳을 소문자로 변환              |                                |
| `[Number]`     | 옵션 | 시작 값에서부터 순차적으로 증가하는 수를 입력     | 시작값, 자릿수                 |
| `[AtoZ]`       | 옵션 | 시작 알파벳에서 A-Z 순서로 증가하는 알파벳을 입력 | 시작값, 자릿수, 대소문자       |
| `[Name.trim]`  | 옵션 | 파일명의 특정 범위를 잘라내거나 남김              | 시작 위치, 범위, 자르기/남기기 |
| `[Today]`      | 옵션 | 오늘 날짜를 형식(YY, MM, DD 등)에 맞춰 입력       | 형식, 구분자                   |
| `[Time.now]`   | 옵션 | 현재 시간을 형식(HH, MM, SS 등)에 맞춰 입력       | 형식, 구분자                   |

---

## 📝 패치 노트 (Patch Notes)

### v1.0.0 (2026.01.13)

- **초기 버전**

#### v.1.1.0 (2026.01.16)

- [Name] 태그 추가
- [Origin.split] -> [Name.trim] 이름 및 기능 변경
- [Origin] 태그 버그 수정

#### v.1.2.0 (2026.01.23)

- 영어 지원 추가 (English support)
- 한/영 언어 전환 기능 추가 (Added Korean/English language switching)
- 태그 치환 일관성 개선 (Improved tag consistency)
  - 태그 값이 비어 있어도 치환되도록 수정 (Replace even if tag value is empty)

#### v.1.3.0 (2026.02.09)

- 스낵바 알림 안정성 강화 (Improved snackbar notification stability)
  - CancellationTokenSource와 세션 ID(Interlocked Counter) 도입하여 알림이 겹쳤을때 표현 개선
- 파일의 실제 아이콘 반영 (Reflected actual file icons)
  - 기존의 파일/폴더 구분 아이콘에서 `Windows Shell API(SHGetFileInfo)`으로 파일의 실제 아이콘을 표시
- 언어 선택 버튼 변경 (Changed language selection button)
- 내부 구조 개선 (Improved internal structure)

---

## 🔎 개발 정보

- **개발자**: lsj1206
- **기간**: 2025.12 ~ 2026.01
- **[개발 일지](https://lsj1206.github.io/blog/search/?tag=Rename)**

## 🚀 기술 스택

- **Language**: C#
- **Framework**: .NET 10.0 (WPF)
- **UI Library**: [ModernWPF](https://github.com/Kinnara/ModernWPF)
- **IDE**: **Antigravity**, VSCode, Visual Studio
- **AI**: Gemini 3 Pro, Gemini 3 Flash, Claude Sonnet 4.5, GPT 5

## ⚖️ 라이선스 (License)

이 프로젝트는 **MIT License**를 따릅니다. 사용된 외부 라이브러리의 상세 라이선스 정보는 [LICENSE](./LICENSE) 파일에서 확인하실 수 있습니다.

This project is licensed under the **MIT License**.
For detailed license information of third-party libraries, please refer to the [LICENSE](./LICENSE) file.

© 2026 TagNamer. All rights reserved.
