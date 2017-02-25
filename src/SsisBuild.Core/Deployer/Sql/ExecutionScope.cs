//-----------------------------------------------------------------------
//   Copyright 2017 Roman Tumaykin
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//-----------------------------------------------------------------------

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