using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;

namespace System
{
    public class MySqlExpress
    {
        public MySqlCommand cmd;

        public MySqlExpress()
        {
            
        }

        public MySqlExpress(MySqlCommand cmd)
        {
            this.cmd = cmd;
        }

        public void StartTransaction()
        {
            cmd.CommandText = "start transaction;";
            cmd.ExecuteNonQuery();
        }

        public void Commit()
        {
            cmd.CommandText = "commit;";
            cmd.ExecuteNonQuery();
        }

        public void Rollback()
        {
            cmd.CommandText = "rollback";
            cmd.ExecuteNonQuery();
        }

        public DataTable Select(string sql)
        {
            return Select(sql, new List<MySqlParameter>());
        }

        public DataTable Select(string sql, IDictionary<string, object> dicParameters)
        {
            List<MySqlParameter> lst = GetParametersList(dicParameters);
            return Select(sql, lst);
        }

        public DataTable Select(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.CommandText = sql;
            cmd.Parameters.Clear();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();

            da.Fill(dt);
            return dt;
        }

        public DataTable SelectWhere(string sql, Dictionary<string, object> dicCond)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(sql);
            sb.Append(" where 1 = 1 ");

            //bool first = true;

            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                sb.Append("and `");
                sb.Append(kv.Key);
                sb.Append("` = @");
                sb.Append(kv.Key);
            }

            sb.Append(";");

            cmd.CommandText = sb.ToString();

