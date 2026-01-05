using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LocalizationSystemMini
{
    // A component to add to a GameObject with TextMesh Pro
    // This object is used so that texts are not stored directly on the scene, but collected into a single CSV table and their translation is automated
    // Use this component for additional text elements on the scene (for example, the label "Settings," an extra text button like "Skip Tutorial," and so on)
    public class LocalizedTooltip : MonoBehaviour
    {
        #region Variables
        [SerializeField] private InputStringsScriptableObject inputTextSettings; // Reference to the translation storage
        [SerializeField] private TMP_Text objectText; // Text component to fill with text
        [SerializeField] private string connectedTableKey = ""; // The row of the table containing the desired text found by key

        [SerializeField] private List<ParameterSelector> dynamicParameters;
        private object[] cachedParameterValues;
        private bool hasCachedValues = false;
        #endregion

        #region Initialization

        void Start()
        {
            // Triggers only once and waits for translation storage inputTextSettings to initialize(the current language is set and the strings from the CSV are parsed)
            // On initialization the text field is assigned a value from the table in the current language, and the text field itself is cached in the storage
            // Then the current text is automatically managed by the storage: translated when the language changes, until objectText is removed from the scene
            FillTextObject();
        }

        // // Uncomment for auto update parameters
        //void Update()
        //{
        //    if (CheckAndUpdateParametersCache())
        //    {
        //        FillTextObject();
        //    }
        //}

        // If possible, use it on event call when this object is created on the scene or after all components are initialized
        // Call if this text tooltip uses parameters and the parameter value has been changed externally
        public void FillTextObject()
        {
            if (inputTextSettings != null && objectText != null)
            {
                if (!string.IsNullOrEmpty(connectedTableKey))
                {
                    if (dynamicParameters != null && dynamicParameters.Count > 0)
                    {
                        inputTextSettings.FillTextObjectWithPlaceholdersByKey(connectedTableKey, objectText, TryGetAllParameters());
                    }
                    else
                    {
                        inputTextSettings.FillTextObjectByKey(connectedTableKey, objectText);
                    }
                }
            }
        }
        #endregion

        #region Get Parameter Data
        private object[] TryGetAllParameters()
        {
            // Get all dynamic parameters as array
            var allValues = dynamicParameters
                .Select(p => p.GetValue())
                .ToArray();
            return allValues;
        }

        private object TryGetParameterByIndex(int index)
        {
            if (index < 0 || index >= dynamicParameters.Count)
                return null;

            return dynamicParameters[index].GetValue();
        }

        private object TryGetParameterByName(string fieldName)
        {
            foreach (var param in dynamicParameters)
            {
                if (param.fieldName == fieldName)
                    return param.GetValue();
            }
            return null;
        }
        #endregion

        #region Parameter Cache Management

        public bool CheckAndUpdateParametersCache()
        {
            if (dynamicParameters == null || dynamicParameters.Count == 0)
                return false;

            var currentParameters = TryGetAllParameters();

            if (!hasCachedValues)
            {
                CacheAllParameters();
                return true;
            }

            bool hasChanged = inputTextSettings.HaveParametersChanged(currentParameters, cachedParameterValues);

            if (hasChanged)
            {
                cachedParameterValues = currentParameters;
            }

            return hasChanged;
        }

        public void CacheAllParameters()
        {
            if (dynamicParameters != null && dynamicParameters.Count > 0)
            {
                cachedParameterValues = TryGetAllParameters();
                hasCachedValues = true;
            }
        }

        #endregion
    }
}