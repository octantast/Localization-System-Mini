using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace LocalizationSystemMini
{
    // All you need in this class is a few calls to #region Methods To Call Manually - Localization
    [CreateAssetMenu(fileName = "InputData", menuName = "Localization System Mini/InputData")]
    public class InputStringsScriptableObject : ScriptableObject
    {
        #region Variables
        [Header("CSV Source")]
        [Tooltip("CSV file containing translation data")]
        public TextAsset csvFile; // CSV file containing translation data

        [Tooltip("Delimiter used in the CSV file")]
        [ReadOnly] public string Delimiter = ";";
        [Tooltip("File origin to open CSV file")]
        [ReadOnly] public string FileOrigin = "Unicode (UTF-8)";
        [Tooltip("File origin to open CSV file")]
        [ReadOnly] public string TextQualifier = "\"";

        [Header("Font Source")]

        [Tooltip("The base font to apply when switching away from CJK characters")]
        // The font that automatically becomes the font of the resulting text object and is applied for a language set after exceptional ones
        [SerializeField] private TMP_FontAsset defaultFont;
        [NonSerialized] private TMP_FontAsset previousFont;

        [Tooltip("List of fonts used for languages that require special characters")]
        // List of fonts required to be assigned to TMP_Text when switching to the corresponding language
        [SerializeField] private List<TextFontPair> languageSpecialFonts = new List<TextFontPair>();

        [Header("Language")]

        // The default language that is set when the game starts if autosetLanguage is enabled
        // This font will also be applied to tooltips when switching from a special font to a language that does not require a special font (e.g., from Chinese to Polish)
        [Tooltip("Default language for text display. Set on the first launch of the build if autosetLanguage")]
        public Language _defaultLanguage = Language.English; // Default language

        //This field can be manually changed in the editor to test translations outside of Play Mode.
        //Play Mode itself, when launched (if autosetLanguage is enabled), will use the language the player set from the scene (or the default if the language in the Game was never changed).
        [Tooltip("Currently selected language for text display")]
        public Language _currentLanguage = Language.English; // Currently selected language     
        [NonSerialized] [ReadOnly] public string _currentLanguageString; // Currently selected language. The system relies on it when performing translations
        [NonSerialized] private int _customLanguageColumnIndex = -1; // Store custom language column index

        // In the build, changes to ScriptableObject at runtime are not saved between game sessions without additional logic
        // These particular variables should not be persisted between sessions to ensure correct initialization for each new session
        [NonSerialized] private List<TextColumnPair> staticTextPairs = new List<TextColumnPair>(); // List of additional elements
        [NonSerialized] private List<DynamicTextColumnPair> dynamicTextPairs = new List<DynamicTextColumnPair>();
        [NonSerialized] private bool preparedForLocalization = false;

        // CSV Parsing
        [NonSerialized] private string[] lines; // Array of lines from CSV file
        [NonSerialized] private List<string[]> cachedColumns;
        [NonSerialized] private static Regex multipleSpaces = new Regex(@"\s{2,}", RegexOptions.Compiled);

        // Caching calls to optimize GetStringText
        [NonSerialized] private Dictionary<(int row, int column), string> cellValueCache = new Dictionary<(int, int), string>();
        [NonSerialized] private string cachedLanguage;

        // Caching calls to optimize ReplacePlaceholders
        [NonSerialized] private Dictionary<(int row, string language, string paramsHash), string> formattedTextCache = new Dictionary<(int, string, string), string>();

        // Cache data
        private long lastFileModificationTime;
        private long lastFileSize;
        private int minColumn;
        private int maxColumn;

        // A mapping between the language name and its unique font
        // For example, add an element like Chinese_SpecialFontRequired and assign a Chinese font for automatic substitution during translations
        [Serializable]
        public class TextFontPair
        {
            public string languageName;
            public TMP_FontAsset fontAsset;
        }

        [Serializable]
        public class TextColumnPair
        {
            public TMP_Text rememberedText;
            public int tableRow;
            [ReadOnly] public string textValue;
        }

        [Serializable]
        public class DynamicTextColumnPair
        {
            public TMP_Text rememberedText;
            public int tableRow;
            public object[] placeholderValues; // Store the values for placeholders
            [ReadOnly] public string textValue; // Original text without placeholders replaced
            [ReadOnly] public string formattedText; // Text with placeholders replaced
        }

        // A set of predefined languages mapped to the columns of the CSV table
        // Define your language enum for new columns
        public enum Language
        {
            English = 2, // English language column index
            Chinese = 3, // Chinese language column index
            Japanese = 4, // Japanese language column index
            German = 5, // German language column index
            French = 6, // French language column index
            Spanish = 7, // Spanish language column index
            Portuguese = 8, // Portuguese language column index
            Italian = 9, // Italian language column index
            Ukrainian = 10, // Ukrainian language column index
            Polish = 11, // Polish language column index
            Turkish = 12, // Turkish language column index
            Korean = 13, // Korean language column index
            AddCustomLanguage = 14, // Add yours
            FoundByString = -1 // Special value for custom language
        }

        #endregion

        #region Methods To Call Manually - Localization

        // Sets a flag for future calls from individual LocalizedTooltips
        // Use this method at the Start() to load language and allow reading
        // Call inputStrings.InitializeLocalizator();
        public void InitializeLocalizator()
        {
            if (!preparedForLocalization)
            {
                // If there is no loaded language, it will download it automatically or install the default one
                if (string.IsNullOrEmpty(_currentLanguageString))
                {
                    string folderPath = Path.Combine(Application.persistentDataPath, "LocalizationFolder");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                    string filePath = Path.Combine(folderPath, "language_global.json");

                    if (File.Exists(filePath))
                    {
                        string json = File.ReadAllText(filePath);
                        var data = JsonUtility.FromJson<SavedLanguage>(json);

                        UpdateCurrentLanguage(data.currentSavedLanguage);
                    }
                    else
                    {
                        var defaultData = new SavedLanguage();
                        defaultData.currentSavedLanguage = _defaultLanguage.ToString();
                        string json = JsonUtility.ToJson(defaultData, prettyPrint: true);
                        File.WriteAllText(filePath, json);

                        UpdateCurrentLanguage(defaultData.currentSavedLanguage);
                        Debug.Log($"[Localization System Mini] No language file found -> Created default file: {filePath}.\nDefault language set: {_currentLanguageString}");
                    }
                    Debug.Log("[Localization System Mini] Language loaded: " + _currentLanguageString);

                    if (Enum.TryParse<InputStringsScriptableObject.Language>(_currentLanguageString, out InputStringsScriptableObject.Language parsedLanguage))
                    {
                        ChangeLanguage(parsedLanguage);
                    }
                    else
                    {
                        ChangeLanguage(_currentLanguageString);
                    }

                }

                preparedForLocalization = true;

                // If there were requests before initialization, we process them here
                if (staticTextPairs.Count > 0)
                {
                    TranslateAllTexts();
                }
            }
        }

        // Changes the current language by string name and refreshes all tip strings
        // This approach is more reliable for working in a build: the language columns can be moved
        // Use any language name, such as Italian, as long as the table contains a column with that header
        // Call inputStrings.ChangeLanguage("Polish");
        public void ChangeLanguage(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
            {
                Debug.LogError("[Localization System Mini] Language name cannot be empty!");
                return;
            }

            // First check if this is a standard language in our enum
            foreach (Language lang in Enum.GetValues(typeof(Language)))
            {
                if (lang.ToString().Equals(languageName, StringComparison.OrdinalIgnoreCase))
                {
                    ChangeLanguage(lang);
                    return;
                }
            }

            // If not found in enum, search in the CSV headers
            int columnIndex = FindLanguageColumnIndex(languageName);

            Language matchedLanguage = Enum.GetValues(typeof(Language))
                                            .Cast<Language>()
                                            .FirstOrDefault(lang => (int)lang == columnIndex);

            if (Enum.IsDefined(typeof(Language), matchedLanguage) || (int)matchedLanguage == columnIndex)
            {
                ChangeLanguage(matchedLanguage);
                return;
            }

            if (columnIndex >= 0)
            {
                _currentLanguage = Language.FoundByString;
                SaveLanguage(languageName);
                TranslateAllTexts();
                Debug.Log($"[Localization System Mini] Changed to custom language '{languageName}' at column index {columnIndex}");
            }
            else
            {
                Debug.LogError($"[Localization System Mini] Language '{languageName}' not found in CSV headers!");
            }
        }

        // Changes the current language and refreshes all tip strings
        // This approach is easier to test in the inspector: switch between languages
        // Use the predefined Language values with the corresponding table columns
        // Call inputStrings.ChangeLanguage(Language.German);
        public void ChangeLanguage(Language language)
        {
            _currentLanguage = language;
            SaveLanguage(language.ToString());
            TranslateAllTexts();
        }

        // Returns the current language font for manual replacement
        // Call the method for texts that do not pass their text object to the store and simply read variables
        // Call once via objectText.font = inputTextSettings.GetCurrentFont();
        public TMP_FontAsset GetCurrentFont()
        {
            TMP_FontAsset fontAsset = DefineTargetFont();
            return fontAsset;
        }

        // These methods find the text to assign based on the corresponding row in the table, not in scriptable object list. In the current language
        // When selecting the row number, note that counting in visual redactors starts from 1, and code is 0-based
        // If you want to get the text from row 60 of the table, you should refer to it as row 59. Example: inputTextSettings.FillTextObject(59, objectText);

        // GetString Text simply returns the value from a cell. It is intended to be called continuously
        // It doesn't involve font substitution or automatic translation
        // Use this method in update and frequent readings in combination with dynamic variables
        // Call via objectText.text = inputStrings.GetStringText(3) + "...;
        public string GetStringText(int rowIndex)
        {
            // If using custom language, use the custom column index
            int columnIndex = _currentLanguage == Language.FoundByString ? _customLanguageColumnIndex : (int)_currentLanguage;

            // Check if cache is invalid (language changed or cache not initialized)
            if (cachedLanguage != _currentLanguageString || cellValueCache == null)
            {
                ClearCellValueCache();
                cachedLanguage = _currentLanguageString;
            }

            // Check if value is already cached
            var cacheKey = (rowIndex, columnIndex);
            if (cellValueCache.TryGetValue(cacheKey, out string cachedValue))
            {
                return cachedValue;
            }

            // Value not in cache, extract and cache it
            string extractedValue = GetElementFromLine(rowIndex, columnIndex);
            cellValueCache[cacheKey] = extractedValue;
            // Debug.Log($"Value extracted: {extractedValue}");
            return extractedValue;
        }

        // FillTextObject fills an object with text and caches it so that it can be updated later when translation occurs
        // It was invented for possible additional texts for which no predefined text element is provided
        // Call the method when you spawn a text object on the scene with static data ("Skip tutorial", "Settings", etc)
        // Call once via inputTextSettings.FillTextObject(connectedTableRow, objectText);
        public void FillTextObject(int rowIndex, TMP_Text textToFill)
        {
            string textValue = "";

            if (textToFill == null)
            {
                return;
            }

            textValue = GetStringText(rowIndex);
            SetTextDirectly(textValue, textToFill);

            AddOrUpdateTextPair(textToFill, rowIndex, textValue);
        }

        // GetStringTextByKey finds text by key in the first column and returns the value in current language
        // This is useful when you want to reference texts by meaningful keys instead of row numbers
        // Call via objectText.text = inputStrings.GetStringTextByKey("main_menu_play");
        public string GetStringTextByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Localization System Mini] Key cannot be null or empty!");
                return "";
            }
                       
            // Find row index by searching in the first column
            int rowIndex = FindRowIndexByKey(key);

            if (rowIndex >= 0)
            {
                return GetStringText(rowIndex);
            }
            else
            {
                Debug.LogWarning($"[Localization System Mini] Key '{key}' not found in the first column!");
                return "";
            }
        }

        // FillTextObjectByKey fills a text object by finding the key in the first column
        // Call once via inputStrings.FillTextObjectByKey("settings_title", settingsText);
        public void FillTextObjectByKey(string key, TMP_Text textToFill)
        {
            if (textToFill == null) return;

            string textValue = GetStringTextByKey(key);
            if (!string.IsNullOrEmpty(textValue))
            {
                SetTextDirectly(textValue, textToFill);
                AddOrUpdateTextPair(textToFill, FindRowIndexByKey(key), textValue);
            }
        }

        #endregion

        #region Advanced Methods To Call Manually - Dynamic Variables Support

        // Gets string text from the row and replaces ALL curly brace placeholders {} with provided values in order
        // Example: If CSV cell contains "Player {playerName} has {coins} coins" in row 2
        // Call string value = inputStrings.ReplacePlaceholders(2, "John", 100);
        // Call string value = inputStrings.ReplacePlaceholders(2, userName, coins);
        public string ReplacePlaceholders(int rowIndex, params object[] values)
        {
            // Create a hash of the placeholder values for cache key
            string paramsHash = ComputeValuesHash(values);
            var cacheKey = (rowIndex, _currentLanguageString, paramsHash);

            // Check if formatted text is already cached
            if (formattedTextCache.TryGetValue(cacheKey, out string cachedFormattedText))
            {
                return cachedFormattedText;
            }

            // Not in cache, process normally
            string extractedValue = GetStringText(rowIndex);

            if (string.IsNullOrEmpty(extractedValue))
                return extractedValue;

            string formattedText = ReplacePlaceholdersInText(extractedValue, values);

            // Cache the result
            formattedTextCache[cacheKey] = formattedText;
            return formattedText;
        }

        // Fills a TextMeshPro component with text that includes dynamic placeholders
        // Placeholders in {} will be replaced with provided values in order of appearance
        // The text will be automatically updated when language changes
        // Example: If CSV cell contains "Player {playerName} has {coins} coins" in row 2
        // Call inputStrings.FillTextObjectWithPlaceholders(5, myTextComponent, "John", 100);
        public void FillTextObjectWithPlaceholders(int tableRow, TMP_Text textToFill, params object[] values)
        {
            if (textToFill == null) return;

            string textValue = GetStringText(tableRow);
            string formattedText = ReplacePlaceholdersInText(textValue, values);

            SetTextDirectly(formattedText, textToFill);

            // Cache for future translations
            AddOrUpdateDynamicTextPair(textToFill, tableRow, values, formattedText);
        }

        // Method for replacing placeholders by key (string) instead of string index
        public string ReplacePlaceholdersByKey(string key, params object[] values)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Localization System Mini] Key cannot be null or empty!");
                return "";
            }

            int rowIndex = FindRowIndexByKey(key);

            if (rowIndex < 0)
            {
                Debug.LogWarning($"[Localization System Mini] Key '{key}' not found in the first column!");
                return "";
            }

            return ReplacePlaceholders(rowIndex, values);
        }

        // Fills the TextMeshPro component with text with dynamic placeholders using a key instead of an index
        public void FillTextObjectWithPlaceholdersByKey(string key, TMP_Text textToFill, params object[] values)
        {
            if (textToFill == null)
            {
                Debug.LogWarning("[Localization System Mini] TextToFill cannot be null!");
                return;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Localization System Mini] Key cannot be null or empty!");
                return;
            }

            int rowIndex = FindRowIndexByKey(key);

            if (rowIndex < 0)
            {
                Debug.LogWarning($"[Localization System Mini] Key '{key}' not found in the first column!");
                return;
            }

            FillTextObjectWithPlaceholders(rowIndex, textToFill, values);
        }

        #endregion

        #region Localization Helpers

        // Writes the language selected in the build/play mode to JSON for persistence between sessions
        private void SaveLanguage(string language)
        {
            // Checks whether anything has changed
            if (_currentLanguageString == language || string.IsNullOrEmpty(language))
            {
                return;
            }

            UpdateCurrentLanguage(language);

            // Saves changes if enabled

            var data = new SavedLanguage();
            data.currentSavedLanguage = language;
            string folderPath = Path.Combine(Application.persistentDataPath, "LocalizationFolder");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, "language_global.json");

            string json = JsonUtility.ToJson(data, prettyPrint: true);

            File.WriteAllText(filePath, json);
            Debug.Log("[Localization System Mini] Language set: " + _currentLanguageString);

        }

        // This method replaces the text in the given text field and, if necessary, replaces the font according to the current language
        private void SetTextDirectly(string textValue, TMP_Text textToFill)
        {
            if (textToFill && !string.IsNullOrEmpty(textValue))
            {
                textToFill.text = textValue;
                // The object adjusts the tooltip font only when switching between the special and the default font
                // Or in any case, the object adjusts the tooltip font on its own
                if (languageSpecialFonts != null && languageSpecialFonts.Count > 0)
                {
                    TextFontPair pair = languageSpecialFonts
                        .Where(p => !string.IsNullOrEmpty(p.languageName) && p.fontAsset != null)
                        .FirstOrDefault(p => p.languageName == _currentLanguageString);

                    if (pair?.fontAsset != null)
                    {
                        textToFill.font = pair.fontAsset;
                    }
                    else
                    {
                        textToFill.font = defaultFont;
                    }
                }
                else
                {
                    textToFill.font = defaultFont;
                }
            }
        }

        // Extract the element from a specific line and column in the CSV file
        private string GetElementFromLine(int row, int column)
        {
            if (cachedColumns == null || cachedColumns.Count == 0)
            {
                CacheLines();
            }

            string extractedValue = "";

            if (cachedColumns.Count > row && row >= 0)
            {
                string[] columns = cachedColumns[row];
                if (columns.Length > column && column >= 0)
                {
                    extractedValue = columns[column];
                }
                else
                {
                    Debug.LogWarning($"[Localization System Mini] Row {row} does not contain an element at column {column}. Columns available: {columns.Length}");
                    extractedValue = "";
                }
            }
            else
            {
                Debug.LogWarning($"[Localization System Mini] Not enough rows in the CSV file. Requested row: {row}, Total rows: {cachedColumns.Count}");
                extractedValue = "";
            }
            return extractedValue;
        }

        // Helper method to find row index by key
        private int FindRowIndexByKey(string key)
        {
            if (cachedColumns == null || cachedColumns.Count == 0)
            {
                CacheLines();
            }

            for (int i = 0; i < cachedColumns.Count; i++)
            {
                if (cachedColumns[i].Length > 0 &&
                    cachedColumns[i][0].Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        // Updates all text hints it knows
        private void TranslateAllTexts()
        {
            RefreshManuallyConnectedTexts();
            RefreshDynamicTexts();
        }

        // Specifies a font for text based on a list of exceptions according to the current language
        private TMP_FontAsset DefineTargetFont()
        {
            TMP_FontAsset fontAsset = null;

            // The object adjusts the tooltip font only when switching between the special and the default font
            // Or in any case, the object adjusts the tooltip font on its own
            if (languageSpecialFonts != null && languageSpecialFonts.Count > 0 && !string.IsNullOrEmpty(_currentLanguageString))
            {
                TextFontPair pair = languageSpecialFonts
                    .Where(p => !string.IsNullOrEmpty(p.languageName) && p.fontAsset != null)
                    .FirstOrDefault(p => p.languageName == _currentLanguageString);

                if (pair?.fontAsset != null)
                {
                    fontAsset = pair.fontAsset;
                }
                else
                {
                    fontAsset = defaultFont;
                }
            }

            else
            {
                fontAsset = defaultFont;
            }

            if (previousFont != fontAsset)
            {
                previousFont = fontAsset;
                Debug.Log($"[Localization System Mini] Current font for all avaliable texts is set: {fontAsset}.");
            }
            return fontAsset;
        }

        // Remembers a manually called string assigned to a text component
        private void AddOrUpdateTextPair(TMP_Text textToFill, int tableRow, string textValue)
        {
            if (textToFill == null)
            {
                return;
            }

            List<TextColumnPair> toRemove = new List<TextColumnPair>();

            TextColumnPair existingPair = null;
            foreach (var pair in staticTextPairs)
            {
                if (pair.rememberedText == null)
                {
                    toRemove.Add(pair);
                }
                else if (pair.rememberedText == textToFill)
                {
                    existingPair = pair;
                }
            }

            foreach (var pair in toRemove)
            {
                staticTextPairs.Remove(pair);
            }

            if (existingPair != null)
            {
                existingPair.tableRow = tableRow;
            }
            else
            {
                TextColumnPair newPair = new TextColumnPair
                {
                    textValue = textValue,
                    rememberedText = textToFill,
                    tableRow = tableRow
                };
                staticTextPairs.Add(newPair);
            }
        }

        // In case of translation, it automatically replaces all active texts with the translated ones and, if necessary, changes the font
        private void RefreshManuallyConnectedTexts()
        {
            TMP_FontAsset fontAsset = DefineTargetFont();

            if (staticTextPairs == null || staticTextPairs.Count == 0)
                return;

            List<TextColumnPair> toRemove = new List<TextColumnPair>();

            foreach (var pair in staticTextPairs)
            {
                if (pair.rememberedText == null)
                {
                    toRemove.Add(pair);
                }
                else
                {
                    pair.textValue = GetStringText(pair.tableRow);
                    pair.rememberedText.text = pair.textValue;
                    pair.rememberedText.font = fontAsset;
                }
            }

            foreach (var pair in toRemove)
            {
                staticTextPairs.Remove(pair);
            }
        }

        // Finds the column index for a language by its name in the CSV header
        private int FindLanguageColumnIndex(string languageName)
        {
            CacheLines();

            if (lines == null || lines.Length == 0)
            {
                Debug.LogError("[Localization System Mini] CSV file is empty or not loaded properly!");
                return -1;
            }

            // Assume the language names are in the first line (header)
            string headerLine = lines[0];
            string[] headers = headerLine.Split(Delimiter);

            // First, look for an exact match
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim().Equals(languageName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            // If no exact match is found, look for at least a partial match
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();

                if (header.Contains(languageName) || languageName.Contains(header))
                {
                    return i;
                }
            }

            return -1; // Language not found
        }

        // Overwrites strings for reading during a session
        private void UpdateCurrentLanguage(string languageData)
        {
            _currentLanguageString = languageData;

            // Updates _customLanguageColumnIndex
            int columnIndex = FindLanguageColumnIndex(_currentLanguageString);
            if (columnIndex >= 0)
            {
                _customLanguageColumnIndex = columnIndex;
            }

            // Clear cache when language changes
            ClearCellValueCache();
        }

        // Replaces placeholders in text with provided values
        private string ReplacePlaceholdersInText(string text, params object[] values)
        {
            if (string.IsNullOrEmpty(text) || values == null || values.Length == 0)
                return text;

            // Find all placeholders in curly braces
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"\{[^{}]+\}");

            if (matches.Count > 0)
            {
                string dynamicValue = text;
                for (int i = 0; i < Math.Min(matches.Count, values.Length); i++)
                {
                    string placeholder = matches[i].Value;
                    string value = values[i]?.ToString() ?? "";
                    dynamicValue = dynamicValue.Replace(placeholder, value);
                }
                return dynamicValue;
            }

            return text;
        }

        // Adds or updates a dynamic text pair in the cache
        private void AddOrUpdateDynamicTextPair(TMP_Text textToFill, int tableRow, object[] values, string formattedText)
        {
            if (textToFill == null) return;

            // Remove existing pairs with this text
            dynamicTextPairs.RemoveAll(pair => pair.rememberedText == textToFill);

            DynamicTextColumnPair newPair = new DynamicTextColumnPair
            {
                rememberedText = textToFill,
                tableRow = tableRow,
                placeholderValues = values,
                textValue = GetStringText(tableRow),
                formattedText = formattedText
            };
            dynamicTextPairs.Add(newPair);
        }

        // Refreshes all dynamic texts when language changes
        private void RefreshDynamicTexts()
        {
            if (dynamicTextPairs == null || dynamicTextPairs.Count == 0)
                return;

            TMP_FontAsset fontAsset = DefineTargetFont();
            List<DynamicTextColumnPair> toRemove = new List<DynamicTextColumnPair>();

            foreach (var pair in dynamicTextPairs)
            {
                if (pair.rememberedText == null)
                {
                    toRemove.Add(pair);
                }
                else
                {
                    // Get new translation
                    string newText = GetStringText(pair.tableRow);
                    // Apply the same placeholder values to the new translation
                    string newFormattedText = ReplacePlaceholdersInText(newText, pair.placeholderValues);

                    pair.rememberedText.text = newFormattedText;
                    if (fontAsset != null)
                        pair.rememberedText.font = fontAsset;

                    // Update cache
                    pair.textValue = newText;
                    pair.formattedText = newFormattedText;
                }
            }

            foreach (var pair in toRemove)
            {
                dynamicTextPairs.Remove(pair);
            }
        }
        #endregion

        #region CSV Parsing And Optimization

        // Caches CSV file lines for faster access
        // Is called in the game session once during initialization, turns the created CSV into an array
        private void CacheLines()
        {
            if (csvFile == null)
            {
                Debug.LogError($"[Localization System Mini] Failed to load CSV file.");
                return;
            }

            if (HasFileChanged(csvFile) || lines == null || lines.Length == 0)
            {
                // Continues reading if there is new data to read
            }
            else
            {
                return;
            }

            try
            {
                // Use improved CSV parser with current delimiter
                char delimiterChar = Delimiter.Length > 0 ? Delimiter[0] : ';';
                List<string[]> parsedData = ParseCsv(csvFile.text, delimiterChar);

                // Validate structure before proceeding
                ValidateCsvStructure(parsedData);

                if (parsedData.Count == 0)
                {
                    Debug.LogWarning("[Localization System Mini] Parsed CSV data is empty, using fallback parsing");
                    // Fallback to original simple parsing
                    lines = csvFile.text.Split('\n');
                    BuildColumnsCache();
                    return;
                }

                cachedColumns = parsedData;

                // Convert parsed data back to string[] format for compatibility
                lines = new string[parsedData.Count];
                for (int i = 0; i < parsedData.Count; i++)
                {
                    // Reconstruct line with proper delimiter
                    lines[i] = string.Join(Delimiter, parsedData[i]);
                }

                Debug.Log($"[Localization System Mini] CSV file successfully parsed: {parsedData.Count} rows, {parsedData[0].Length} columns");

                // Update column range calculations (existing logic)
                minColumn = int.MaxValue;
                foreach (Language lang in Enum.GetValues(typeof(Language)))
                {
                    if (lang != Language.FoundByString)
                    {
                        int value = (int)lang;
                        if (value >= 0)
                        {
                            minColumn = Math.Min(minColumn, value);
                        }
                    }
                }

                if (minColumn == int.MaxValue)
                {
                    minColumn = 0;
                }

                maxColumn = 0;
                if (lines.Length > 0)
                {
                    string headerLine = lines[0];
                    string[] headers = headerLine.Split(Delimiter);

                    for (int i = headers.Length - 1; i >= 0; i--)
                    {
                        if (!string.IsNullOrWhiteSpace(headers[i]))
                        {
                            maxColumn = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization System Mini] Critical error during CSV parsing: {ex.Message}");
            }
        }

        // Improved parser
        // Takes into account Delimiter inside quotes
        // Takes into account escaped quotes
        // Takes into account mixed quotes and delimiters
        // Takes into account multi-line fields
        // Takes into account empty fields
        // Takes into account trailing spaces
        // Takes into account leading spaces
        // Takes into account mixed whitespace
        // Takes into account special characters
        // Takes into account unicode characters
        // Takes into account only quotes
        // Takes into account multiple consecutive delimiters
        // Takes into account Windows line endings (simulated)
        private static List<string[]> ParseCsv(string text, char delimiter)
        {
            var result = new List<string[]>();

            // Handle empty input edge cases
            if (string.IsNullOrEmpty(text))
                return result;

            if (string.IsNullOrWhiteSpace(text))
            {
                // Return single empty row for whitespace-only input to maintain structure
                result.Add(new string[] { "" });
                return result;
            }

            var currentField = new System.Text.StringBuilder();
            var currentRow = new List<string>();

            bool inQuotes = false;

            try
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char currentChar = text[i];

                    // Handle carriage return tracking for Windows line endings (\r\n)
                    if (currentChar == '\r')
                    {
                        continue; // Skip CR, we'll handle the line ending at LF
                    }

                    if (inQuotes)
                    {
                        // Inside quoted field - most characters are taken literally
                        if (currentChar == '"')
                        {
                            // Check for escaped double quote ("")
                            if (i + 1 < text.Length && text[i + 1] == '"')
                            {
                                currentField.Append('"'); // Add single quote to field
                                i++; // Skip next quote
                            }
                            else
                            {
                                // Closing quote found
                                inQuotes = false;
                            }
                        }
                        else
                        {
                            // Normal character inside quotes (including delimiters and newlines)
                            currentField.Append(currentChar);
                        }
                    }
                    else
                    {
                        // Outside quoted field - special characters have meaning
                        if (currentChar == '"')
                        {
                            // Opening quote found
                            inQuotes = true;
                        }
                        else if (currentChar == delimiter)
                        {
                            // Field delimiter - complete current field and start new one
                            currentRow.Add(currentField.ToString());
                            currentField.Clear();
                        }
                        else if (currentChar == '\n')
                        {
                            // Line ending - complete current field and row
                            currentRow.Add(currentField.ToString());
                            currentField.Clear();

                            result.Add(currentRow.ToArray());
                            currentRow.Clear();
                        }
                        else
                        {
                            // Regular character - add to current field
                            currentField.Append(currentChar);
                        }
                    }
                }

                // Handle final field and row if file doesn't end with newline
                if (currentField.Length > 0 || currentRow.Count > 0)
                {
                    currentRow.Add(currentField.ToString());
                }

                if (currentRow.Count > 0)
                {
                    result.Add(currentRow.ToArray());
                }

                // Validate that we're not ending in an unterminated quoted field
                if (inQuotes)
                {
                    Debug.LogWarning("[Localization System Mini] CSV parsing warning: Unterminated quoted field detected at end of file");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization System Mini] CSV parsing error: {ex.Message}");
                // Return empty result to avoid corrupting existing data
                return new List<string[]>();
            }

            return result;
        }

        // Validates CSV structure consistency and logs warnings for potential issues
        private void ValidateCsvStructure(List<string[]> parsedData)
        {
            if (parsedData == null || parsedData.Count == 0)
            {
                Debug.LogWarning("[Localization System Mini] CSV validation: Empty or null parsed data");
                return;
            }

            // Check header row
            if (parsedData[0].Length == 0)
            {
                Debug.LogWarning("[Localization System Mini] CSV validation: Header row is empty");
                return;
            }

            int headerColumnCount = parsedData[0].Length;
            Debug.Log($"[Localization System Mini] CSV structure: {parsedData.Count} rows, {headerColumnCount} columns in header");

            // Check for inconsistent column counts in data rows
            for (int i = 1; i < parsedData.Count; i++)
            {
                if (parsedData[i].Length != headerColumnCount)
                {
                    Debug.LogWarning($"[Localization System Mini] CSV validation: Row {i + 1} has {parsedData[i].Length} columns, expected {headerColumnCount}. This may cause localization issues.");

                    // Log sample of the problematic row for debugging
                    string rowPreview = string.Join(", ", parsedData[i].Take(3));
                    if (parsedData[i].Length > 3) rowPreview += "...";
                    Debug.LogWarning($"[Localization System Mini] Problematic row preview: {rowPreview}");
                }
            }

            // Check for empty rows that might indicate parsing issues
            int emptyRows = parsedData.Count(row => row.Length == 1 && string.IsNullOrWhiteSpace(row[0]));
            if (emptyRows > 0)
            {
                Debug.LogWarning($"[Localization System Mini] CSV validation: Found {emptyRows} empty rows that may indicate parsing issues");
            }
        }

        // Builds a column cache for more optimized reading
        private void BuildColumnsCache()
        {
            if (lines == null || lines.Length == 0)
            {
                cachedColumns = new List<string[]>();
                return;
            }

            cachedColumns = new List<string[]>();
            char delimiterChar = Delimiter.Length > 0 ? Delimiter[0] : ';';

            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    cachedColumns.Add(new string[0]);
                }
                else
                {
                    cachedColumns.Add(line.Split(delimiterChar));
                }
            }
        }

        // Clear the cache when language changes or data is reloaded
        private void ClearCellValueCache()
        {
            cellValueCache?.Clear();
            formattedTextCache?.Clear(); 
            cachedLanguage = _currentLanguageString;
        }

        private string ComputeValuesHash(object[] values)
        {
            if (values == null || values.Length == 0)
                return "empty";

            return string.Join("|", values.Select(v => v?.ToString() ?? "null")).GetHashCode().ToString();
        }

        // Checks if the CSV file has been modified
        // Used in the editor during development when file replacement is possible
        private bool HasFileChanged(TextAsset csvAsset)
        {
#if UNITY_EDITOR
            if (csvAsset == null)
                return false;

            try
            {
                string filePath = UnityEditor.AssetDatabase.GetAssetPath(csvAsset);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);

                    bool timeChanged = fileInfo.LastWriteTimeUtc.Ticks > lastFileModificationTime;
                    bool sizeChanged = fileInfo.Length != lastFileSize;

                    if (timeChanged || sizeChanged)
                    {
                        lastFileModificationTime = fileInfo.LastWriteTimeUtc.Ticks;
                        lastFileSize = fileInfo.Length;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization System Mini] Error checking file: {ex.Message}");
            }
