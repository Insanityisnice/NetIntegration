using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace NetIntegration.Testing.Database
{
    public sealed class BasicReader
    {
        private readonly IDataReader _reader;
        private Dictionary<string, int> _ordinals;

        internal BasicReader(IDataReader reader)
        {
            this._reader = reader;
        }

        public static implicit operator SqlDataReader(BasicReader reader)
        {
            return reader != null ? reader._reader as SqlDataReader : null;
        }

        public T Get<T>(int ordinal, T defaultValue = default(T))
        {
            var t = typeof(T);

            if (this._reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }

            if (t == typeof(string))
            {
                return (T)(object)this._reader.GetString(ordinal);
            }

            if (t == typeof(int) && !t.IsEnum)
            {
                return (T)(object)this._reader.GetInt32(ordinal);
            }

            return this._reader.Get<T>(ordinal);
        }

        public T Get<T>(string name)
        {
            return this.Get<T>(this.GetOrdinal(name));
        }

        public int GetInt32(int ordinal)
        {
            return this._reader.GetInt32(ordinal);
        }

        public long GetInt64(int ordinal)
        {
            return this._reader.GetInt64(ordinal);
        }

        public string GetString(int ordinal)
        {
            return this._reader.GetString(ordinal);
        }

        public bool TryGet<T>(string name, out T value)
        {
            return this._reader.TryGet(this.GetOrdinal(name), out value);
        }

        public bool TryGet<T>(int ordinal, out T value)
        {
            return this._reader.TryGet(ordinal, out value);
        }

        private int GetOrdinal(string name)
        {
            if (this._ordinals == null)
            {
                var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int n = 0; n < this._reader.FieldCount; n++)
                {
                    col.Add(this._reader.GetName(n), n);
                }

                this._ordinals = col;
            }

            return this._ordinals[name];
        }
    }
}