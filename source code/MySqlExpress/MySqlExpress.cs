using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySqlConnector;
using System.Globalization;

namespace System
{
    public class MySqlExpress
    {
        public const string Version = "1.4.1";

        public enum FieldsOutputType
        {
            PublicProperties,
            PublicFields,
            PrivateFielsPublicProperties
        }

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
            cmd.CommandText = "rollback;";
            cmd.ExecuteNonQuery();
        }

        public DataTable Select(string sql)
        {
            return SelectParam(sql, null);
        }

        public DataTable Select(string sql, IDictionary<string, object> dicParameters)
        {
            if (dicParameters == null)
            {
                return SelectParam(sql, null);
            }

            List<MySqlParameter> lst = GetParametersList(dicParameters);
            return SelectParam(sql, lst);
        }

        public DataTable Select(string sql, IEnumerable<MySqlParameter> parameters)
        {
            return SelectParam(sql, parameters);
        }

        DataTable SelectParam(string sql, IEnumerable<MySqlParameter> parameters)
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

        public void Execute(string sql)
        {
            ExecuteParam(sql, null);
        }

        public void Execute(string sql, IDictionary<string, object> dicParameters)
        {
            if (dicParameters == null)
            {
                ExecuteParam(sql, null);
            }
            else
            {
                List<MySqlParameter> lst = GetParametersList(dicParameters);
                ExecuteParam(sql, lst);
            }
        }

        public void Execute(string sql, IEnumerable<MySqlParameter> parameters)
        {
            ExecuteParam(sql, parameters);
        }

        void ExecuteParam(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.Parameters.Clear();
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
            List<MySqlParameter> parameters = GetParametersList(dicParameters);
            return ExecuteScalar(sql, parameters);
        }

