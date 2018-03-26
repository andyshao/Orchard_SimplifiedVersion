using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTG.Common.Services
{
    public static partial class SqlServer2000Helper
    {
        private static readonly string coonstr = ConfigurationManager.ConnectionStrings["DBconnection"].ConnectionString;
        /// <summary>
        /// 对连接执行 Transact-SQL 语句并返回受影响的行数。
        /// </summary>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] parmeters)
        {
            using (SqlConnection conn = new SqlConnection(coonstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parmeters);
                    int i = cmd.ExecuteNonQuery();
                    return i;
                }
            }

        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集中第一行的第一列。忽略其他列或行。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object ExecuteSacalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(coonstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parameters);
                    object i = cmd.ExecuteScalar();
                    return i;
                }
            }
        }
        /// <summary>
        /// 执行查询，并返回查询所返回的结果集
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(coonstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parameters);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }
        public static SqlDataReader ExecuteDataReader(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(coonstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parameters);
                    //return cmd.ExecuteReader(CommandBehavior.CloseConnection);

                    SqlDataReader i = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    return i;
                }
            }
        }
        public static SqlParameter[] Daifanhuizhidecunchuguocheng(string sql, params SqlParameter[] parameters)
        {
            string[] str = new string[2] { "", "" };
            using (SqlConnection conn = new SqlConnection(coonstr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddRange(parameters);
                    //cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();
                    //str[0] = parameters[22].Value.ToString();
                    //str[1] = parameters[23].Value.ToString();
                }
            }
            return parameters;
        }
    }
}
