using TMPro;
using UnityEngine;
namespace LocalizationSystemMini
{
    public class TextCallExamples : MonoBehaviour
    {
        [SerializeField] private InputStringsScriptableObject textStrings; // Reference to scriptable object
        [SerializeField] private TMP_Text textObj;

        [Header("Simple Call With Dynamic Var")]
        [ReadOnly][SerializeField] private bool testDynamic;
        [SerializeField] private string userName = "User X";
        [ReadOnly][SerializeField] private int nameRow = 1;

        [Header("Initial Call Of Static Text")]
        [ReadOnly][SerializeField] private bool testStatic;
        [ReadOnly][SerializeField] private int tipRow = 2;

        [Header("Call With Dynamic Variables")]
        [ReadOnly][SerializeField] private bool testDynamicWithVariables;
        [SerializeField] private int dynamicValue = 5;
        [ReadOnly][SerializeField] private int variableRow = 4;

        [Header("Initial Call With Dynamic Variables")]
        [ReadOnly][SerializeField] private bool testStaticWithVariables;
        [ReadOnly][SerializeField] private string language;
        [ReadOnly][SerializeField] private string needsSpecialFont;
        [ReadOnly][SerializeField] private int noteRow = 3;

        private void Start()
        {
            // Initialize the translation system at the start of the project
            if (textStrings != null)
            {
                textStrings.InitializeLocalizator();
            }

            // At the start or when you spawn text on the scene that you don`t need to change often, use these methods
            // Static methods pass the text object to the cache and can change the font during translation

            if (testStatic)
            {
                // Call once via FillTextObject
                // Doesn`t track anything, changes automatically if the new language was set
                textStrings.FillTextObject(tipRow, textObj);
                enabled = false;
            }

            if (testStaticWithVariables)
            {
                // Call once via FillTextObjectWithPlaceholders
                // Doesn`t track anything, changes automatically only if the new language was set, YET variables have to be updated manually

                UpdateStaticWithVariables();                
            }
        }

        void Update()
        {
            if (textStrings != null)
            {
                // These dynamic methods are lightweight for continuous data reading
                // They don`t write text object to memory, so they do not automatically respond to translation or change the font
                // Font change for them can be added manually like here or where you call the language change

                if (testDynamic)
                {
                    // Constant call via GetStringText
                    // Doesn`t take the font, just returns the current string value
                    textObj.text = $"{textStrings.GetStringText(nameRow)}, {userName}!";

                    // Changes the font if a language change is detected
                    if (language != textStrings._currentLanguageString)
                    {
                        textObj.font = textStrings.GetCurrentFont();
                    }
                }

                if (testDynamicWithVariables)
                {
                    // Constant call via ReplacePlaceholders
                    // Doesn`t take the font, just returns the current string value + variables
                    textObj.text = textStrings.ReplacePlaceholders(variableRow, dynamicValue, dynamicValue);

                    // Changes the font if a language change is detected
                    if (language != textStrings._currentLanguageString)
                    {
                        textObj.font = textStrings.GetCurrentFont();
                    }
                }

                // This method monitors whether there have been changes to cached text object with variables and, if so, initiates a rewrite
                if (testStaticWithVariables)
                {
                    // Updates variables on change only
                    if (language != textStrings._currentLanguageString)
                    {
                        UpdateStaticWithVariables();
                    }
                }

                // After processing dynamic variables, you can update the language if it has been changed
                if (language != textStrings._currentLanguageString)
                {
                    language = textStrings._currentLanguageString;
                }
            }
        }

        private void UpdateStaticWithVariables()
        {
            // For example, as variables we will use a language string and localized yes/no string
            var newLanguage = textStrings._currentLanguageString;
            if (newLanguage == "Chinese" || newLanguage == "Japanese")
            {
                //for bool needsSpecialFont = true;
                needsSpecialFont = textStrings.GetStringTextByKey("yes_key");
            }
            else
            {
                //for bool needsSpecialFont = false;
                needsSpecialFont = textStrings.GetStringTextByKey("no_key");
            }

            // Substitute your variables to fill the placeholders {} provided in the translations
            textStrings.FillTextObjectWithPlaceholders(noteRow, textObj, newLanguage, needsSpecialFont);
        }
    }
}