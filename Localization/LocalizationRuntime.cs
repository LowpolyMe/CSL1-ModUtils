using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ColossalFramework.Globalization;
using ModUtils.LocalizationHelper;

namespace ModUtils.Localization
{
    public sealed class LocalizationRuntime
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;
        private Localizer _localizer;
        private string[] _languageCodes;
        private string[] _languageDisplayNames;
        private int _loadedLanguageIndex = -1;

        public LocalizationRuntime(Assembly assembly, string resourcePrefix)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (string.IsNullOrEmpty(resourcePrefix))
                throw new ArgumentException("Resource prefix is required.", "resourcePrefix");

            _assembly = assembly;
            _resourcePrefix = resourcePrefix;
        }

        public void CheckInitialized(int languageIndex)
        {
            EnsureLocaleManager();
            EnsureLanguageData();
            if (_localizer != null && _loadedLanguageIndex == languageIndex)
                return;

            LoadLocalizer(languageIndex);
        }

        public string Get(string key)
        {
            CheckInitialized(_loadedLanguageIndex >= 0 ? _loadedLanguageIndex : 0);
            return _localizer.Get(key);
        }

        public string[] GetAllLanguageOptions()
        {
            EnsureLocaleManager();
            EnsureLanguageData();

            int entryCount = _languageDisplayNames.Length;
            string[] options = new string[entryCount + 1];
            options[0] = BuildAutoLabel();

            for (int i = 0; i < entryCount; i++)
            {
                options[i + 1] = _languageDisplayNames[i];
            }

            return options;
        }

        public void Reload(int languageIndex)
        {
            _localizer = null;
            _loadedLanguageIndex = -1;
            CheckInitialized(languageIndex);
        }

        private void LoadLocalizer(int languageIndex)
        {
            string requestedLanguage = ResolveRequestedLanguage(languageIndex);
            _localizer = Localizer.Load(_assembly, _resourcePrefix, requestedLanguage);
            _loadedLanguageIndex = languageIndex;
        }

        private string ResolveRequestedLanguage(int languageIndex)
        {
            EnsureLanguageData();

            if (languageIndex <= 0 || _languageCodes.Length == 0)
                return LocaleManager.instance.language;

            int targetIndex = languageIndex - 1;
            if (targetIndex < 0)
                targetIndex = 0;
            if (targetIndex >= _languageCodes.Length)
                targetIndex = _languageCodes.Length - 1;

            return _languageCodes[targetIndex];
        }

        private void EnsureLocaleManager()
        {
            if (LocaleManager.exists)
                return;

            throw new InvalidOperationException("LocaleManager must exist before localization is initialized.");
        }

        private void EnsureLanguageData()
        {
            if (_languageCodes != null)
                return;

            string prefixWithSeparator = _resourcePrefix + ".";
            const string suffix = ".csv";
            string[] resources = _assembly.GetManifestResourceNames();
            List<string> codes = new List<string>();

            for (int i = 0; i < resources.Length; i++)
            {
                string resource = resources[i];
                if (!resource.StartsWith(prefixWithSeparator, StringComparison.Ordinal))
                    continue;

                if (!resource.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    continue;

                string code = resource.Substring(
                    prefixWithSeparator.Length,
                    resource.Length - prefixWithSeparator.Length - suffix.Length);

                if (string.IsNullOrEmpty(code) || code.IndexOf('.') >= 0)
                    continue;

                codes.Add(code);
            }

            if (codes.Count == 0)
            {
                throw new InvalidOperationException(
                    "No translation resources were found for prefix '" + _resourcePrefix + "'.");
            }

            codes.Sort(StringComparer.OrdinalIgnoreCase);
            _languageCodes = codes.ToArray();
            _languageDisplayNames = new string[codes.Count];

            for (int i = 0; i < codes.Count; i++)
            {
                _languageDisplayNames[i] = FormatLanguageLabel(codes[i]);
            }
        }

        private static string FormatLanguageLabel(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
                return languageCode ?? string.Empty;

            try
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(languageCode);
                string nativeName = culture.NativeName;
                if (!string.IsNullOrEmpty(nativeName))
                    return nativeName + " [" + languageCode + "]";
            }
            catch (ArgumentException)
            {
                // Fallback to the raw code if the culture is missing.
            }

            return languageCode;
        }

        private static string BuildAutoLabel()
        {
            string code = LocaleManager.instance.language;
            if (string.IsNullOrEmpty(code))
                return "Auto (Game Language)";

            return "Auto (Game Language: " + code + ")";
        }
    }
}
