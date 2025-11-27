# Localization System Mini for Unity
[FEATURES](#features) | [QUICK START](#quick-start) | [CSV FORMAT](#csv-format) | [FONTS](#font-management) | [API](#api-reference) | [DEMO SCENE](#example-scene) | [NOTES](#notes) | [LICENSE](#license) | [SUPPORT](#support)

A simple CSV-based text localization system for Unity projects using TextMeshPro. Features automatic font switching, dynamic placeholders, optimized with caching.

> **Source:** This is a simplified version of the localization system from [Automatic Tutorial Maker](https://u3d.as/3tsL) asset.

<p align="center">
  <img width="30%" alt="CSV Table Import" src="https://github.com/user-attachments/assets/0001a6ec-6736-460a-b35c-de63d1625ee5" />
  <img width="30%" alt="ScriptableObject Setup" src="https://github.com/user-attachments/assets/e67125dc-90b6-472c-be7d-437ecc495f95" />  
  <img width="30%" alt="Method Calls Demo" src="https://github.com/user-attachments/assets/669d0aff-1462-4d37-9a2e-8d83727a6399" />
</p>

## Features

- **CSV-based translations**  
  Edit in Excel, Google Sheets, or any CSV editor. Default: `;` delimiter, UTF-8, `"` text qualifier.

- **12+ languages included**  
  English, Chinese, Japanese, German, French, Spanish, Portuguese, Italian, Ukrainian, Polish, Turkish, Korean. Add custom via `Language` enum or column name.

- **Automatic font switching**  
  Assign fonts per language (e.g., NotoSansTC for Chinese), auto-applies on change. Includes Noto Sans CJK + Audex fonts with licenses.

- **Dynamic placeholders**  
  Insert variables: `{playerName}`, `{score}`, `{anyVariable}` → filled at runtime.

- **TextMeshPro tags support**  
  Use `<color=#FF0000>`, `<sprite=0>`, emojis 😊 directly in translations.

- **Two usage modes:**
  - **Static** – Cache text objects, auto-update on language change, including font switching (efficient for menus/UI)
  - **Dynamic** – Read strings per-frame, no built-in font switching (for scores/timers, cached lookups)

## Quick Start

### 1. Setup

1. Import the package into your Unity project
2. Locate the pre-configured `InputData` asset in `Assets/CSV Localization/InputData`
3. Set **Default Language** in the Inspector (English by default)
4. Assign your fonts in the Inspector:
   - **Default Font** for Latin-based languages
   - **Language Special Fonts** for CJK languages (Chinese, Japanese, Korean)
5. Add translations to the included CSV file (see format below)

> **Note:** You can create your own InputData asset (`Right-click → Create → Localization System Mini → InputData`) and CSV file if needed.

### 2. Initialize
```csharp
using LocalizationSystemMini;

public class GameManager : MonoBehaviour
{
    [SerializeField] private InputStringsScriptableObject textStrings;
    
    void Start()
    {
        // Initialize localization system (loads saved language or sets default)
        textStrings.InitializeLocalizator();
    }
}
```

### 3. Use in Code

> **Row Indexing:** Visual editors (Excel, Google Sheets) start from row 1, but code uses 0-based indexing. When specifying a row number, subtract 1 from what you see in the editor (e.g., row 2 in Excel = index 1 in code). Alternatively, use keys instead of row numbers. This is more reliable and allows you to swap table rows.

**Static Mode** – caches text objects, auto-updates on language change, handles font switching  
*Call once: on Start, or when spawning text objects*
```csharp
// Simple text
textStrings.FillTextObject(2, textComponent);

// By key (uses column A)
textStrings.FillTextObjectByKey("settings_key", textComponent);

// With placeholders (requires manual re-call if variables change)
textStrings.FillTextObjectWithPlaceholders(2, textComponent, userName, score);
```

**Dynamic Mode** – reads on-demand, no font switching  
*Call in Update() or when data changes frequently*
```csharp
void Update()
{
    // By row index
    textComponent.text = textStrings.GetStringText(2);
    
    // By key
    textComponent.text = textStrings.GetStringTextByKey("greeting_key");
    
    // With placeholders
    textComponent.text = textStrings.ReplacePlaceholders(3, playerName, level);
    
    // Manual font update (call once, if needed)
    textComponent.font = textStrings.GetCurrentFont();
}
```

**Change Language** – bind to UI buttons for language switching
```csharp
// By enum
textStrings.ChangeLanguage(InputStringsScriptableObject.Language.Japanese);

// By string (recommended for custom languages)
textStrings.ChangeLanguage("Ukrainian");
```

## CSV Format

|   | <sub>A</sub> | <sub>B</sub>     | <sub>C</sub>     | <sub>D</sub>     | <sub>E</sub>     | <sub>F</sub>        | <sub>...</sub> |
|---|--------------|------------------|------------------|------------------|------------------|---------------------|----------------|
| <sub>1</sub> | Use keys     | Default          | English          | Chinese          | Japanese          | German              | ...            |
| <sub>2</sub> | greeting_key | hello            | hello            | 你好             | こんにちは         | hallo               | ...            |
| <sub>3</sub> | welcome_key  | Welcome, {name}! | Welcome, {name}! | 欢迎，{name}！    | ようこそ、{name}！ | Willkommen, {name}! | ...            |
| <sub>4</sub> | yes_key      | Yes              | Yes              | 是               | はい              | Ja                  | ...            |

- First column: unique keys (for `GetStringTextByKey`)
- First row: language names to switch by name
- Second column: default fallback values (in the full Automatic Tutorial Maker version, auto-generated tutorial hints are stored here)
- Other columns: language translations (column order = enum Language order)
- **Placeholders:** Use `{variableName}` syntax. Position in sentence can vary between languages, but **order must be identical** in all translations.

> **Important:** Placeholders are replaced by **position, not by name**. The system replaces the 1st placeholder with the 1st value, 2nd with 2nd value, etc., regardless of what's inside the braces.
> 
> **Example with call:** `ReplacePlaceholders(row, "John", 100)`
> 
> ✅ **Correct:**  
> EN: `"Player {name} has {score} coins"` → "Player John has 100 coins"  
> CN: `"{name}有{score}金币"` → "John有100金币"  
> *(Same order: {name} first, {score} second)*
> 
> ❌ **Wrong:**  
> CN: `"{score}金币属于{name}"` → "John金币属于100" *(nonsense!)*  
> *(Reversed order: system replaces {score} with "John", {name} with 100)*

## Font Management

For languages with special characters (Chinese, Japanese, Korean):

1. In the InputData Inspector → **Language Special Fonts**:
   - Add language name: `Chinese`
   - Assign TMP_FontAsset: `NotoSansTC-Regular SDF`
2. Set **Default Font** for all other languages

The system automatically switches fonts when language changes.

## API Reference

### Methods

| Method | Description | Use Case |
|--------|-------------|----------|
| `InitializeLocalizator()` | Load saved language or set default | Call once on game start |
| `ChangeLanguage(Language)` | Switch to enum language | Testing in Inspector |
| `ChangeLanguage(string)` | Switch by string name | UI language buttons, custom languages |
| `GetStringText(int row)` | Get text by row | Dynamic reading |
| `GetStringTextByKey(string key)` | Get text by key | Dynamic reading |
| `FillTextObject(int row, TMP_Text)` | Cache text object | Static UI |
| `ReplacePlaceholders(int row, params object[])` | Get text with variables | Dynamic with variables |
| `FillTextObjectWithPlaceholders(int row, TMP_Text, params object[])` | Cache text with variables | Static with variables |
| `GetCurrentFont()` | Get current language font | Manual font management |

### Properties

`_currentLanguageString` – Current language as string. Track this to detect language changes in external scripts (e.g., `if (language != textStrings._currentLanguageString)`).

## Example Scene

Open `DemoScene` to see all features in action.

### Language Switching
- Flag buttons to switch between languages
- Automatic font switching for CJK languages

### Dynamic Mode Examples
- DynamicHelloText – simple dynamic text (*try changing "User Name" field in Play Mode*)
- DynamicVarText – dynamic text with variables (*try changing "Dynamic Value" field in Play Mode*)

### Static Mode Examples
- StaticTipText – static text with auto-update on language change
- StaticVarText – static text with variables (*try switching language to see variables update automatically*)

### Font Management
- Compare automatic font switching (static mode) vs manual (dynamic mode)
- See implementation in the `TextCallExamples` component

### Learning Resources

Examine `TextCallExamples.cs` for implementation patterns:
- How to use both static and dynamic modes
- When to cache vs read on-demand
- How to handle font switching manually

## Notes

- **Saved language**  
  Stored in `Application.persistentDataPath/LocalizationFolder/language_global.json`  
  Use the **"Open Saved Language File"** button in the InputData Inspector to navigate to this folder.

- **Performance**  
  - Static mode: one-time setup, cached in memory
  - Dynamic mode: per-frame reading with automatic caching for frequently accessed strings
  - Dictionary-based lookups optimize repeated reads

- **Custom languages**  
  Add new languages by:
  1. Adding columns to CSV file
  2. Extending the `Language` enum in `InputStringsScriptableObject.cs`  
  OR use `ChangeLanguage(string)` without modifying the enum

## License

Free to use in personal and commercial projects.

## Support

For the full-featured version, visit:  
**[Automatic Tutorial Maker on Unity Asset Store](https://u3d.as/3tsL)**

[![YouTube](https://img.shields.io/badge/YouTube-Watch_Demo-red?style=for-the-badge&logo=youtube)](https://youtu.be/8RE4LOaLAI4)

[FEATURES](#features) | [QUICK START](#quick-start) | [CSV FORMAT](#csv-format) | [FONTS](#font-management) | [API](#api-reference) | [DEMO SCENE](#example-scene) | [NOTES](#notes) | [LICENSE](#license) | [SUPPORT](#support)
