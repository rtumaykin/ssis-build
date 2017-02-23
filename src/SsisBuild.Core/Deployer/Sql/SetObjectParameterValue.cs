using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SsisBuild.Core.Deployer.Sql
{
    [ExcludeFromCodeCoverage]
    public class SetObjectParameterValue
    {
        public class ParametersCollection
        {
            public short? ObjectType { get; private set; }
            public string FolderName { get; private set; }
            public string ProjectName { get; private set; }
            public string ParameterName { get; private set; }
            public object ParameterValue { get; private set; }
            public string ObjectName { get; private set; }
            public string ValueType { get; private set; }
            public ParametersCollection(short? objectType, string folderName, string projectName, string parameterName, object parameterValue, string objectName, string valueType)
            {
                ObjectType = objectType;
                FolderName = folderName;
                ProjectName = projectName;
                ParameterName = parameterName;
                ParameterValue = parameterValue;
                ObjectName = objectName;
                ValueType = valueType;
            }

        }
        public ParametersCollection Parameters { get; private set; }
        public int ReturnValue { get; private set; }
        public static async Task<SetObjectParameterValue> ExecuteAsync(short? objectType, string folderName, string projectName, string parameterName, object parameterValue, string objectName, string valueType, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new SetObjectParameterValue();
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
                            cmd.CommandText = "[catalog].[set_object_parameter_value]";
                            cmd.Parameters.Add(new SqlParameter("@object_type", SqlDbType.SmallInt, 2, ParameterDirection.Input, true, 5, 0, null, DataRowVersion.Default, objectType));
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@project_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, projectName));
                            cmd.Parameters.Add(new SqlParameter("@parameter_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, parameterName));
                            cmd.Parameters.Add(new SqlParameter("@parameter_value", SqlDbType.Variant, 8016, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, parameterValue));
                            cmd.Parameters.Add(new SqlParameter("@object_name", SqlDbType.NVarChar, 260, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, objectName));
                            cmd.Parameters.Add(new SqlParameter("@value_type", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, valueType));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            await cmd.ExecuteNonQueryAsync();
                            retValue.Parameters = new ParametersCollection(objectType, folderName, projectName, parameterName, parameterValue, objectName, valueType);
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

        public static SetObjectParameterValue Execute(short? objectType, string folderName, string projectName, string parameterName, object parameterValue, string objectName, string valueType, ExecutionScope executionScope = null, int commandTimeout = 30)
        {
            var retValue = new SetObjectParameterValue();
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
                            cmd.CommandText = "[catalog].[set_object_parameter_value]";
                            cmd.Parameters.Add(new SqlParameter("@object_type", SqlDbType.SmallInt, 2, ParameterDirection.Input, true, 5, 0, null, DataRowVersion.Default, objectType));
                            cmd.Parameters.Add(new SqlParameter("@folder_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, folderName));
                            cmd.Parameters.Add(new SqlParameter("@project_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, projectName));
                            cmd.Parameters.Add(new SqlParameter("@parameter_name", SqlDbType.NVarChar, 128, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, parameterName));
                            cmd.Parameters.Add(new SqlParameter("@parameter_value", SqlDbType.Variant, 8016, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, parameterValue));
                            cmd.Parameters.Add(new SqlParameter("@object_name", SqlDbType.NVarChar, 260, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, objectName));
                            cmd.Parameters.Add(new SqlParameter("@value_type", SqlDbType.Char, 1, ParameterDirection.Input, true, 0, 0, null, DataRowVersion.Default, valueType));
                            cmd.Parameters.Add(new SqlParameter("@ReturnValue", SqlDbType.Int, 4, ParameterDirection.ReturnValue, true, 0, 0, null, DataRowVersion.Default, DBNull.Value));
                            cmd.ExecuteNonQuery();
                            retValue.Parameters = new ParametersCollection(objectType, folderName, projectName, parameterName, parameterValue, objectName, valueType);
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