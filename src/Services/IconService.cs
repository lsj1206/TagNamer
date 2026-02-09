using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace TagNamer.Services;

/// <summary>
/// Windows Shell API를 사용하여 시스템 아이콘을 추출하는 서비스입니다.
/// </summary>
public class IconService : IIconService
{
    private readonly Dictionary<string, ImageSource> _iconCache = new();

    // Shell API 구조체 및 함수 정의
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_SMALLICON = 0x000000001;
    private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public ImageSource? GetIcon(string path, bool isFolder)
    {
        string key = isFolder ? "::folder::" : Path.GetExtension(path).ToLower();

        // 간단한 캐싱 (확장자 기준)
        if (_iconCache.TryGetValue(key, out var cachedIcon))
        {
            return cachedIcon;
        }

        IntPtr hIcon = IntPtr.Zero;
        try
        {
            var shinfo = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_SMALLICON;

            // 실제 파일이 없어도 아이콘을 가져올 수 있도록 USEFILEATTRIBUTES 사용 (가상 경로 대응)
            flags |= SHGFI_USEFILEATTRIBUTES;
            uint attributes = isFolder ? 0x00000010u : 0x00000080u; // FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL

            SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            hIcon = shinfo.hIcon;

            if (hIcon != IntPtr.Zero)
            {
                var iconSource = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                iconSource.Freeze(); // 크로스 스레드 안전을 위해 프리즈
                _iconCache[key] = iconSource;
                return iconSource;
            }
        }
        catch
        {
            // 오류 발생 시 기본값 null
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
            {
                DestroyIcon(hIcon);
            }
        }

        return null;
    }
}
