using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ModUtils.LocalizationHelper
{
    public sealed class Localizer
    {
        private readonly Dictionary<string, string> _activeTranslations;

        private Localizer(
            string requestedLanguageCode,
            string activeLanguageCode,
            Dictionary<string, Dictionary<string, string>> languages)
        {
            RequestedLanguageCode = requestedLanguageCode;
            ActiveLanguageCode = activeLanguageCode;
            _activeTranslations = languages[activeLanguageCode];
        }

        public string RequestedLanguageCode { get; private set; }
        public string ActiveLanguageCode { get; private set; }

        public static Localizer Load(
            Assembly assembly,
            string resourcePrefix,
            string requestedLanguageCode,
            string defaultLanguageCode)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (string.IsNullOrEmpty(resourcePrefix))
                throw new ArgumentException("Resource prefix is required.", "resourcePrefix");

            if (string.IsNullOrEmpty(requestedLanguageCode))
                throw new ArgumentException("Requested language code is required.", "requestedLanguageCode");

            if (string.IsNullOrEmpty(defaultLanguageCode))
                throw new ArgumentException("Default language code is required.", "defaultLanguageCode");

            Dictionary<string, Dictionary<string, string>> languages = LoadLanguages(assembly, resourcePrefix);
            EnsureDefaultLanguageExists(languages, defaultLanguageCode);
            ValidateLanguageKeySets(languages, defaultLanguageCode);

            string activeLanguageCode = languages.ContainsKey(requestedLanguageCode)
                ? requestedLanguageCode
                : defaultLanguageCode;

            return new Localizer(requestedLanguageCode, activeLanguageCode, languages);
        }

        public static Localizer Load(
            Assembly assembly,
            string resourcePrefix,
            string requestedLanguageCode)
        {
            return Load(assembly, resourcePrefix, requestedLanguageCode, "en");
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Translation key is required.", "key");

            string value;
            if (!_activeTranslations.TryGetValue(key, out value))
            {
                throw new KeyNotFoundException(
                    "Missing translation key '" + key + "' in active language '" + ActiveLanguageCode + "'.");
            }

            return value;
        }

        private static Dictionary<string, Dictionary<string, string>> LoadLanguages(
            Assembly assembly,
            string resourcePrefix)
        {
            string[] resourceNames = assembly.GetManifestResourceNames();
            string resourcePrefixWithSeparator = resourcePrefix + ".";
            Dictionary<string, Dictionary<string, string>> languages =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            int resourceCount = resourceNames.Length;
            for (int i = 0; i < resourceCount; i++)
            {
                string resourceName = resourceNames[i];
                if (!resourceName.StartsWith(resourcePrefixWithSeparator, StringComparison.Ordinal) ||
                    !resourceName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string languageCode = resourceName.Substring(
                    resourcePrefixWithSeparator.Length,
                    resourceName.Length - resourcePrefixWithSeparator.Length - 4);
                if (string.IsNullOrEmpty(languageCode) || languageCode.IndexOf('.') >= 0)
                {
                    throw new InvalidOperationException(
                        "Invalid translation resource name '" + resourceName + "'.");
                }

                if (languages.ContainsKey(languageCode))
                {
                    throw new InvalidOperationException(
                        "Duplicate translation resource for language '" + languageCode + "'.");
                }

                languages.Add(languageCode, LoadLanguage(assembly, resourceName, languageCode));
            }

            if (languages.Count == 0)
            {
                throw new InvalidOperationException(
                    "No translation resources were found for prefix '" + resourcePrefix + "'.");
            }

            return languages;
        }

        private static Dictionary<string, string> LoadLanguage(
            Assembly assembly,
            string resourceName,
            string languageCode)
        {
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new InvalidOperationException(
                    "Translation resource '" + resourceName + "' could not be opened.");
            }

            using (stream)
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                Dictionary<string, string> translations = new Dictionary<string, string>(StringComparer.Ordinal);
                int lineNumber = 0;
                string line = reader.ReadLine();
                while (line != null)
                {
                    lineNumber++;
                    if (line.Trim().Length > 0)
                    {
                        string[] fields = ParseCsvLine(resourceName, lineNumber, line);
                        string key = fields[0];
                        string value = fields[1];

                        if (key.Length == 0)
                        {
                            throw new InvalidOperationException(
                                BuildCsvError(resourceName, lineNumber, "Translation key cannot be empty."));
                        }

                        if (value.Length == 0)
                        {
                            throw new InvalidOperationException(
                                BuildCsvError(resourceName, lineNumber, "Translation value cannot be empty."));
                        }

                        if (translations.ContainsKey(key))
                        {
                            throw new InvalidOperationException(
                                BuildCsvError(resourceName, lineNumber, "Duplicate translation key '" + key + "'."));
                        }

                        translations.Add(key, value);
                    }

                    line = reader.ReadLine();
                }

                if (translations.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Translation resource '" + resourceName + "' for language '" + languageCode + "' contained no entries.");
                }

                return translations;
            }
        }

        private static string[] ParseCsvLine(string resourceName, int lineNumber, string line)
        {
            List<string> fields = new List<string>(2);
            StringBuilder builder = new StringBuilder();
            bool isQuoted = false;
            bool atFieldStart = true;

            int characterCount = line.Length;
            for (int i = 0; i < characterCount; i++)
            {
                char character = line[i];
                if (isQuoted)
                {
                    if (character == '"')
                    {
                        int nextIndex = i + 1;
                        if (nextIndex < characterCount && line[nextIndex] == '"')
                        {
                            builder.Append('"');
                            i = nextIndex;
                            continue;
                        }

                        isQuoted = false;
                        atFieldStart = false;
                        continue;
                    }

                    builder.Append(character);
                    continue;
                }

                if (atFieldStart && character == '"')
                {
                    isQuoted = true;
                    continue;
                }

                if (character == ',')
                {
                    fields.Add(builder.ToString());
                    builder.Length = 0;
                    atFieldStart = true;
                    continue;
                }

                builder.Append(character);
                atFieldStart = false;
            }

            if (isQuoted)
            {
                throw new InvalidOperationException(
                    BuildCsvError(resourceName, lineNumber, "Quoted field was not closed."));
            }

            fields.Add(builder.ToString());
            if (fields.Count != 2)
            {
                throw new InvalidOperationException(
                    BuildCsvError(resourceName, lineNumber, "Expected exactly two CSV fields."));
            }

            return fields.ToArray();
        }

        private static string BuildCsvError(string resourceName, int lineNumber, string error)
        {
            return "Invalid translation CSV in '" + resourceName + "' at line " + lineNumber + ": " + error;
        }

        private static void EnsureDefaultLanguageExists(
            Dictionary<string, Dictionary<string, string>> languages,
            string defaultLanguageCode)
        {
            if (!languages.ContainsKey(defaultLanguageCode))
            {
                throw new InvalidOperationException(
                    "Default language '" + defaultLanguageCode + "' is missing.");
            }
        }

        private static void ValidateLanguageKeySets(
            Dictionary<string, Dictionary<string, string>> languages,
            string defaultLanguageCode)
        {
            Dictionary<string, string> defaultTranslations = languages[defaultLanguageCode];

            foreach (KeyValuePair<string, Dictionary<string, string>> languageEntry in languages)
            {
                if (string.Equals(languageEntry.Key, defaultLanguageCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                Dictionary<string, string> candidateTranslations = languageEntry.Value;
                foreach (string defaultKey in defaultTranslations.Keys)
                {
                    if (!candidateTranslations.ContainsKey(defaultKey))
                    {
                        throw new InvalidOperationException(
                            "Language '" + languageEntry.Key + "' is missing translation key '" + defaultKey + "'.");
                    }
                }

                foreach (string candidateKey in candidateTranslations.Keys)
                {
                    if (!defaultTranslations.ContainsKey(candidateKey))
                    {
                        throw new InvalidOperationException(
                            "Language '" + languageEntry.Key + "' contains unknown translation key '" + candidateKey + "'.");
                    }
                }
            }
        }
    }
}
