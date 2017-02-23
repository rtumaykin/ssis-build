using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace SsisBuild.Core.Deployer.Sql
{
    [ExcludeFromCodeCoverage]
    public class ExecutionScope : IDisposable
    {
        internal static List<int> RetryableErrors = new List<int>
        {
            53, 601, 615, 913, 921, 922, 923, 924, 926, 927, 941, 955, 956, 983, 976, 978, 979, 982, 983, 1204, 1205, 1214, 1222, 1428, 35201
        };
        public SqlTransaction Transaction { get; }
        private readonly SqlConnection _connection;
        public ExecutionScope()
        {
            _connection = new SqlConnection(ConnectionString);
            _connection.Open();
            Transaction = _connection.BeginTransaction();
        }
        public void Commit()
        {
            Transaction?.Commit();
        }
        public void Rollback()
        {
            Transaction?.Rollback();
        }
        public void Dispose()
        {
            Transaction?.Dispose();
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
                _connection.Dispose();
            }
        }

        public static string ConnectionString { get; set; }
    }
}