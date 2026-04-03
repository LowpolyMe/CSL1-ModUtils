using System;
using System.IO;
using System.Xml.Serialization;

namespace ModUtils.XMLHelper
{
    public static class XmlStorage
    {
        public static void Save<T>(string path, T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, value);
            }
        }

        public static bool TryLoad<T>(string path, out T value)
        {
            if (!File.Exists(path))
            {
                value = default(T);
                return false;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                object result = serializer.Deserialize(stream);
                if (result is T)
                {
                    value = (T)result;
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        public static T LoadOrDefault<T>(string path) where T : new()
        {
            T value;
            return TryLoad(path, out value) ? value : new T();
        }

        public static T LoadOrDefault<T>(string path, T fallback)
        {
            T value;
            return TryLoad(path, out value) ? value : fallback;
        }
    }
}
