using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniSpore.Common
{
    public class SQLiteHelper
    {
        //从配置文本中读取连接字符串
        private static string connectionString = "";
        /// <summary>
        /// 创建连接到指定数据库
        /// </summary>
        /// <param name="datasource"></param>
        /// <param name="password"></param>
        /// <param name="version"></param>
        public static void SetConnectionString(string datasource, int version = 3)
        {
            connectionString = string.Format("Data Source={0};Version={1};",
                datasource, version);
        }

        /// <summary>
        /// 执行命令的方法：insert,update,delete
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters">可变参数，目的是省略了手动构造数组的过程，直接指定对象，编译器会帮助我们构造数组，并将对象加入数组中，传递过来</param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string sql, params SQLiteParameter[] parameters)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    try
                    {
                        connection.Open();
                        command.CommandText = sql;
                        if (parameters != null && parameters.Length != 0)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        return command.ExecuteNonQuery();
                    }
                    catch (Exception) { throw; }
                }
            }
        }

        /// <summary>
        /// 执行查询语句，并返回第一个结果。
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string sql, params SQLiteParameter[] parameters)
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    try
                    {
                        conn.Open();
                        cmd.CommandText = sql;
                        if (parameters != null && parameters.Length != 0)
                        {
                            cmd.Parameters.AddRange(parameters);
                        }
                        return cmd.ExecuteScalar();
                    }
                    catch (Exception) { throw; }
                }
            }
        }


        /// <summary>
        /// 执行一个查询语句，返回一个包含查询结果的DataTable。 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DataTable ExecuteQuery(string sql, params SQLiteParameter[] parameters)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    if (parameters != null && parameters.Length != 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable data = new DataTable();
                    try
                    {
                        adapter.Fill(data);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    return data;
                }
            }
        }


        /// <summary>
        /// 执行一个查询语句，返回一个关联的SQLiteDataReader实例。 
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static SQLiteDataReader ExecuteReader(string sql, params SQLiteParameter[] parameters)
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            try
            {
                if (parameters != null && parameters.Length != 0)
                {
                    command.Parameters.AddRange(parameters);
                }
                connection.Open();
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// 查询表字段类型
        /// </summary>
        /// <returns></returns>
        public static DataTable GetSchema()
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    return connection.GetSchema("TABLES");
                }
                catch (Exception) { throw; }
            }
        }



        #region 通用分页查询方法
        /// <summary>
        /// 通用分页查询方法
        /// </summary>
        /// <param name="connString">连接字符串</param>
        /// <param name="tableName">表名</param>
        /// <param name="strColumns">查询字段名</param>
        /// <param name="strWhere">where条件</param>
        /// <param name="strOrder">排序条件</param>
        /// <param name="pageSize">每页数据数量</param>
        /// <param name="currentIndex">当前页数</param>
        /// <param name="recordOut">数据总量</param>
        /// <returns>DataTable数据表</returns>
        public static DataTable SelectPaging(string tableName, string strColumns, string strWhere, string strOrder, int pageSize, int currentIndex, out int recordOut)
        {
            DataTable dt = new DataTable();
            recordOut = Convert.ToInt32(ExecuteScalar("select count(*) from " + tableName));

            string sql = string.Format("select {0} from {1}", strColumns, tableName);
            if (!string.IsNullOrEmpty(strWhere))
            {
                sql += string.Format(" where {0} ", strWhere);
            }

            if (!string.IsNullOrEmpty(strOrder))
            {

                sql += string.Format(" order by {0} ", strOrder);
            }
            int offsetCount = (currentIndex - 1) * pageSize;
            sql += string.Format(" limit {0} offset {1} ", pageSize, offsetCount);

            using (DbDataReader reader = ExecuteReader(sql))
            {
                if (reader != null)
                {
                    dt.Load(reader);
                }
            }
            return dt;
        }


        #endregion
    }
}
