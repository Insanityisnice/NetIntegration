using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIntegration.Testing.Database
{
    internal static class IDataReaderExtensions
    {
        public static List<TResult> ToList<TResult>(this IDataReader reader, Func<BasicReader, TResult> function)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var list = new List<TResult>();
            var basicReader = new BasicReader(reader);

            while (reader.Read())
            {
                list.Add(function(basicReader));
            }

            return list;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDataReader reader, Func<BasicReader, TValue> function, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }

            var dictionary = new Dictionary<TKey, TValue>(equalityComparer ?? EqualityComparer<TKey>.Default);
            var basicReader = new BasicReader(reader);

            while (reader.Read())
            {
                TValue value = function(basicReader);
                dictionary.Add(keySelector(value), value);
            }

            return dictionary;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IDataReader reader, Func<IDataReader, TValue> function, Func<IDataReader, TKey> keySelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            if (function == null)
            {
                throw new ArgumentNullException("function");
            }

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }

            var dictionary = new Dictionary<TKey, TValue>(equalityComparer ?? EqualityComparer<TKey>.Default);

            while (reader.Read())
            {
                TValue value = function(reader);
                dictionary.Add(keySelector(reader), value);
            }

            return dictionary;
        }

        public static T Get<T>(this IDataReader reader, string name)
        {
            T value;

            TryGet<T>(reader, name, out value);

            return value;
        }

        public static T Get<T>(this IDataReader reader, int ordinal)
        {
            T value;

            TryGet<T>(reader, ordinal, out value);

            return value;
        }

        public static T Get<T>(this IDataReader reader, string name, T defaultValue)
        {
            T value;

            if (!TryGet<T>(reader, name, out value))
            {
                value = defaultValue;
            }

            return value;
        }

        public static T Get<T>(this IDataReader reader, int ordinal, T defaultValue)
        {
            T value;

            if (!TryGet<T>(reader, ordinal, out value))
            {
                value = defaultValue;
            }

            return value;
        }

        public static bool TryGet<T>(this IDataReader reader, string name, out T value)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            return TryGet<T>(reader, reader.GetOrdinal(name), out value);
        }

        public static bool TryGet<T>(this IDataReader reader, int ordinal, out T value)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (reader.IsDBNull(ordinal))
            {
                value = default(T);
                return false;
            }

            if (typeof(T) == typeof(string))
            {
                value = (T)(object)reader.GetString(ordinal);
                return true;
            }

            Type t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = t.GetGenericArguments()[0];
            }

            if (!t.IsValueType)
            {
                throw new InvalidCastException("Cannot cast SQL value to " + typeof(T).Name);
            }

            object v = reader.GetValue(ordinal);

            if (t.IsAssignableFrom(v.GetType()))
            {
                value = (T)(object)v;
            }
            else if (t.IsEnum)
            {
                if (v is string)
                {
                    value = (T)Enum.Parse(typeof(T), v.ToString());
                }
                else if (typeof(T) == t)
                {
                    if (v is int)
                        value = (T)(object)v;
                    else
                        value = (T)Convert.ChangeType(v, typeof(int));
                }
                else
                {
                    value = (T)Enum.Parse(t, v.ToString());
                }
            }
            else
            {
                value = (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
            }

            return true;
        }
    }
}
