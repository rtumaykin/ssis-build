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
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SsisBuild.Core.Deployer.Sql
{
    [ExcludeFromCodeCoverage]
    public class ValidateProject
    {
        public class ParametersCollection
        {
            public string FolderName { get; private set; }
            public string ProjectName { get; private set; }
            public string ValidateType { get; private set; }
            public long? ValidationId { get; private set; }
            public bool? Use32Bitruntime { get; private set; }
            public string EnvironmentScope { get; private set; }
            public long? ReferenceId { get; private set; }
            public ParametersCollection(string folderName, string projectName, string validateType, long? validationId, bool? use32Bitruntime, string environmentScope, long? referenceId)
            {
                FolderName = folderName;
                ProjectName = projectName;
                ValidateType = validateType;
                ValidationId = validationId;
                Use32Bitruntime = use32Bitruntime;
                EnvironmentScope = environmentScope;
                ReferenceId = referenceId;
            }

        }
        public ParametersCollection Parameters { get; private set; }
        public int ReturnValue { get; private set; }
        public static async Task<ValidateProject> ExecuteAsync(string folderName, string projectName, string validateType, long? validationId, bool? use32Bitruntime, string environmentScope, long? referenceId, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new ValidateProject();
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
                            cmd.CommandText = "[catalog].[validate_project]";
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@project_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, projectName));
                            cmd.Parameters.Add(new SqlParameter("@validate_type", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, validateType));
                            cmd.Parameters.Add(new SqlParameter("@validation_id", SqlDbType.BigInt, 8, ParameterDirection.Output, true, 19, 0, null, DataRowVersion.Default, validationId));
                            cmd.Parameters.Add(new SqlParameter("@use32bitruntime", SqlDbType.Bit, 1, ParameterDirection.Input, true, 1, 0, null, DataRowVersion.Default, use32Bitruntime));
                            cmd.Parameters.Add(new SqlParameter("@environment_scope", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, environmentScope));
                            cmd.Parameters.Add(new SqlParameter("@reference_id", SqlDbType.BigInt, 8, ParameterDirection.Input, true, 19, 0, null, DataRowVersion.Default, referenceId));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                            retValue.Parameters = new ParametersCollection(folderName, projectName, validateType, cmd.Parameters["@validation_id"].Value == DBNull.Value ? null : (long?)cmd.Parameters["@validation_id"].Value, use32Bitruntime, environmentScope, referenceId);
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
        /*end*/
        public static ValidateProject Execute(string folderName, string projectName, string validateType, long? validationId, bool? use32Bitruntime, string environmentScope, long? referenceId, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new ValidateProject();
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
                            cmd.CommandText = "[catalog].[validate_project]";
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@project_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, projectName));
                            cmd.Parameters.Add(new SqlParameter("@validate_type", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, validateType));
                            cmd.Parameters.Add(new SqlParameter("@validation_id", SqlDbType.BigInt, 8, ParameterDirection.Output, true, 19, 0, null, DataRowVersion.Default, validationId));
                            cmd.Parameters.Add(new SqlParameter("@use32bitruntime", SqlDbType.Bit, 1, ParameterDirection.Input, true, 1, 0, null, DataRowVersion.Default, use32Bitruntime));
                            cmd.Parameters.Add(new SqlParameter("@environment_scope", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, environmentScope));
                            cmd.Parameters.Add(new SqlParameter("@reference_id", SqlDbType.BigInt, 8, ParameterDirection.Input, true, 19, 0, null, DataRowVersion.Default, referenceId));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            cmd.ExecuteNonQuery();
                            retValue.Parameters = new ParametersCollection(folderName, projectName, validateType, cmd.Parameters["@validation_id"].Value == DBNull.Value ? null : (long?)cmd.Parameters["@validation_id"].Value, use32Bitruntime, environmentScope, referenceId);
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