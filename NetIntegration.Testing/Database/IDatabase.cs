using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace NetIntegration.Testing.Database
{
    public interface IDatabase
    {
        void GrantFileAccessForAttach();
        void Deploy(string dacPackageFile);
        void DeployLocalDB(string dacPackageFile);
        void DetachDatabase();
        int ExecuteNonQuery(Action<SqlCommand> prepare);
        TResult ExecuteReader<TResult>(Action<SqlCommand> prepare, Func<SqlDataReader, TResult> function);
        TResult ExecuteReader<TResult>(Action<SqlCommand> prepare, Func<SqlConnection, SqlDataReader, TResult> function);
        IEnumerable<TResult> GetResults<TResult>(Action<SqlCommand> prepare, Func<BasicReader, TResult> function);
        Dictionary<TKey, TValue> GetResults<TKey, TValue>(Action<SqlCommand> prepare, Func<BasicReader, TValue> function, Func<TValue, TKey> keySelector);
        TResult GetSingleResult<TResult>(Action<SqlCommand> prepare, Func<SqlDataReader, TResult> function);
    }
}
