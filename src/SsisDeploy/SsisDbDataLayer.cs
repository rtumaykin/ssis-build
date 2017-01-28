namespace SsisDeploy.Data
{
	public class ExecutionScope : global::System.IDisposable
	{
		internal static global::System.Collections.Generic.List<int> RetryableErrors = new global::System.Collections.Generic.List<int>
		{
			53, 601, 615, 913, 921, 922, 923, 924, 926, 927, 941, 955, 956, 983, 976, 978, 979, 982, 983, 1204, 1205, 1214, 1222, 1428, 35201
		};
		public global::System.Data.SqlClient.SqlTransaction Transaction { get; private set; }
		private readonly global::System.Data.SqlClient.SqlConnection _connection;
		public ExecutionScope()
		{
			this._connection = new global::System.Data.SqlClient.SqlConnection(ConnectionString);
			this._connection.Open();
			this.Transaction = _connection.BeginTransaction();
		}
		public void Commit()
		{
			if (this.Transaction != null)
				this.Transaction.Commit();
		}
		public void Rollback()
		{
			if (this.Transaction != null)
				this.Transaction.Rollback();
		}
		public void Dispose()
		{
			if (this.Transaction != null)
			{
				this.Transaction.Dispose();
			}
			if (this._connection != null && this._connection.State != System.Data.ConnectionState.Closed)
			{
				this._connection.Close();
				this._connection.Dispose();
			}
		}
		private static global::System.String _connectionString;
		public static global::System.String ConnectionString
		{
			get
			{
				global::System.Threading.LazyInitializer.EnsureInitialized(
					ref _connectionString,
					() => global::System.Configuration.ConfigurationManager.ConnectionStrings["ssisdb"].ConnectionString
				);
				return _connectionString;
			}
			set
			{
				_connectionString = value;
			}
		}
	}
}namespace SsisDeploy.Data.Executables.catalog
{
public class create_folder
	{
public class ParametersCollection
		{
public global::System.String folder_name { get; private set; }
public global::System.Int64? folder_id { get; private set; }
public ParametersCollection(global::System.String folder_name, global::System.Int64? folder_id)
			{
				this.folder_name = folder_name;
				this.folder_id = folder_id;
			}

		}
public ParametersCollection Parameters { get; private set; }
public global::System.Int32 ReturnValue { get; private set; }
public static async global::System.Threading.Tasks.Task<create_folder> ExecuteAsync(global::System.String folder_name, global::System.Int64? folder_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new create_folder();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								await conn.OpenAsync();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[create_folder]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, folder_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							await cmd.ExecuteNonQueryAsync();
							retValue.Parameters = new ParametersCollection(folder_name, cmd.Parameters["@folder_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@folder_id"].Value);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/
public static create_folder Execute(global::System.String folder_name, global::System.Int64? folder_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new create_folder();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								conn.Open();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[create_folder]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, folder_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							cmd.ExecuteNonQuery();
							retValue.Parameters = new ParametersCollection(folder_name, cmd.Parameters["@folder_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@folder_id"].Value);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/

	}
public class deploy_project
	{
public class ParametersCollection
		{
public global::System.String folder_name { get; private set; }
public global::System.String project_name { get; private set; }
public global::System.Byte[] project_stream { get; private set; }
public global::System.Int64? operation_id { get; private set; }
public ParametersCollection(global::System.String folder_name, global::System.String project_name, global::System.Byte[] project_stream, global::System.Int64? operation_id)
			{
				this.folder_name = folder_name;
				this.project_name = project_name;
				this.project_stream = project_stream;
				this.operation_id = operation_id;
			}

		}
public ParametersCollection Parameters { get; private set; }
public global::System.Int32 ReturnValue { get; private set; }
public static async global::System.Threading.Tasks.Task<deploy_project> ExecuteAsync(global::System.String folder_name, global::System.String project_name, global::System.Byte[] project_stream, global::System.Int64? operation_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new deploy_project();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								await conn.OpenAsync();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[deploy_project]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_stream", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_stream){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@operation_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, operation_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							await cmd.ExecuteNonQueryAsync();
							retValue.Parameters = new ParametersCollection(folder_name, project_name, project_stream, cmd.Parameters["@operation_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@operation_id"].Value);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/
public static deploy_project Execute(global::System.String folder_name, global::System.String project_name, global::System.Byte[] project_stream, global::System.Int64? operation_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new deploy_project();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								conn.Open();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[deploy_project]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_stream", global::System.Data.SqlDbType.VarBinary, -1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_stream){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@operation_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, operation_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							cmd.ExecuteNonQuery();
							retValue.Parameters = new ParametersCollection(folder_name, project_name, project_stream, cmd.Parameters["@operation_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@operation_id"].Value);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/

	}
public class set_object_parameter_value
	{
public class ParametersCollection
		{
public global::System.Int16? object_type { get; private set; }
public global::System.String folder_name { get; private set; }
public global::System.String project_name { get; private set; }
public global::System.String parameter_name { get; private set; }
public global::System.Object parameter_value { get; private set; }
public global::System.String object_name { get; private set; }
public global::System.String value_type { get; private set; }
public ParametersCollection(global::System.Int16? object_type, global::System.String folder_name, global::System.String project_name, global::System.String parameter_name, global::System.Object parameter_value, global::System.String object_name, global::System.String value_type)
			{
				this.object_type = object_type;
				this.folder_name = folder_name;
				this.project_name = project_name;
				this.parameter_name = parameter_name;
				this.parameter_value = parameter_value;
				this.object_name = object_name;
				this.value_type = value_type;
			}

		}
public ParametersCollection Parameters { get; private set; }
public global::System.Int32 ReturnValue { get; private set; }
public static async global::System.Threading.Tasks.Task<set_object_parameter_value> ExecuteAsync(global::System.Int16? object_type, global::System.String folder_name, global::System.String project_name, global::System.String parameter_name, global::System.Object parameter_value, global::System.String object_name, global::System.String value_type, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new set_object_parameter_value();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								await conn.OpenAsync();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[set_object_parameter_value]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@object_type", global::System.Data.SqlDbType.SmallInt, 2, global::System.Data.ParameterDirection.Input, true, 5, 0, null, global::System.Data.DataRowVersion.Default, object_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@parameter_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, parameter_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@parameter_value", global::System.Data.SqlDbType.Variant, 8016, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, parameter_value){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@object_name", global::System.Data.SqlDbType.NVarChar, 260, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, object_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@value_type", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, value_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							await cmd.ExecuteNonQueryAsync();
							retValue.Parameters = new ParametersCollection(object_type, folder_name, project_name, parameter_name, parameter_value, object_name, value_type);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/
public static set_object_parameter_value Execute(global::System.Int16? object_type, global::System.String folder_name, global::System.String project_name, global::System.String parameter_name, global::System.Object parameter_value, global::System.String object_name, global::System.String value_type, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new set_object_parameter_value();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								conn.Open();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[set_object_parameter_value]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@object_type", global::System.Data.SqlDbType.SmallInt, 2, global::System.Data.ParameterDirection.Input, true, 5, 0, null, global::System.Data.DataRowVersion.Default, object_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@parameter_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, parameter_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@parameter_value", global::System.Data.SqlDbType.Variant, 8016, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, parameter_value){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@object_name", global::System.Data.SqlDbType.NVarChar, 260, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, object_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@value_type", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, value_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							cmd.ExecuteNonQuery();
							retValue.Parameters = new ParametersCollection(object_type, folder_name, project_name, parameter_name, parameter_value, object_name, value_type);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/

	}
public class validate_project
	{
public class ParametersCollection
		{
public global::System.String folder_name { get; private set; }
public global::System.String project_name { get; private set; }
public global::System.String validate_type { get; private set; }
public global::System.Int64? validation_id { get; private set; }
public global::System.Boolean? use32bitruntime { get; private set; }
public global::System.String environment_scope { get; private set; }
public global::System.Int64? reference_id { get; private set; }
public ParametersCollection(global::System.String folder_name, global::System.String project_name, global::System.String validate_type, global::System.Int64? validation_id, global::System.Boolean? use32bitruntime, global::System.String environment_scope, global::System.Int64? reference_id)
			{
				this.folder_name = folder_name;
				this.project_name = project_name;
				this.validate_type = validate_type;
				this.validation_id = validation_id;
				this.use32bitruntime = use32bitruntime;
				this.environment_scope = environment_scope;
				this.reference_id = reference_id;
			}

		}
public ParametersCollection Parameters { get; private set; }
public global::System.Int32 ReturnValue { get; private set; }
public static async global::System.Threading.Tasks.Task<validate_project> ExecuteAsync(global::System.String folder_name, global::System.String project_name, global::System.String validate_type, global::System.Int64? validation_id, global::System.Boolean? use32bitruntime, global::System.String environment_scope, global::System.Int64? reference_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new validate_project();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								await conn.OpenAsync();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[validate_project]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@validate_type", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, validate_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@validation_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, validation_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@use32bitruntime", global::System.Data.SqlDbType.Bit, 1, global::System.Data.ParameterDirection.Input, true, 1, 0, null, global::System.Data.DataRowVersion.Default, use32bitruntime){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@environment_scope", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, environment_scope){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@reference_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Input, true, 19, 0, null, global::System.Data.DataRowVersion.Default, reference_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							await cmd.ExecuteNonQueryAsync();
							retValue.Parameters = new ParametersCollection(folder_name, project_name, validate_type, cmd.Parameters["@validation_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@validation_id"].Value, use32bitruntime, environment_scope, reference_id);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/
public static validate_project Execute(global::System.String folder_name, global::System.String project_name, global::System.String validate_type, global::System.Int64? validation_id, global::System.Boolean? use32bitruntime, global::System.String environment_scope, global::System.Int64? reference_id, global::SsisDeploy.Data.ExecutionScope executionScope = null, global::System.Int32 commandTimeout = 30)
		{
			var retValue = new validate_project();
			{
				var retryCycle = 0;
				while (true)
				{
					global::System.Data.SqlClient.SqlConnection conn = executionScope == null ? new global::System.Data.SqlClient.SqlConnection(global::SsisDeploy.Data.ExecutionScope.ConnectionString) : executionScope.Transaction.Connection;
					try
					{
						if (conn.State != global::System.Data.ConnectionState.Open)
						{
							if (executionScope == null)
							{
								conn.Open();
							}
							else
							{
								retryCycle = int.MaxValue;
								throw new global::System.Exception("Execution Scope must have an open connection.");
							}
						}
						using (global::System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand())
						{
							cmd.CommandType = global::System.Data.CommandType.StoredProcedure;
							if (executionScope != null && executionScope.Transaction != null)
								cmd.Transaction = executionScope.Transaction;
							cmd.CommandTimeout = commandTimeout;
							cmd.CommandText = "[catalog].[validate_project]";
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@folder_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, folder_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@project_name", global::System.Data.SqlDbType.NVarChar, 128, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, project_name){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@validate_type", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, validate_type){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@validation_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Output, true, 19, 0, null, global::System.Data.DataRowVersion.Default, validation_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@use32bitruntime", global::System.Data.SqlDbType.Bit, 1, global::System.Data.ParameterDirection.Input, true, 1, 0, null, global::System.Data.DataRowVersion.Default, use32bitruntime){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@environment_scope", global::System.Data.SqlDbType.Char, 1, global::System.Data.ParameterDirection.Input, true, 0, 0, null, global::System.Data.DataRowVersion.Default, environment_scope){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@reference_id", global::System.Data.SqlDbType.BigInt, 8, global::System.Data.ParameterDirection.Input, true, 19, 0, null, global::System.Data.DataRowVersion.Default, reference_id){ });
							cmd.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@ReturnValue", global::System.Data.SqlDbType.Int, 4, global::System.Data.ParameterDirection.ReturnValue, true, 0, 0, null, global::System.Data.DataRowVersion.Default, global::System.DBNull.Value));
							cmd.ExecuteNonQuery();
							retValue.Parameters = new ParametersCollection(folder_name, project_name, validate_type, cmd.Parameters["@validation_id"].Value == global::System.DBNull.Value ? null : (global::System.Int64?)cmd.Parameters["@validation_id"].Value, use32bitruntime, environment_scope, reference_id);
							retValue.ReturnValue = (global::System.Int32)cmd.Parameters["@ReturnValue"].Value;
							return retValue;
						}
					}
					catch (global::System.Data.SqlClient.SqlException e)
					{
						if (retryCycle++ > 9 || !ExecutionScope.RetryableErrors.Contains(e.Number))
							throw;
						global::System.Threading.Thread.Sleep(1000);
					}
					finally
					{
						if (executionScope == null && conn != null)
						{
							((global::System.IDisposable)conn).Dispose();
						}
					}
				}}
		}
/*end*/

	}
}