using CC.Public;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace CC.Framework.DAL
{
    /// <summary>
    ///  数据表架构管理（包含表字段及默认SQL语句、动态SQL语句）
    /// </summary>
    public static class SchemaManager
    {
        /// <summary>
        /// 存放所有的SQL语句(key = 数据库名称.表名称.SQL语句名称,如：test.tt.getdata)
        /// </summary>
        private static Dictionary<string, SqlObj> _sqlDic = new Dictionary<string, SqlObj>();
        /// <summary>
        /// 存放所有表的列名(key = 数据库名称.表名称)
        /// </summary>
        private static Dictionary<string, Dictionary<string, Column>> _allColumnsDic = new Dictionary<string, Dictionary<string, Column>>();

        /// <summary>
        /// 存放所有表的主键列名(key = 数据库名称.表名称)
        /// </summary>
        private static Dictionary<string, string> _primaryColumnsDic = new Dictionary<string, string>();



        private static string DefaultSQLFilePath = "SQL";
        private static string RAssemblyName = ConfigurationManager.AppSettings["SQLRAssemblyName"];
        /// <summary>
        /// 返回资源文件里面的sql文件的内容集合
        /// </summary>
        /// <returns></returns>
        static List<string> GetRNames()
        {
            try
            {
                if (string.IsNullOrEmpty(RAssemblyName))
                {
                    throw new FileNotFoundException();
                }
                Assembly ass = Assembly.Load(RAssemblyName);
                string[] arr = ass.GetManifestResourceNames();
                List<string> ret = new List<string>();
                foreach (string s in arr)
                {
                    if (Regex.IsMatch(s, string.Format(@"^{0}.{1}.[\S\s]*?.xml", RAssemblyName, DefaultSQLFilePath), RegexOptions.IgnoreCase))
                    {
                        using (StreamReader sr = new StreamReader(ass.GetManifestResourceStream(s), true))
                        {
                            ret.Add(sr.ReadToEnd());
                        }
                    }
                }
                return ret;
            }
            catch (FileNotFoundException)
            {
                throw new CCError("请配置正确的AppSettings项SQLRAssemblyName，表示SQL文件的资源的程序集名称,区分大小写");
            }
        }

        static SchemaManager()
        {
            //从XML加载

            //读取SQL文件，默认在根目录下面的sql 文件夹中 （此文件夹禁止外部访问）
            //string filePath = AppDomain.CurrentDomain.BaseDirectory;//web程序默认在根目录，非WEB程序默认在bin\debug
            //if (!filePath.EndsWith(@"\"))//非WEB程序默认不含\结束，
            //{
            //    filePath = filePath + @"\";
            //}
            //if (filePath.Contains("bin"))
            //{
            //    filePath = filePath.Substring(0, filePath.Length - 10);//默认根目录，去掉bin\debug

            //}
            //filePath = filePath + DefaultSQLFilePath;
            //var sqlFiles = Directory.GetFiles(filePath, "*.xml", SearchOption.AllDirectories);
            List<string> sqlFiles = GetRNames();
            foreach (var sqlFile in sqlFiles)
            {
                var sqlDoc = new XmlDocument();
                try
                {
                    sqlDoc.LoadXml(sqlFile);
                }
                catch
                {
                    throw new Exception(sqlFile + "格式错误!");
                }
                XmlNode tableNode = sqlDoc.DocumentElement;

                //读取每个xml里面的SQL语句，放入SQL字典，名称为DBName.TableName.SqlName
                var columnNodes = sqlDoc.DocumentElement.SelectNodes("//Table/COLUMN");
                string dbname = tableNode.Attributes["DBName"].Value;
                string tablename = tableNode.Attributes["TableName"].Value;
                string keyForTable = string.Format("{0}.{1}", dbname, tablename).ToLower();

                if (columnNodes != null)
                {
                    foreach (XmlNode columnElement in columnNodes)
                    {
                        Column column = new Column();
                        column.ColumnName = columnElement.Attributes["Name"].Value;
                        column.Type = columnElement.Attributes["Type"].Value;
                        column.Default = columnElement.Attributes["Default"].Value;
                        column.IsPrimary = Convert.ToBoolean(columnElement.Attributes["IsPrimary"].Value);
                        if (column.IsPrimary)
                        {
                            if (_primaryColumnsDic.ContainsKey(keyForTable))
                            {
                                throw new Exception(string.Format("不支持多主键:{0}", keyForTable));
                            }
                            _primaryColumnsDic.Add(keyForTable, column.ColumnName);
                        }
                        if (!_allColumnsDic.ContainsKey(keyForTable))
                            _allColumnsDic.Add(keyForTable, new Dictionary<string, Column>());
                        _allColumnsDic[keyForTable].Add(column.ColumnName, column);
                    }
                }

                //读取每个xml里面的SQL语句，放入SQL字典，名称为DBName.TableName.SqlName
                var sqlNodes = sqlDoc.DocumentElement.SelectNodes("//Table/SQL");
                if (sqlNodes != null)
                {
                    foreach (XmlNode sqlElement in sqlNodes)
                    {
                        SqlObj sqlObj = new SqlObj();
                        string key = string.Format("{0}.{1}.{2}", dbname, tablename, sqlElement.Attributes["SqlName"].Value).ToLower();
                        string sqltext = sqlElement.InnerText.Trim();
                        sqlObj.Name = key;
                        sqlObj.SqlText = sqltext;
                        if (sqlElement.Attributes["dbrwtype"] != null)//SQL本身包含
                        {
                            sqlObj.DBrwType = (DBrwType)Enum.Parse(typeof(DBrwType), sqlElement.Attributes["dbrwtype"].Value);
                        }
                        else if (tableNode.Attributes["dbrwtype"] != null)//table上包含定义
                        {
                            sqlObj.DBrwType = (DBrwType)Enum.Parse(typeof(DBrwType), tableNode.Attributes["dbrwtype"].Value);
                        }
                        else
                        {
                            sqlObj.DBrwType = DBrwType.None;//默认未设置
                        }

                        XmlNodeList defaultParameterNodes = sqlElement.SelectNodes("defaultParameters/item");
                        if (defaultParameterNodes != null)//包含默认参数
                        {
                            HashObject defaultParameters = new HashObject();
                            foreach (XmlNode item in defaultParameterNodes)
                            {
                                defaultParameters.Add(item.Attributes["name"].Value, item.Attributes["value"].Value);
                            }
                            sqlObj.DefaultParameters = defaultParameters;
                        }


                        bool isoverride = sqlElement.Attributes["isoverride"] == null ? false : true;
                        if (_sqlDic.ContainsKey(key))
                        {
                            if (isoverride)
                            {
                                _sqlDic.Remove(key);
                                _sqlDic.Add(key, sqlObj);
                            }
                        }
                        else
                        {
                            _sqlDic.Add(key, sqlObj);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 判断某个SQL是否存在
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tablename"></param>
        /// <param name="sqlName"></param>
        /// <returns></returns>
        public static bool Exists(string dbName, string tablename, string sqlName)
        {
            string key = string.Format("{0}.{1}.{2}", dbName, tablename, sqlName).ToLower();
            return _sqlDic.ContainsKey(key);
        }

        /// <summary>
        /// 获取某数据库全部列
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static Dictionary<string, Column> GetAllColumns(string dbName, string tableName)
        {
            string key = string.Format("{0}.{1}", dbName, tableName).ToLower();
            if (_allColumnsDic.ContainsKey(key))
                return _allColumnsDic[key];
            else
                return null;
        }

        /// <summary>
        /// 获取某数据库全部主键列
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetPrimaryColumn(string dbName, string tableName)
        {
            string key = string.Format("{0}.{1}", dbName, tableName).ToLower();
            if (_primaryColumnsDic.ContainsKey(key))
                return _primaryColumnsDic[key];
            else
                return null;
        }




        /// <summary>
        /// 根据模块名称、SQL名称获取具体SQL语句
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称（也可以是虚拟的）</param>
        /// <param name="sqlName">sql名称</param>
        /// <returns></returns>
        public static SqlObj GetDefaultSQL(string dbName, string tableName, string sqlName)
        {
            string key = string.Format("{0}.{1}.{2}", dbName, tableName, sqlName).ToLower(); ;
            if (!_sqlDic.ContainsKey(key))
            {
                throw new ArgumentException(string.Format("SQL语句不存在:{0}", key));
            }
            return _sqlDic[key];
        }


        /// <summary>
        /// 获取判断是否存在的SQL语句
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="whereParameters">参数名称</param>
        /// <returns></returns>
        public static string GetExistsSQL(string dbName, string tableName, HashObject whereParameters)
        {
            if (whereParameters == null || whereParameters.Count == 0)
            {
                throw new ArgumentNullException("exists 参数不能为空!");
            }

            //string sqlKey = GetSqlCacheKey(dbName, tableName, "exists", whereParameters);
            string strSql = CreateExistsSQL(dbName, tableName, whereParameters);
            return strSql;

            //if (_sqlDic.ContainsKey(sqlKey))
            //    return _sqlDic[sqlKey];
            //else
            //{
            //    string strSql = CreateExistsSQL(dbName, tableName, whereParameters);
            //    _sqlDic.Add(sqlKey, strSql);//放入缓存
            //    return strSql;
            //}
        }

        private static string CreateExistsSQL(string dbName, string tableName, HashObject parameters)
        {
            StringBuilder sql = new StringBuilder();
            Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);

            sql.Append("select count(1) from ");
            sql.Append(tableName);
            sql.Append(" where ");

            foreach (string key in parameters.Keys)
            {
                if (allColumns.ContainsKey(key))//包含在此表的列中
                {
                    sql.Append(key);
                    sql.Append("= @");
                    sql.Append(key);
                    sql.Append(" and ");
                }
            }

            sql.Remove(sql.Length - 4, 4);

            return sql.ToString();
        }

        /// <summary>
        /// 组装cache key
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="functionName">函数名称</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public static string GetSqlCacheKey(string dbName, string tableName, string functionName, HashObject parameters = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(dbName);
            sb.Append(".");
            sb.Append(tableName);
            sb.Append(".");
            sb.Append(functionName);

            Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);
            if (parameters != null && parameters.Count > 0)
            {
                sb.Append(".");
                foreach (string key in parameters.Keys)
                {
                    if (allColumns.ContainsKey(key))//参数列包含在此表的列中
                    {
                        sb.Append(key);
                        sb.Append("-");
                    }
                }
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString().ToLower();
        }

        ///// <summary>
        ///// 按主键update 时，组装SQL字符串
        ///// </summary>
        ///// <param name="dbName">数据库名称</param>
        ///// <param name="tableName">模块名称（通常是类名或表名）</param>
        ///// <param name="columns">列名集合</param>
        ///// <param name="primaryColumns">主键集合</param>
        ///// <param name="parameters">参数</param>
        ///// <returns></returns>
        //public static string GetUpdatePrimarySQL(string dbName, string tableName, HashObject parameters)
        //{
        //    string sqlKey = GetUpdatePrimarySqlKey(dbName, tableName, parameters);

        //    if (_sqlDic.ContainsKey(sqlKey))
        //        return _sqlDic[sqlKey];
        //    else
        //    {
        //        StringBuilder Sql = new StringBuilder();
        //        StringBuilder SqlFields = new StringBuilder();
        //        StringBuilder SqlWhere = new StringBuilder();
        //        Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);
        //        Dictionary<string, Column> primaryColumns = GetPrimaryColumns(dbName, tableName);

        //        SqlFields.Append("Update ");
        //        SqlFields.Append(tableName);
        //        SqlFields.Append(" set ");

        //        SqlWhere.Append(" where ");
        //        foreach (string key in parameters.Keys)
        //        {
        //            if (allColumns.ContainsKey(key))//包含在此表的列中
        //            {
        //                if (primaryColumns.ContainsKey(key))//包含在主键中
        //                {
        //                    SqlWhere.Append(key);
        //                    SqlWhere.Append("=@");
        //                    SqlWhere.Append(key);
        //                    SqlWhere.Append(" and ");
        //                }
        //                else
        //                {
        //                    SqlFields.Append(key);
        //                    SqlFields.Append("=@");
        //                    SqlFields.Append(key);
        //                    SqlFields.Append(",");
        //                }
        //            }
        //        }

        //        Sql.Append(SqlFields.ToString(0, SqlFields.Length - 1));
        //        Sql.Append(" ");
        //        Sql.Append(SqlWhere.ToString(0, SqlWhere.Length - 4));

        //        string strSql = Sql.ToString().ToLower();
        //        _sqlDic.Add(sqlKey, strSql);//放入缓存

        //        return strSql;
        //    }
        //}

        ///// <summary>
        ///// 按主键update 时，拼装一个update SQL 名称
        ///// </summary>
        ///// <param name="dbName">数据库名称</param>
        ///// <param name="tableName">模块名称（通常是类名或表名）</param>
        ///// <param name="parameters">参数</param>
        ///// <returns></returns>
        //private static string GetUpdatePrimarySqlKey(string dbName, string tableName, HashObject parameters)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(dbName);
        //    sb.Append(".");
        //    sb.Append(tableName);
        //    sb.Append(".updatePrimary.");
        //    Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);

        //    foreach (string key in parameters.Keys)
        //    {
        //        if (allColumns.ContainsKey(key))//参数列包含在此表的列中，加入到key 中
        //        {
        //            sb.Append(key);
        //            sb.Append("_");
        //        }
        //    }
        //    sb.Remove(sb.Length - 1, 1);

        //    return sb.ToString().ToLower();
        //}

        /// <summary>
        /// 获取判断是否存在的SQL语句
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="whereParameters">参数名称</param>
        /// <returns></returns>
        public static string GetCountSQL(string dbName, string tableName, HashObject whereParameters)
        {
            if (whereParameters == null || whereParameters.Count == 0)
            {
                throw new ArgumentNullException("count 参数不能为空!");
            }

            //string sqlKey = GetSqlCacheKey(dbName, tableName, "count", whereParameters);
            string strSql = CreateCountSQL(dbName, tableName, whereParameters);
            return strSql;
            //if (_sqlDic.ContainsKey(sqlKey))
            //    return _sqlDic[sqlKey];
            //else
            //{
            //    string strSql = CreateCountSQL(dbName, tableName, whereParameters);
            //    _sqlDic.Add(sqlKey, strSql);//放入缓存
            //    return strSql;
            //}
        }

        /// <summary>
        /// 创建计算count 的SQL语句
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string CreateCountSQL(string dbName, string tableName, HashObject parameters)
        {
            StringBuilder sql = new StringBuilder();
            Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);

            sql.Append("select count(*) from ");
            sql.Append(tableName);
            sql.Append(" where ");

            foreach (string key in parameters.Keys)
            {
                if (allColumns.ContainsKey(key))//包含在此表的列中
                {
                    sql.Append(key);
                    sql.Append("= @");
                    sql.Append(key);
                    sql.Append(" and ");
                }
            }

            sql.Remove(sql.Length - 4, 4);

            return sql.ToString();
        }


        /// <summary>
        /// 获取部分列
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <param name="whereParameters"></param>
        /// <param name="selectColumns"></param>
        /// <returns></returns>
        public static string GetSelectColumnsSql(string dbName, string tableName, HashObject whereParameters, string[] selectColumns)
        {
            if (whereParameters == null || whereParameters.Count == 0)
            {
                throw new ArgumentNullException("参数不能为空!");
            }
            if (selectColumns == null || selectColumns.Length == 0)
            {
                throw new ArgumentNullException("查询列不能为空!");
            }

            Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);

            StringBuilder sql = new StringBuilder("select ");
            foreach (string columnn in selectColumns)
            {
                sql.Append(columnn + ",");
            }
            sql.Remove(sql.Length - 1, 1);
            sql.Append("  from ");
            sql.Append(tableName);
            sql.Append(" where ");

            foreach (string key in whereParameters.Keys)
            {
                if (allColumns.ContainsKey(key))//包含在此表的列中
                {
                    sql.Append(key);
                    sql.Append("= @");
                    sql.Append(key);
                    sql.Append(" and ");
                }
            }

            sql.Remove(sql.Length - 4, 4);

            return sql.ToString();
        }


        /// <summary>
        /// 按自定义where列更新时，组装SQL字符串
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">模块名称（通常是类名或表名）</param>
        /// <param name="columns">列名集合</param>
        /// <param name="setParameters">更新参数列和值</param>
        /// <param name="whereParameters">where 参数列和值</param>
        /// <param name="offsetParameters">需要偏移更新的参数列update set a = a + @a</param>
        /// <param name="newParameters">组合的新参数</param>
        /// <returns></returns>
        public static string GetUpdateSQL(string dbName, string tableName, HashObject setParameters, HashObject whereParameters, string[] offsetParameters, out HashObject newParameters)
        {
            if (setParameters == null || setParameters.Count == 0)
            {
                throw new ArgumentNullException("Update set 参数不能为空!");
            }

            if (whereParameters == null || whereParameters.Count == 0)
            {
                throw new ArgumentNullException("Update where 参数不能为空!");
            }


            //string sqlKey = GetSqlCacheKeyForUpdate(dbName, tableName, setParameters, whereParameters, out newParameters);
            string strSql = CreateUpdateSQL(dbName, tableName, setParameters, whereParameters, offsetParameters, out newParameters);
            return strSql;
            //if (_sqlDic.ContainsKey(sqlKey))
            //    return _sqlDic[sqlKey];
            //else
            //{
            //    string strSql = CreateUpdateSQL(dbName, tableName, setParameters, whereParameters,offsetParameters);
            //    _sqlDic.Add(sqlKey, strSql);//放入缓存
            //    return strSql;
            //}
        }

        /// <summary>
        /// 组装update SQL 语句
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="setParameters">set 参数</param>
        /// <param name="whereParameters">条件参数</param>
        /// <returns></returns>
        private static string CreateUpdateSQL(string dbName, string tableName, HashObject setParameters, HashObject whereParameters, string[] offsetParameters, out HashObject newParameters)
        {
            StringBuilder Sql = new StringBuilder();
            StringBuilder SqlFields = new StringBuilder();
            StringBuilder SqlWhere = new StringBuilder();
            newParameters = new HashObject();

            SqlFields.Append("update ");
            SqlFields.Append(tableName);
            SqlFields.Append(" set ");

            SqlWhere.Append(" where ");
            Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);
            string primaryColumn = GetPrimaryColumn(dbName, tableName);
            string hostColumn = DALConfig.GetHostColName(dbName);

            foreach (string column in setParameters.Keys)
            {
                string key = column.ToLower().Trim();
                if (key == hostColumn)//=分区列
                    newParameters[key] = setParameters[key];
                else if (allColumns.ContainsKey(key) && key != primaryColumn)//包含在此表的列中,且不包含在主键中,不能是分区列
                {
                    SqlFields.Append(key);
                    SqlFields.Append("=");
                    if (offsetParameters != null && offsetParameters.Contains(key))//包含在偏移量数组中，则更新时包含自己
                    {
                        SqlFields.Append(key);
                        SqlFields.Append("+"); //update table set a = a + @a,b=b+@b
                    }
                    SqlFields.Append("@");
                    SqlFields.Append(key);
                    SqlFields.Append(",");

                    object setvalue = setParameters[key];
                    //setvalue 为空时，取默认值
                    if (setvalue == null || setvalue.ToString() == string.Empty)
                    {
                        setvalue = allColumns[key].GetDefaultValue();
                    }
                    newParameters.Add(key.ToLower(), setvalue);
                }
            }

            foreach (string column in whereParameters.Keys)
            {
                string key = column.ToLower().Trim();
                if (allColumns.ContainsKey(key))//包含在此表的列中
                {
                    if (newParameters.ContainsKey(key))//where 条件在 set 里面存在
                    {
                        SqlWhere.Append(key);
                        SqlWhere.Append("=@w_");
                        SqlWhere.Append(key);
                        SqlWhere.Append(" and ");

                        newParameters["w_" + key] = whereParameters[key];
                    }
                    else
                    {
                        SqlWhere.Append(key);
                        SqlWhere.Append("=@");
                        SqlWhere.Append(key);
                        SqlWhere.Append(" and ");

                        newParameters[key] = whereParameters[key];
                    }
                }
            }

            Sql.Append(SqlFields.ToString(0, SqlFields.Length - 1));
            Sql.Append(" ");
            Sql.Append(SqlWhere.ToString(0, SqlWhere.Length - 4));

            string strSql = Sql.ToString().ToLower();

            return strSql;
        }

        ///// <summary>
        ///// 按where 列 update 时，拼装一个update SQL 名称
        ///// </summary>
        ///// <param name="dbName">数据库名称</param>
        ///// <param name="tableName">模块名称（通常是类名或表名）</param>
        ///// <param name="columns">列集合</param>
        ///// <param name="setParameters">set 列和值</param>
        ///// <param name="whereParameters">where 列和值</param>
        ///// <param name="newParameters">新的参数合集(已去掉不属于本表的列)</param>
        ///// <returns></returns>
        //public static string GetSqlCacheKeyForUpdate(string dbName, string tableName, HashObject setParameters , HashObject whereParameters , out HashObject newParameters)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    newParameters = new HashObject();

        //    sb.Append(dbName);
        //    sb.Append(".");
        //    sb.Append(tableName);
        //    sb.Append(".update");

        //    Dictionary<string, Column> allColumns = GetAllColumns(dbName, tableName);
        //    string primaryColumn = GetPrimaryColumn(dbName, tableName);

        //    if (setParameters != null && setParameters.Count > 0)
        //    {
        //        sb.Append(".");
        //        foreach (string key in setParameters.Keys)
        //        {
        //            if (allColumns.ContainsKey(key) && key != primaryColumn)//包含在此表的列中,且不包含在主键中
        //            {
        //                sb.Append(key);
        //                sb.Append("-");
        //                object setvalue = setParameters[key];
        //                //setvalue 为空时，取默认值
        //                if (setvalue == null || setvalue.ToString() == string.Empty)
        //                {
        //                    setvalue = allColumns[key].GetDefaultValue();
        //                }
        //                newParameters.Add(key.ToLower(), setvalue);
        //            }
        //        }
        //        sb.Remove(sb.Length - 1, 1);
        //    }

        //    if (whereParameters != null && whereParameters.Count > 0)
        //    {
        //        sb.Append(".");
        //        foreach (string key in whereParameters.Keys)//拼接where 列
        //        {
        //            if (allColumns.ContainsKey(key))//参数列包含在此表的列中
        //            {
        //                sb.Append("w_");
        //                sb.Append(key);
        //                sb.Append("-");

        //                newParameters.Add("w_" + key.ToLower(), whereParameters[key]);
        //            }
        //        }
        //        sb.Remove(sb.Length - 1, 1);
        //    }

        //    return sb.ToString().ToLower();
        //}
    }
}