        public object ExecuteScalar(string sql, IEnumerable<MySqlParameter> parameters)
        {
            cmd.Parameters.Clear();

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    cmd.Parameters.Add(parameter);
                }
            }

            return cmd.ExecuteScalar();
        }

        public T ExecuteScalar<T>(string sql, IDictionary<string, object> dicParameters)
        {
            if (dicParameters == null)
            {
                return ExecuteScalarParam<T>(sql, null);
            }

            List<MySqlParameter> parameters = GetParametersList(dicParameters);

            return ExecuteScalarParam<T>(sql, parameters);
        }

        public T ExecuteScalar<T>(string sql, IEnumerable<MySqlParameter> parameters)
        {
            return ExecuteScalarParam<T>(sql, parameters);
        }

        public T ExecuteScalar<T>(string sql)
        {
            return ExecuteScalarParam<T>(sql, null);
        }

        T ExecuteScalarParam<T>(string sql, IEnumerable<MySqlParameter> parameters)
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

            DataTable dt = SelectParam(sql, parameters);

            object ob = dt.Rows[0][0];

            try
            {
                return (T)GetValue(ob, typeof(T));
            }
            catch { }

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

        public void InsertUpdate(string table, Dictionary<string, object> dic, IEnumerable<string> lstUpdateCols, bool include)
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

        public void InsertUpdate(string table, Dictionary<string, object> dic, IEnumerable<string> lstUpdateCols)
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

        public long LastInsertIdLong
        {
            get
            {
                return cmd.LastInsertedId;
            }
        }

        public T GetObject<T>(string sql)
        {
            DataTable dt = Select(sql);

            return Bind<T>(dt);
        }

        public T GetObject<T>(string sql, IDictionary<string, object> dicParameters)
        {
            DataTable dt = Select(sql, dicParameters);

            return Bind<T>(dt);
        }

        public T GetObject<T>(string sql, IEnumerable<MySqlParameter> parameters)
        {
            DataTable dt = Select(sql, parameters);

            return Bind<T>(dt);
        }

        public List<T> GetObjectList<T>(string sql)
        {
            DataTable dt = Select(sql);
            return BindList<T>(dt);
        }

        public List<T> GetObjectList<T>(string sql, IDictionary<string, object> dicParameters)
        {
            DataTable dt = Select(sql, dicParameters);
            return BindList<T>(dt);
        }

        public List<T> GetObjectList<T>(string sql, IEnumerable<MySqlParameter> parameters)
        {
            DataTable dt = Select(sql, parameters);
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
            return GetLikeString(input, false);
        }

        public string GetLikeString(string input, bool escapeSqlStringSequence)
        {
            string[] sa = input.Split(' ');
            StringBuilder sb = new StringBuilder();
            foreach (string s in sa)
            {
                sb.Append("%");
                if (escapeSqlStringSequence)
                    sb.Append(Escape(s));
                else
                    sb.Append(s);
            }
            sb.Append("%");
            return sb.ToString();
        }

        public void GenerateContainsString(string columnName, string value, StringBuilder sb, Dictionary<string, object> dicParameters)
        {
            string[] sa = value.Trim().Split(' ');

            for (int i = 0; i < sa.Length; i++)
            {
                string paramName = $"@cs{columnName}{i}";

                string paramValue = sa[i].Trim();

                if (!paramValue.StartsWith("%"))
                    paramValue = "%" + paramValue;

                if (!paramValue.EndsWith("%"))
                    paramValue += "%";

                dicParameters[paramName] = paramValue;

                if (i == 0)
                    sb.Append(" and (");
                else
                    sb.Append(" and ");

                sb.Append($"`{columnName}` like {paramName}");
            }
            sb.Append(")");
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

                foreach (var propertyInfo in properties)
                {
                    if (!propertyInfo.CanWrite)
                        continue;

                    foreach (DataColumn dc in dt.Columns)
                    {
                        if (propertyInfo.Name == dc.ColumnName)
                        {
                            propertyInfo.SetValue(ob, GetValue(dr[dc.ColumnName], propertyInfo.PropertyType));
                            break;
                        }
                    }
                }

                lst.Add(ob);
            }

            return lst;
        }

        static object GetValue(object ob, Type t)
        {
            if (t == typeof(string))
            {
                return ob + "";
            }
            else if (t == typeof(bool))
            {
                if (ob.GetType() == typeof(DBNull))
                    return false;
                return Convert.ToBoolean(ob);
            }
            else if (t == typeof(byte))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToByte(ob);
            }
            else if (t == typeof(sbyte))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToSByte(ob);
            }
            else if (t == typeof(short))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToInt16(ob);
            }
            else if (t == typeof(ushort))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToUInt16(ob);
            }
            else if (t == typeof(int))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToInt32(ob);
            }
            else if (t == typeof(uint))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0;
                return Convert.ToUInt32(ob);
            }
            else if (t == typeof(long))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0L;
                return Convert.ToInt64(ob);
            }
            else if (t == typeof(ulong))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0L;
                return Convert.ToUInt64(ob);
            }
            else if (t == typeof(float))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0F;
                return Convert.ToSingle(ob);
            }
            else if (t == typeof(double))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0D;
                return Convert.ToDouble(ob, CultureInfo.InvariantCulture);
            }
            else if (t == typeof(decimal))
            {
                if (ob.GetType() == typeof(DBNull))
                    return 0m;
                return Convert.ToDecimal(ob, CultureInfo.InvariantCulture);
            }
            else if (t == typeof(char))
            {
                if (ob.GetType() == typeof(DBNull))
                    return Convert.ToChar("");
                return Convert.ToChar(ob);
            }
            else if (t == typeof(DateTime))
            {
                if (ob.GetType() == typeof(DBNull))
                    return DateTime.MinValue;
                return Convert.ToDateTime(ob, CultureInfo.InvariantCulture);
            }
            else if (t == typeof(byte[]))
            {
                if (ob == null || ob.GetType() == typeof(DBNull))
                    return null;

                return (byte[])ob;
            }
            else if (t == typeof(Guid))
            {
                if (ob == null || ob.GetType() == typeof(DBNull))
                    return null;

                return (Guid)ob;
            }
            else if (t == typeof(TimeSpan))
            {
                if (ob == null || ob.GetType() == typeof(DBNull))
                    return null;

                return (TimeSpan)ob;
            }

            return Convert.ChangeType(ob, t);
        }

        public List<string> GetTableList()
        {
            DataTable dt = Select("show tables;");

            List<string> lst = new List<string>();

            foreach (DataRow dr in dt.Rows)
            {
                lst.Add(dr[0] + "");
            }

            return lst;
        }

        public string GenerateCustomClassField(string sql, FieldsOutputType _fieldOutputType)
        {
            return GenerateClassField(sql, _fieldOutputType);
        }

        public string GenerateTableClassFields(string tablename, FieldsOutputType _fieldOutputType)
        {
            return GenerateClassField($"select * from `{tablename}` where 1=0;", _fieldOutputType);
        }

        public string GenerateTableDictionaryEntries(string tablename)
        {
            DataTable dt = Select($"show columns from `{tablename}`;");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Dictionary<string, object> dic = new Dictionary<string, object>();");


            foreach (DataRow dr in dt.Rows)
            {
                sb.AppendLine();
                sb.Append($"            dic[\"{dr[0]}\"] = ");
            }

            return sb.ToString();
        }

        string GenerateClassField(string sql, FieldsOutputType _fieldOutputType)
        {
            var dicColType = GetColumnsDataType(sql);

            StringBuilder sb = new StringBuilder();

            if (_fieldOutputType == FieldsOutputType.PublicProperties || _fieldOutputType == FieldsOutputType.PublicFields)
            {
                foreach (var kv in dicColType)
                {
                    if (sb.Length > 0)
                    {
                        sb.AppendLine();
                    }

                    string datatypestr = GetFieldTypeString(kv.Value);

                    switch (_fieldOutputType)
                    {
                        case FieldsOutputType.PublicProperties:
                            sb.Append($"public {datatypestr} {kv.Key} {{ get; set; }}");
                            break;
                        case FieldsOutputType.PublicFields:
                            sb.Append($"public {datatypestr} {kv.Key} = {GetDefaultValueString(kv.Value)};");
                            break;
                    }
                }
            }
            else if (_fieldOutputType == FieldsOutputType.PrivateFielsPublicProperties)
            {
                bool isfirst = true;

                foreach (var kv in dicColType)
                {
                    string datatypestr = GetFieldTypeString(kv.Value);

                    if (isfirst)
                        isfirst = false;
                    else
                        sb.AppendLine();

                    sb.Append($"{datatypestr} {kv.Key} = {GetDefaultValueString(kv.Value)};");
                }

                sb.AppendLine();

                foreach (var kv in dicColType)
                {
                    string datatypestr = GetFieldTypeString(kv.Value);

                    sb.AppendLine();
                    sb.Append($"public {datatypestr} {GetUpperCaseColName(kv.Key)} {{ get {{ return {kv.Key}; }} set {{ {kv.Key} = value; }}");
                }
            }

            return sb.ToString();
        }

        Dictionary<string, Type> GetColumnsDataType(string sql)
        {
            DataTable dt = Select(sql);

            Dictionary<string, Type> dic = new Dictionary<string, Type>();

            foreach (DataColumn dc in dt.Columns)
            {
                dic[dc.ColumnName] = dc.DataType;
            }

            return dic;
        }

        string GetUpperCaseColName(string colname)
        {
            bool toUpperCase = true;

            StringBuilder sb = new StringBuilder();
            foreach (char c in colname)
            {
                if (c == '_')
                {
                    toUpperCase = true;
                    continue;
                }
                if (toUpperCase)
                {
                    sb.Append(Char.ToUpper(c));
                    toUpperCase = false;
                    continue;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        string GetDefaultValueString(Type t)
        {
            if (t == typeof(string))
            {
                return "\"\"";
            }
            else if (t == typeof(bool))
            {
                return "false";
            }
            else if (t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint))
            {
                return "0";
            }
            else if (t == typeof(long) ||
                t == typeof(ulong))
            {
                return "0l";
            }
            else if (t == typeof(float))
            {
                return "0f";
            }
            else if (t == typeof(double))
            {
                return "0d";
            }
            else if (t == typeof(decimal))
            {
                return "0m";
            }
            else if (t == typeof(char))
            {
                return "''";
            }
            else if (t == typeof(DateTime))
            {
                return "DateTime.MinValue";
            }
            else if (t == typeof(byte[]))
            {
                return "null";
            }
            else if (t == typeof(Guid))
            {
                return "null";
            }
            else if (t == typeof(TimeSpan))
            {
                return "null";
            }

            throw new Exception($"Unhandled Data Type: {t.ToString()}. Please report this to the development team.");
        }

        string GetFieldTypeString(Type t)
        {
            if (t == typeof(string))
            {
                return "string";
            }
            else if (t == typeof(bool))
            {
                return "bool";
            }
            else if (t == typeof(byte) || t == typeof(sbyte))
            {
                return "byte";
            }
            else if (t == typeof(short) || t == typeof(ushort))
            {
                return "short";
            }
            else if (t == typeof(int) || t == typeof(uint))
            {
                return "int";
            }
            else if (t == typeof(long) || t == typeof(ulong))
            {
                return "long";
            }
            else if (t == typeof(float))
            {
                return "float";
            }
            else if (t == typeof(double))
            {
                return "double";
            }
            else if (t == typeof(decimal))
            {
                return "decimal";
            }
            else if (t == typeof(char))
            {
                return "char";
            }
            else if (t == typeof(DateTime))
            {
                return "DateTime";
            }
            else if (t == typeof(byte[]))
            {
                return "byte[]";
            }
            else if (t == typeof(Guid))
            {
                return "Guid";
            }
            else if (t == typeof(TimeSpan))
            {
                return "TimeSpan";
            }

            throw new Exception($"Unhandled Data Type: {t.ToString()}. Please report this to the development team.");
        }

        public string GetCreateTableSql(string tablename)
        {
            DataTable dt = Select($"show create table `{tablename}`;");

            return dt.Rows[0][1] + "";
        }

        public string GenerateUpdateColumnList(string tablename)
        {
            DataTable dt = Select($"show columns from `{tablename}`;");

            List<string> lst = new List<string>();

            foreach (DataRow dr in dt.Rows)
            {
                if (dr["Key"] + "" == "")
                {
                    lst.Add(dr[0] + "");
                }
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("List<string> lstUpdateCol = new List<string>();");

            foreach (var l in lst)
            {
                sb.AppendLine();
                sb.Append($"lstUpdateCol.Add(\"{l}\");");
            }

            return sb.ToString();
        }
    }
}