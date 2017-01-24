using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssis_deploy
{
    class Program
    {
        static void Main(string[] args)
        {
            //    public Operation DeployProject(string projectName, byte[] projectStream)
            //{
            //    if (projectName == null)
            //        throw new ArgumentNullException("projectName");
            //    if (projectStream == null)
            //        throw new ArgumentNullException("projectStream");
            //    string cmdText = string.Format((IFormatProvider)CultureInfo.InvariantCulture, "[{0}].[catalog].[deploy_project]", new object[1]
            //    {
            //(object) Helpers.GetEscapedName(this.Parent.Name)
            //    });
            //    SqlParameter[] parameters = new SqlParameter[4]
            //    {
            //new SqlParameter("folder_name", (object) this.Name),
            //new SqlParameter("project_name", (object) projectName),
            //new SqlParameter("project_stream", (object) projectStream),
            //new SqlParameter("operation_id", SqlDbType.BigInt)
            //    };
            //    parameters[3].Direction = ParameterDirection.InputOutput;
            //    CatalogFolder.tc.Assert(this.GetDomain() != null);
            //    SqlHelper.ExecuteSQLCommand(((Microsoft.SqlServer.Management.IntegrationServices.IntegrationServices)this.GetDomain()).Connection, CommandType.StoredProcedure, cmdText, parameters, ExecuteType.ExecuteNonQuery, 300);
            //    long id = (long)parameters[3].Value;
            //    CatalogFolder.tc.Assert(id > 0L);
            //    Operation operation = new Operation(this.Parent, id);
            //    operation.Refresh();
            //    while (!operation.Completed)
            //    {
            //        Thread.Sleep(1000);
            //        operation.Refresh();
            //    }
            //    return operation;
            //}
        }
    }
}
