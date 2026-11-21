// ================================================================================
// FILE: Cp437NfoHelper.cs
// PROJECT: RaggieSoft StoreTrainer 2.0 (StoreTrainer.csproj)
// LICENSE: MIT License
//
// PLAIN-ENGLISH OVERVIEW (FOR A 100-LEVEL BEGINNER):
//   Why do classic PC game trainer '.nfo' and 'file_id.diz' files look scrambled
//   when you open them in modern Windows Notepad?
//     In the 1990s and 2000s, trainer release groups drew elaborate ASCII borders
//     and shading using **IBM PC Code Page 437 (CP437)** — the original 8-bit
//     character set burned into IBM VGA graphics cards (where bytes 128–255 represent
//     box-drawing corners '╔══╗', shade blocks '░▒▓█', and symbols). Modern editors
//     assume UTF-8, turning those bytes into '' replacement diamonds!
//
//   What does this helper do (with ZERO external NuGet packages)?
//     1. 'ReadNfoText(filePath)': Checks if a file is valid UTF-8; if not, maps
//        every byte 0–255 through the exact IBM PC CP437 Unicode lookup table so
//        classic '.nfo' / '.diz' art renders crisp and authentic!
//     2. 'ExtractHotkeysFromText(nfoText)': Scans the '.nfo' or 'readme.txt' lines
//        for trainer shortcut patterns (like 'Numpad 1 ... Infinite Health',
//        'F1 - God Mode', or '[Ctrl + 2] : Unlimited Gold') and turns them straight
//        into structured 'HotkeyItem' rows for your Cheat Card!
// ================================================================================

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using StoreTrainer.Models;

namespace StoreTrainer.Services;

public static partial class Cp437NfoHelper
{
    // Exact 128-character Unicode mapping for IBM PC Code Page 437 upper bytes (0x80..0xFF).
    private static readonly char[] Cp437HighTable =
    [
        'Ç', 'ü', 'é', 'â', 'ä', 'à', 'å', 'ç', 'ê', 'ë', 'è', 'ï', 'î', 'ì', 'Ä', 'Å',
        'É', 'æ', 'Æ', 'ô', 'ö', 'ò', 'û', 'ù', 'ÿ', 'Ö', 'Ü', '¢', '£', '¥', '₧', 'ƒ',
        'á', 'í', 'ó', 'ú', 'ñ', 'Ñ', 'ª', 'º', '¿', '⌐', '¬', '½', '¼', '¡', '«', '»',
        '░', '▒', '▓', '│', '┤', '╡', '╢', '╖', '╕', '╣', '║', '╗', '╝', '╜', '╛', '┐',
        '└', '┴', '┬', '├', '─', '┼', '╞', '╟', '╚', '╔', '╩', '╦', '╠', '═', '╬', '╧',
        '╨', '╤', '╥', '╙', '╘', '╒', '╓', '╫', '╪', '┘', '┌', '█', '▄', '▌', '▐', '▀',
        'α', 'ß', 'Γ', 'π', 'Σ', 'σ', 'µ', 'τ', 'Φ', 'Θ', 'Ω', 'δ', '∞', 'φ', 'ε', '∩',
        '≡', '±', '≥', '≤', '⌠', '⌡', '÷', '≈', '°', '∙', '·', '√', 'ⁿ', '²', '■', '\u00A0'
    ];

    // IBM PC CP437 graphical glyphs for control bytes 0x01..0x1F (excluding \t, \n, \r).
    private static readonly char[] Cp437LowGlyphs =
    [
        ' ', '☺', '☻', '♥', '♦', '♣', '♠', '•', '◘', '○', '◙', '♂', '♀', '♪', '♫', '☼',
        '►', '◄', '↕', '‼', '¶', '§', '▬', '↨', '↑', '↓', '→', '←', '∟', '↔', '▲', '▼'
    ];

    /// <summary>
    /// Reads a <c>.nfo</c>, <c>.diz</c>, or <c>.txt</c> file, decoding MS-DOS CP437 art cleanly.
    /// </summary>
    public static string ReadNfoText(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return string.Empty;
        }

        var rawBytes = File.ReadAllBytes(filePath);

        // If the file starts with a UTF-8 BOM (EF BB BF), read it as UTF-8.
        if (rawBytes.Length >= 3 && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(rawBytes, 3, rawBytes.Length - 3);
        }

        // For '.txt' files that happen to be valid strict UTF-8 without high-ASCII CP437 art, prefer UTF-8;
        // for '.nfo' and '.diz' files (or any file with invalid UTF-8 high bytes), decode via CP437!
        if (!filePath.EndsWith(".nfo", StringComparison.OrdinalIgnoreCase) &&
            !filePath.EndsWith(".diz", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                return strictUtf8.GetString(rawBytes);
            }
            catch
            {
                // Fall through to authentic CP437 decoding!
            }
        }

        var sb = new StringBuilder(rawBytes.Length);
        foreach (var b in rawBytes)
        {
            if (b == 0x09 || b == 0x0A || b == 0x0D)
            {
                sb.Append((char)b);
            }
            else if (b < 0x20)
            {
                sb.Append(Cp437LowGlyphs[b]);
            }
            else if (b < 0x7F)
            {
                sb.Append((char)b);
            }
            else if (b == 0x7F)
            {
                sb.Append('⌂');
            }
            else
            {
                sb.Append(Cp437HighTable[b - 0x80]);
            }
        }
        return sb.ToString();
    }

    // Matches common trainer readme hotkey lines like:
    //   "Numpad 1   - Infinite Health"
    //   "[F2]       : Unlimited Ammo"
    //   "Ctrl+Num 3 ... Instant Build"
    [GeneratedRegex(
        @"^\s*[\[\(\*•│║]*\s*(?<key>(?:(?:Ctrl|Alt|Shift)\s*\+\s*)?(?:Num(?:pad)?\s*(?:[0-9\+\-\*/\.]|Add|Sub)|F(?:1[0-2]|[1-9])|Insert|Home|PgUp|PgDn|Delete|End|Backspace))\s*[\]\)]*\s*(?:[-:=~>]+|\.{2,})\s*(?<desc>[A-Za-z0-9][^\│║\r\n]{2,65})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TrainerHotkeyLineRegex();

    /// <summary>
    /// Scans <c>.nfo</c> or <c>readme.txt</c> content and extracts clean hotkey/effect pairs.
    /// </summary>
    public static List<HotkeyItem> ExtractHotkeysFromText(string text)
    {
        var list = new List<HotkeyItem>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return list;
        }

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in text.Split('\n'))
        {
            // Strip decorative outer CP437 border bars before matching
            var trimmed = rawLine.Trim('\r', ' ', '\t', '║', '│', '█', '▓', '▒', '░', '*');
            var match = TrainerHotkeyLineRegex().Match(trimmed);
            if (!match.Success)
            {
                continue;
            }

            var key = Regex.Replace(match.Groups["key"].Value.Trim(), @"\s+", " ");
            var desc = match.Groups["desc"].Value.Trim(' ', '\t', '.', '*', '║', '│');
            if (key.Length > 0 && desc.Length >= 3 && seenKeys.Add(key))
            {
                list.Add(new HotkeyItem
                {
                    Keys = key,
                    Effect = desc
                });
            }
        }

        return list;
    }
}