            cmd.Parameters.Clear();

            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                cmd.Parameters.AddWithValue("@" + kv.Key, kv.Value);
            }

            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);
            return dt;
        }

        public void Execute(string sql)
        {
            Execute(sql, new List<MySqlParameter>());
        }

        public void Execute(string sql, IDictionary<string, object> dicParameters)
        {
            List<MySqlParameter> lst = GetParametersList(dicParameters);
            Execute(sql, lst);
        }

        public void Execute(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }
            cmd.ExecuteNonQuery();
        }

        public object ExecuteScalar(string sql)
        {
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> dicParameters)
        {
            List<MySqlParameter> lst = GetParametersList(dicParameters);
            return ExecuteScalar(sql, lst);
        }

        public object ExecuteScalar(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    cmd.Parameters.Add(parameter);
                }
            }
            return cmd.ExecuteScalar();
        }

        public T ExecuteScalar<T>(string sql, Dictionary<string, object> dicParameters)
        {
            List<MySqlParameter> lst = null;
            if (dicParameters != null)
            {
                lst = new List<MySqlParameter>();
                foreach (KeyValuePair<string, object> kv in dicParameters)
                {
                    lst.Add(new MySqlParameter(kv.Key, kv.Value));
                }
            }
            return ExecuteScalar<T>(sql, lst);
        }

        public T ExecuteScalar<T>(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.Parameters.Clear();
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    cmd.Parameters.Add(parameter);
                }
            }
            return (T)Convert.ChangeType(cmd.ExecuteScalar(), typeof(T));
        }

        public T ExecuteScalar<T>(string sql)
        {
            cmd.CommandText = sql;
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            object ob = null;
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                ob = dt.Rows[0][0];
            }

            if (ob == null || ob is DBNull)
            {
                if (typeof(T) == typeof(string))
                    ob = "";
                else if (typeof(T) == typeof(int) ||
                        typeof(T) == typeof(long) ||
                        typeof(T) == typeof(short) ||
                        typeof(T) == typeof(uint) ||
                        typeof(T) == typeof(ulong) ||
                        typeof(T) == typeof(ushort))
                    ob = 0;
                else if (typeof(T) == typeof(decimal))
                    ob = 0m;
                else if (typeof(T) == typeof(double))
                    ob = 0d;
                else if (typeof(T) == typeof(DateTime))
                    ob = DateTime.MinValue;
                else if (typeof(T) == typeof(bool))
                    ob = false;
                else if (typeof(T) == typeof(byte[]))
                    ob = new byte[0];
                else if (typeof(T) == typeof(TimeSpan))
                    ob = new TimeSpan(0);
                else if (typeof(T) == typeof(Guid))
                    ob = new Guid();
                else
                    throw new Exception("Error MySqlHelper.ExecuteScalar<T> - T = " + typeof(T).ToString());
            }
            //else
            //{
            //    if (typeof(T) == typeof(string))
            //        ob = "";
            //    else if (typeof(T) == typeof(int) ||
            //            typeof(T) == typeof(long) ||
            //            typeof(T) == typeof(short) ||
            //            typeof(T) == typeof(uint) ||
            //            typeof(T) == typeof(ulong) ||
            //            typeof(T) == typeof(ushort))
            //        ob = 0;
            //    else if (typeof(T) == typeof(decimal))
            //        ob = 0m;
            //    else if (typeof(T) == typeof(double))
            //        ob = 0d;
            //    else if (typeof(T) == typeof(DateTime))
            //        ob = DateTime.MinValue;
            //    else if (typeof(T) == typeof(bool))
            //        ob = false;
            //    else if (typeof(T) == typeof(byte[]))
            //        ob = new byte[0];
            //    else if (typeof(T) == typeof(TimeSpan))
            //        ob = new TimeSpan(0);
            //    else if (typeof(T) == typeof(Guid))
            //        ob = new Guid();
            //    else
            //        throw new Exception("Error MySqlHelper.ExecuteScalar<T> - T = " + typeof(T).ToString());
            //}
            //if (typeof(T) == typeof(int))
            //{
            //    int i = 0;
            //    int.TryParse(ob + "", out i);
            //    ob = i;
            //}
            //else if (typeof(T) == typeof(decimal))
            //{
            //    decimal d = 0m;
            //    decimal.TryParse(ob + "", out d);
            //    ob = d;
            //}
            //else if (typeof(T) == typeof(DateTime))
            //{
            //    DateTime dtime = DateTime.MinValue;
            //    ob = Convert.ToDateTime(ob);
            //}

            return (T)Convert.ChangeType(ob, typeof(T));
        }

        private List<MySqlParameter> GetParametersList(IDictionary<string, object> dicParameters)
        {
            List<MySqlParameter> lst = new List<MySqlParameter>();
            if (dicParameters != null)
            {
                foreach (KeyValuePair<string, object> kv in dicParameters)
                {
                    lst.Add(new MySqlParameter(kv.Key, kv.Value));
                }
            }
            return lst;
        }

        public void InsertUpdate(string table, Dictionary<string, object> dic, List<string> lstUpdateCols, bool include)
        {
            if (include)
            {
                InsertUpdate(table, dic, lstUpdateCols);
            }
            else
            {
                List<string> lstup = new List<string>();

                foreach (var kv in dic)
                {
                    if (!lstUpdateCols.Contains(kv.Key))
                    {
                        lstup.Add(kv.Key);
                    }
                }

                InsertUpdate(table, dic, lstup);
            }
        }

        public void InsertUpdate(string table, Dictionary<string, object> dic, List<string> lstUpdateCols)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("insert into `");
            sb.Append(table);
            sb.Append("`(");

            bool isFirst = true;

            foreach (KeyValuePair<string, object> kv in dic)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(",");

                sb.Append("`");
                sb.Append(kv.Key);
                sb.Append("`");
            }

            sb.Append(")values(");

            isFirst = true;

            foreach (KeyValuePair<string, object> kv in dic)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(" , ");

                sb.Append("@v");
                sb.Append(kv.Key);
            }

            sb.Append(") on duplicate key update ");

            isFirst = true;

            foreach (string key in lstUpdateCols)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(",");

                sb.Append("`");
                sb.Append(key);
                sb.Append("`=@v");
                sb.Append(key);
            }

            sb.Append(";");

            cmd.CommandText = sb.ToString();

            cmd.Parameters.Clear();

            foreach (KeyValuePair<string, object> kv in dic)
            {
                cmd.Parameters.AddWithValue("@v" + kv.Key, kv.Value);
            }

            cmd.ExecuteNonQuery();
        }

        public void Insert(string tableName, Dictionary<string, object> dic)
        {
            StringBuilder sbCol = new System.Text.StringBuilder();
            StringBuilder sbVal = new System.Text.StringBuilder();

            foreach (KeyValuePair<string, object> kv in dic)
            {
                if (sbCol.Length == 0)
                {
                    sbCol.Append("insert into `");
                    sbCol.Append(tableName);
                    sbCol.Append("`(");
                }
                else
                {
                    sbCol.Append(",");
                }

                sbCol.Append("`");
                sbCol.Append(kv.Key);
                sbCol.Append("`");

                if (sbVal.Length == 0)
                {
                    sbVal.Append(" values(");
                }
                else
                {
                    sbVal.Append(", ");
                }

                sbVal.Append("@v");
                sbVal.Append(kv.Key);
            }

            sbCol.Append(") ");
            sbVal.Append(");");

            cmd.CommandText = sbCol.ToString() + sbVal.ToString();

            cmd.Parameters.Clear();

            foreach (KeyValuePair<string, object> kv in dic)
                cmd.Parameters.AddWithValue("@v" + kv.Key, kv.Value);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Update single row
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dicData"></param>
        /// <param name="colCond"></param>
        /// <param name="varCond"></param>
        public void Update(string tableName, Dictionary<string, object> dicData, string colCond, object varCond)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            Update(tableName, dicData, dic);
        }

        /// <summary>
        /// Update all rows that match the conditions
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dicData"></param>
        /// <param name="colCond"></param>
        /// <param name="varCond"></param>
        /// <param name="updateSingleRow"></param>
        public void Update(string tableName, Dictionary<string, object> dicData, string colCond, object varCond, bool updateSingleRow)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic[colCond] = varCond;
            Update(tableName, dicData, dic, updateSingleRow);
        }

        /// <summary>
        /// Update single row
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dicData"></param>
        /// <param name="dicCond"></param>
        public void Update(string tableName, Dictionary<string, object> dicData, Dictionary<string, object> dicCond)
        {
            Update(tableName, dicData, dicCond, true);
        }

        /// <summary>
        /// Update all rows that match the condition
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="dicData"></param>
        /// <param name="dicCond"></param>
        /// <param name="updateSingleRow"></param>
        public void Update(string tableName, Dictionary<string, object> dicData, Dictionary<string, object> dicCond, bool updateSingleRow)
        {
            cmd.Parameters.Clear();

            if (dicData.Count == 0)
                throw new Exception("dicData is empty.");

            StringBuilder sbData = new System.Text.StringBuilder();

            Dictionary<string, object> _dicTypeSource = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> kv1 in dicData)
            {
                _dicTypeSource[kv1.Key] = null;
            }

            foreach (KeyValuePair<string, object> kv2 in dicCond)
            {
                if (!_dicTypeSource.ContainsKey(kv2.Key))
                    _dicTypeSource[kv2.Key] = null;
            }

            sbData.Append("update `");
            sbData.Append(tableName);
            sbData.Append("` set ");

            bool firstRecord = true;

            foreach (KeyValuePair<string, object> kv in dicData)
            {
                if (firstRecord)
                    firstRecord = false;
                else
                    sbData.Append(",");

                sbData.Append("`");
                sbData.Append(kv.Key);
                sbData.Append("` = ");

                sbData.Append("@v");
                sbData.Append(kv.Key);
            }

            sbData.Append(" where ");

            firstRecord = true;

            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                if (firstRecord)
                    firstRecord = false;
                else
                {
                    sbData.Append(" and ");
                }

                sbData.Append("`");
                sbData.Append(kv.Key);
                sbData.Append("` = ");

                sbData.Append("@c");
                sbData.Append(kv.Key);
            }

            if (updateSingleRow)
                sbData.Append(" limit 1;");
            else
                sbData.Append(";");

            cmd.CommandText = sbData.ToString();

            foreach (KeyValuePair<string, object> kv in dicData)
            {
                cmd.Parameters.AddWithValue("@v" + kv.Key, kv.Value);
            }

            foreach (KeyValuePair<string, object> kv in dicCond)
            {
                cmd.Parameters.AddWithValue("@c" + kv.Key, kv.Value);
            }

            cmd.ExecuteNonQuery();
        }

        public int LastInsertId
        {
            get
            {
                return (int)cmd.LastInsertedId;
            }
        }

        public T GetObject<T>(string sql)
        {
            DataTable dt = Select(sql);

            return Bind<T>(dt);
        }

        public List<T> GetObjectList<T>(string sql)
        {
            DataTable dt = Select(sql);
            return BindList<T>(dt);
        }

        public string Escape(string data)
        {
            data = data.Replace("'", "''");
            data = data.Replace("\\", "\\\\");
            return data;
        }

        public string GetLikeString(string input)
        {
            string[] sa = input.Split(' ');
            StringBuilder sb = new StringBuilder();
            sb.Append("'");
            foreach (string s in sa)
            {
                sb.Append("%" + Escape(s));
            }
            sb.Append("%'");
            return sb.ToString();
        }

        static T Bind<T>(DataTable dt)
        {
            List<T> lst = BindList<T>(dt);
            if (lst.Count == 0)
            {
                return Activator.CreateInstance<T>();
            }
            return lst[0];
        }

        static List<T> BindList<T>(DataTable dt)
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            List<T> lst = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                var ob = Activator.CreateInstance<T>();

                foreach (var fieldInfo in fields)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (fieldInfo.Name == dc.ColumnName)
                        {
                            fieldInfo.SetValue(ob, GetValue(dr[dc.ColumnName], fieldInfo.FieldType));
                            break;
                        }
                    }
                }

                lst.Add(ob);
            }

            return lst;
        }

        static List<T> BindPublicFields<T>(DataTable dt)
        {
            var fields = typeof(T).GetFields();
            var properties = typeof(T).GetProperties();

            List<T> lst = new List<T>();

            foreach (DataRow dr in dt.Rows)
            {
                var ob = Activator.CreateInstance<T>();

                foreach (var fieldInfo in fields)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (fieldInfo.Name == dc.ColumnName)
                        {
                            fieldInfo.SetValue(ob, GetValue(dr[dc.ColumnName], fieldInfo.FieldType));
                            break;
                        }
                    }
                }

                foreach (var property in properties)
                {
                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (property.Name == dc.ColumnName)
                        {
                            if (property.CanWrite)
                                property.SetValue(ob, GetValue(dr[dc.ColumnName], property.PropertyType));
                            break;
                        }
                    }
                }

                lst.Add(ob);
            }

            return lst;
        }

        static object GetValue(object ob, Type targetType)
        {
            if (targetType == null)
            {
                return null;
            }
            else if (targetType == typeof(String))
            {
                return ob + "";
            }
            else if (targetType == typeof(DateTime))
            {
                try
                {
                    return Convert.ToDateTime(ob);
                }
                catch { }
                return DateTime.MinValue;
            }
            else if (targetType == typeof(bool))
            {
                try
                {
                    return Convert.ToBoolean(ob);
                }
                catch { }
                return false;
            }
            else if (targetType == typeof(short))
            {
                try
                {
                    return Convert.ToInt16(ob);
                }
                catch { }
                return 0;
            }
            else if (targetType == typeof(int))
            {
                try
                {
                    return Convert.ToInt32(ob);
                }
                catch { }
                return 0;
            }
            else if (targetType == typeof(long))
            {
                long i = 0;
                long.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(ushort))
            {
                ushort i = 0;
                ushort.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(uint))
            {
                uint i = 0;
                uint.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(ulong))
            {
                ulong i = 0;
                ulong.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(double))
            {
                double i = 0;
                double.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(decimal))
            {
                try
                {
                    return Convert.ToDecimal(ob);
                }
                catch { }
                return 0m;
            }
            else if (targetType == typeof(float))
            {
                float i = 0;
                float.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(byte))
            {
                byte i = 0;
                byte.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(sbyte))
            {
                sbyte i = 0;
                sbyte.TryParse(ob + "", out i);
                return i;
            }
            else if (targetType == typeof(TimeSpan))
            {
                return (TimeSpan)ob;
            }
            else if (targetType == typeof(System.Guid))
            {
                return (System.Guid)ob;
            }

            return Convert.ChangeType(ob, targetType);
        }

    }
}
