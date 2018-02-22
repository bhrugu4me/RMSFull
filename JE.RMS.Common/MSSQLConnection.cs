using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using Dapper;
namespace JE.RMS.Common
{
    public class MSSQLConnection
    {
        public static List<T> ExecuteStoredProcedure<T>(string sql, List<SqlParameter> parameters = null)
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();

                    DynamicParameters p = new DynamicParameters();
                    if (parameters != null)
                    {
                        foreach (var parm in parameters)
                        {
                            p.Add(parm.ParameterName, parm.Value);
                        }
                    }
                    return connection.Query<T>(sql, p, null, true,60, CommandType.StoredProcedure).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Tuple<T1,List<T2>> QueryMultiple<T1,T2>(string sql, List<SqlParameter> parameters = null)
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();

                    DynamicParameters p = new DynamicParameters();
                    if (parameters != null)
                    {
                        foreach (var parm in parameters)
                        {
                            p.Add(parm.ParameterName, parm.Value);
                        }
                    }
                    using (var ret =connection.QueryMultiple(sql, p,null,null, CommandType.StoredProcedure))
                    {
                        var obj = ret.Read<T1>().First();
                        var objlist = ret.Read<T2>().ToList();
                       
                        return Tuple.Create(obj,objlist);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<T> ExecuteStoredProcedureWithPOCO<T>(string sql, object parameters = null)
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    connection.Open();
                    return connection.Query<T>(sql, parameters, null, true, null, CommandType.StoredProcedure).ToList();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static IDbConnection CreateConnection()
        {
            string connectionString = ConfigurationManager.ConnectionStrings["mycnnstr"].ToString();
            var connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}