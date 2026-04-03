using System;
using System.ComponentModel;
using System.Xml.Linq;

namespace ModUtils.XMLHelper
{
    public static class XElementExtensions
    {
        public static T GetAttrValue<T>(this XElement element, string attrName, T defaultValue, Predicate<T> validator)
        {
            return Convert(element.Attribute(attrName) != null ? element.Attribute(attrName).Value : null, defaultValue, validator);
        }

        public static T GetAttrValue<T>(this XElement element, string attrName, T defaultValue)
        {
            return GetAttrValue(element, attrName, defaultValue, null);
        }

        public static T GetAttrValue<T>(this XElement element, string attrName)
        {
            return GetAttrValue(element, attrName, default(T), null);
        }

        public static object GetAttrValue(this XElement element, string attrName, Type type)
        {
            return Convert(element.Attribute(attrName) != null ? element.Attribute(attrName).Value : null, type);
        }

        public static T GetValue<T>(this XElement element, T defaultValue, Predicate<T> validator)
        {
            return Convert(element.Value, defaultValue, validator);
        }

        public static T GetValue<T>(this XElement element, T defaultValue)
        {
            return GetValue(element, defaultValue, null);
        }

        public static T GetValue<T>(this XElement element)
        {
            return GetValue(element, default(T), null);
        }

        public static bool TryGetAttrValue<T>(this XElement element, string attrName, out T value)
        {
            return TryGetAttrValue(element, attrName, null, out value);
        }

        public static bool TryGetAttrValue<T>(this XElement element, string attrName, Predicate<T> validator, out T value)
        {
            XAttribute attribute = element.Attribute(attrName);
            if (attribute != null)
            {
                object converted = Convert(attribute.Value, typeof(T));
                if (converted is T && (validator == null || validator((T)converted)))
                {
                    value = (T)converted;
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public static bool TryGetValue<T>(this XElement element, out T value)
        {
            return TryGetValue(element, null, out value);
        }

        public static bool TryGetValue<T>(this XElement element, Predicate<T> validator, out T value)
        {
            object converted = Convert(element.Value, typeof(T));
            if (converted is T && (validator == null || validator((T)converted)))
            {
                value = (T)converted;
                return true;
            }

            value = default(T);
            return false;
        }

        public static void AddAttr(this XElement element, string name, object value)
        {
            element.Add(new XAttribute(name, value));
        }

        private static T Convert<T>(string text, T defaultValue, Predicate<T> validator)
        {
            object value = Convert(text, typeof(T));
            if (value is T && (validator == null || validator((T)value)))
                return (T)value;

            return defaultValue;
        }

        private static object Convert(string text, Type type)
        {
            try
            {
                if (type == typeof(string))
                    return text;

                if (string.IsNullOrEmpty(text))
                    return null;

                return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(text);
            }
            catch
            {
                return null;
            }
        }
    }
}
