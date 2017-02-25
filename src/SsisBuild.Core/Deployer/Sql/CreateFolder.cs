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
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SsisBuild.Core.Deployer.Sql
{
    [ExcludeFromCodeCoverage]
    public class CreateFolder
    {
        public class ParametersCollection
        {
            public string FolderName { get; private set; }
            public long? FolderId { get; private set; }
            public ParametersCollection(string folderName, long? folderId)
            {
                FolderName = folderName;
                FolderId = folderId;
            }

        }
        public ParametersCollection Parameters { get; private set; }
        public int ReturnValue { get; private set; }
        public static async Task<CreateFolder> ExecuteAsync(string folderName, long? folderId, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new CreateFolder();
            {
                var retryCycle = 0;
                while (true)
                {
                    var conn = executionScope?.Transaction?.Connection ?? new SqlConnection(ExecutionScope.ConnectionString);
                    try
                    {
                        if (conn.State != ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                await conn.OpenAsync();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (executionScope?.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[catalog].[create_folder]";
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@folder_id", SqlDbType.BigInt, 8, ParameterDirection.Output, true, 19, 0, null, DataRowVersion.Default, folderId));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                            retValue.Parameters = new ParametersCollection(folderName, cmd.Parameters["@folder_id"].Value == DBNull.Value ? null : (long?)cmd.Parameters["@folder_id"].Value);
                            retValue.ReturnValue = (int)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null)
                        {
                            conn?.Dispose();
                        }
                    }
                }
            }
        }

        public static CreateFolder Execute(string folderName, long? folderId, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new CreateFolder();
            {
                var retryCycle = 0;
                while (true)
                {
                    var conn = executionScope?.Transaction?.Connection ?? new SqlConnection(ExecutionScope.ConnectionString);
                    try
                    {
                        if (conn.State != ConnectionState.Open)
                        {
                            if (executionScope == null)
                            {
                                conn.Open();
                            }
                            else
                            {
                                retryCycle = int.MaxValue;
                                throw new Exception("Execution Scope must have an open connection.");
                            }
                        }
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            if (executionScope?.Transaction != null)
                                cmd.Transaction = executionScope.Transaction;
                            cmd.CommandTimeout = commandTimeout;
                            cmd.CommandText = "[catalog].[create_folder]";
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@folder_id", SqlDbType.BigInt, 8, ParameterDirection.Output, true, 19, 0, null, DataRowVersion.Default, folderId));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            cmd.ExecuteNonQuery();
                            retValue.Parameters = new ParametersCollection(folderName, cmd.Parameters["@folder_id"].Value == DBNull.Value ? null : (long?)cmd.Parameters["@folder_id"].Value);
                            retValue.ReturnValue = (int)cmd.Parameters["@ReturnValue"].Value;
                            return retValue;
                        }
                    }
                    catch (SqlException e)
                    {
                        if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
                            throw;
                        System.Threading.Thread.Sleep(1000);
                    }
                    finally
                    {
                        if (executionScope == null)
                        {
                            conn?.Dispose();
                        }
                    }
                }
            }
        }
    }
}