#endif
            return false;
        }

        #endregion

        #region Editor
#if UNITY_EDITOR
        [CustomEditor(typeof(InputStringsScriptableObject))]
        public class DialogueDataEditor : Editor
        {
            private Language lastLanguage; // Tracks the previous language selection

            // Called when the editor is enabled
            private void OnEnable()
            {
                var dialogueData = (InputStringsScriptableObject)target;
                lastLanguage = dialogueData._currentLanguage;
            }

            // Custom inspector GUI implementation
            public override void OnInspectorGUI()
            {
                InputStringsScriptableObject dialogueData = (InputStringsScriptableObject)target;
                var beforeLanguage = dialogueData._currentLanguage;

                EditorGUILayout.HelpBox("Add and translate your texts in the CSV table. Then, at runtime, initialize this object on startup and use it`s Methods To Call Manually to retrieve the localized texts.",
     MessageType.Info);
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 0.4f, 1f);

                if (GUILayout.Button("Open Saved Language File >"))
                {
                    OpenLanguageFileLocation(dialogueData);
                }

                GUI.backgroundColor = originalColor;

                EditorGUILayout.Space();

                DrawDefaultInspector();

                EditorGUILayout.Space();

                //GUI.backgroundColor = originalColor;
                if (dialogueData._currentLanguage != beforeLanguage)
                {
                    bool saveandrefresh = false;
                    if (dialogueData._currentLanguage == Language.FoundByString)
                    {
                        Debug.LogWarning("[Localization System Mini] Cannot directly select FoundByString language. Call ChangeLanguage(string languageName) instead.");
                        dialogueData._currentLanguage = beforeLanguage;
                        saveandrefresh = true;
                    }
                    else
                    {
                        Debug.Log($"[Localization System Mini] Language changed from {beforeLanguage} to {dialogueData._currentLanguage}");
                        saveandrefresh = true;

                    }
                    if (saveandrefresh)
                    {
                        dialogueData.ChangeLanguage(dialogueData._currentLanguage.ToString());
                        lastLanguage = dialogueData._currentLanguage;
                    }
                }
            }

            private void OpenLanguageFileLocation(InputStringsScriptableObject dialogueData)
            {
                string folderPath = Path.Combine(Application.persistentDataPath, "LocalizationFolder");
                if (!Directory.Exists(folderPath))
                {
                    EditorUtility.DisplayDialog("Folder Not Found",
                        "Saved language folder doesn't exist yet. Play the scene to create it.",
                        "OK");
                    return;
                }

                // Open folder in file explorer
                EditorUtility.RevealInFinder(folderPath);
            }
        }

#endif
        #endregion
    }

    #region Helper Classes
    [Serializable]
    class SavedLanguage
    {
        public string currentSavedLanguage;
    }

    // Custom attribute to mark properties as read-only in the Unity Inspector
    public class ReadOnlyAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    // Custom property drawer to implement read-only functionality in the Unity Editor
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        // Renders the property field as disabled, preventing user editing
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Temporarily disable GUI interaction
            GUI.enabled = false;
            // Draw the property field
            EditorGUI.PropertyField(position, property, label, true);
            // Restore GUI interaction
            GUI.enabled = true;
        }
    }
#endif
    #endregion
}