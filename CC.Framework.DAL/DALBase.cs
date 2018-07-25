using CC.Framework.DAL;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using CC.DataAccess.Extend;

namespace CC.Framework.DAL
{
    /// <summary>
    /// 水平分区的对象数据层基类
    /// </summary>
    public class DALBase
    {
        private string _dbName;
        private string _tableName;
        private Dictionary<string, Column> _allColumns;
        private string _primaryColumn;//暂时只支持单主键
        private DBrwType _dbrwType;//读写类型
        //private Dictionary<string, Column> _primaryColumns;
        //private static string _SQLForInsert = "insert";
        private static Dictionary<string, DALBase> _dic = new Dictionary<string, DALBase>();
        private static object _lock = new object();

        /// <summary>
        /// 是否记录日志，默认true
        /// </summary>
        private bool LogEnable { get; set; }

        //static DALBase()
        //{
        //    _varcharTypes.Add("char");
        //    _varcharTypes.Add("varchar");
        //    _varcharTypes.Add("blob");
        //    _varcharTypes.Add("text");
        //    _varcharTypes.Add("binary");
        //    _varcharTypes.Add("datetime");
        //    _varcharTypes.Add("date");
        //    _varcharTypes.Add("time");
        //}


        /// <summary>
        /// 所有列信息
        /// </summary>
        public Dictionary<string, Column> AllColumns
        {
            get
            {
                return _allColumns;
            }
        }

        ///// <summary>
        ///// 所有主键列信息
        ///// </summary>
        //public Dictionary<string, Column> PrimaryColumns
        //{
        //    get
        //    {
        //        return _allColumns;
        //    }
        //}

        /// <summary>
        /// 创建一个DALBase 实例
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <returns></returns>
        public static DALBase Create(string dbName, string tableName, DBrwType dbrwType = DBrwType.Write)
        {
            //if (ifforbatch == true)//批量操作的时候，不取缓存的值（因为有不同的实例参数）
            //    return new DALBase(dbName, tableName, dbrwType);

            string key = string.Format("{0}.{1}.{2}", dbName, tableName, (int)dbrwType).ToLower();
            if (!_dic.ContainsKey(key))
            {
                lock (_lock)
                {
                    if (!_dic.ContainsKey(key))
                    {
                        DALBase dal = new DALBase(dbName, tableName, dbrwType);
                        _dic.Add(key, dal);
                    }
                }
            }

            return _dic[key];
        }

        /// <summary>
        /// 构造一个DALBase 实例
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        public DALBase(string dbName, string tableName, DBrwType dbrwType = DBrwType.Write, bool logenable = false)
        {
            this._dbName = dbName.ToLower();
            this._tableName = tableName.ToLower();
            this._dbrwType = dbrwType;
            this._allColumns = SchemaManager.GetAllColumns(this._dbName, this._tableName);
            this._primaryColumn = SchemaManager.GetPrimaryColumn(this._dbName, this._tableName);
            this.LogEnable = logenable;
        }
        /// <summary>
        /// 构造一个DALBase 实例
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        public DALBase(string dbName, DBrwType dbrwType = DBrwType.Write, bool logenable = false)
        {
            this._dbName = dbName.ToLower();
            this._dbrwType = dbrwType;
            this.LogEnable = logenable;
        }
        #region save （插入或更新）
        /// <summary>
        /// 保存，如果存在符合主键列的数据，则更新（默认）,否则，插入
        /// 如果 updateIfExists = false ，存在则不做任何处理
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public long Save(HashObject parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                throw new ArgumentNullException("parameters 不能为空!");
            }

            HashObject whereParameters = null;
            if (parameters.ContainsKey(this._primaryColumn))
            {
                whereParameters = new HashObject();
                whereParameters.Add(this._primaryColumn, parameters[this._primaryColumn]);
                string colName = DALConfig.GetHostColName(this._dbName);
                if (!string.IsNullOrEmpty(colName))
                {
                    whereParameters.Add(colName, parameters[colName]);
                }
            }


            return this.Save(parameters, whereParameters, null);
        }

        /// <summary>
        /// 保存，如果存在符合where列 的数据，则更新（默认）,否则，插入
        /// 如果 updateIfExists = false ，存在则不做任何处理
        /// offsetParameters : 需要偏移更新的数据列,一般用于末态表
        /// </summary>
        /// <param name="setParameters">set 参数</param>
        /// <param name="whereColumns">条件参数 </param>
        /// <param name="offsetParameters">需要偏移更新的参数列update set a = a + @a</param>
        /// <returns></returns>
        public long Save(HashObject setParameters, string[] whereColumns, string[] offsetParameters = null)
        {
            if (setParameters == null || setParameters.Count == 0)
            {
                throw new ArgumentNullException("setParameters 不能为空!");
            }

            HashObject whereParameters = null;
            if (whereColumns != null)
            {
                whereParameters = new HashObject();

                foreach (string key in whereColumns)
                {
                    whereParameters.Add(key, setParameters[key]);
                }
            }

            return this.Save(setParameters, whereParameters, offsetParameters);
        }

        /// <summary>
        /// 根据条件保存
        /// </summary>
        /// <param name="setParameters">set 参数</param>
        /// <param name="whereParameters">条件参数</param>
        /// <param name="offsetParameters">需要偏移更新的参数列update set a = a + @a</param>
        /// <returns></returns>
        public long Save(HashObject setParameters, HashObject whereParameters, string[] offsetParameters = null)
        {
            if (setParameters == null || setParameters.Count == 0)
            {
                throw new ArgumentNullException("setParameters 不能为空!");
            }


            long result = 0;
            if (whereParameters != null && whereParameters.Count > 0)//如果存在条件
            {
                bool exists = Exists(whereParameters);
                if (exists == true)//存在时更新
                {
                    UpdateByWhere(setParameters, whereParameters, offsetParameters);
                    if (whereParameters.ContainsKey(this._primaryColumn))//如果条件中包含主键，则返回主键
                    {
                        result = Convert.ToInt64(whereParameters[this._primaryColumn]);
                    }
                }
                else//不存在时插入
                {
                    result = Insert(setParameters);
                }
            }
            else//无条件时插入
            {
                result = Insert(setParameters);
            }

            return result;
        }

        /// <summary>
        /// 填充新的insert 参数 (按照默认值)
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private HashObject CreateNewInsertParameters(HashObject parameters)
        {
            HashObject newParameters = new HashObject();
            foreach (KeyValuePair<string, Column> pair in this._allColumns)//循环所有列
            {
                if (pair.Value.Type == "timestamp")//timestamp不添加默认值
                    continue;

                if (!parameters.ContainsKey(pair.Key)
                    || (parameters.ContainsKey(pair.Key) && (parameters[pair.Key] == null || parameters[pair.Key].ToString() == string.Empty))
                    )//不包含或者包含此列单但数据为空则补充默认值
                {
                    newParameters.Add(pair.Key, pair.Value.GetDefaultValue());
                }
                else
                {
                    newParameters.Add(pair.Key, parameters[pair.Key]);
                }
            }

            return newParameters;
        }

        /// <summary>
        /// 填充新的insert 参数 (按照默认值), 同时自动生成ID
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private HashObject CreateNewInsertParametersAutoId(HashObject parameters, out long result)
        {
            var rev = CreateNewInsertParameters(parameters);

            //主键.
            result = NeedCreateNewId(parameters) ? GetIdentity() : Convert.ToInt64(parameters[_primaryColumn]);
            rev[_primaryColumn] = result;

            return rev;
        }

        /// <summary>
        /// 判断是否需要创建新的ID
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private bool NeedCreateNewId(HashObject parameters)
        {
            //参数不包含，或者为0，自动自增一个新的
            return !parameters.ContainsKey(_primaryColumn) || Convert.ToInt64(parameters[_primaryColumn]) == 0;
        }

        #region 批量插入
        //private static List<string> _varcharTypes = new List<string>();//用于批量insert,需要'的类型 

        //private List<DALObject> _insertBatchList = new List<DALObject>();//特殊实例参数，不同线程不一样
        private List<string> _insertBatchList = new List<string>();//用于批量插入
        private long _profileid = 0;//用于批量插入
        private int _totalNeedNewId = 0; //批量插入时，统计需要生成的ID数量
        private long _firstNewId = 0;

        public void BatchInsertBegin(IEnumerable<HashObject> parameters)
        {
            foreach (var item in parameters)
            {
                BatchInsertBegin(item);
            }
        }

        public void BatchInsertBegin(HashObject parameters)
        {
            //if (this == DALBase.Create(this._dbName, this._tableName, this._dbrwType))
            //{
            //    throw new CCError("批量插入的时候,dalbase需要传入ifforbatch参数");
            //}
            if (parameters == null || parameters.Count == 0)
            {
                throw new ArgumentNullException("parameters 不能为空!");
            }
            this._profileid = Convert.ToInt64(parameters["profileid"]);
            
            HashObject newParameters = CreateNewInsertParameters(parameters);
            var needCreateNewId = NeedCreateNewId(parameters);
            StringBuilder itemSqlData = new StringBuilder("(");
            foreach (KeyValuePair<string, Column> pair in this._allColumns)//循环所有列
            {
                if (needCreateNewId && _primaryColumn == pair.Key) //自动生成主键处理
                {
                    itemSqlData.AppendFormat("@{0} + {1}", _primaryColumn, _totalNeedNewId); //参数连续
                    _totalNeedNewId++;
                }
                else
                {
                    object value = newParameters[pair.Key];
                    string columnType = pair.Value.Type;

                    if (columnType == "time" || columnType == "date" || columnType == "datetime")
                        itemSqlData.Append(string.Format("'{0}'", Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss")));

                    else if (columnType == "binary" || columnType.Contains("text") || columnType == "blob" || columnType == "varchar" || columnType == "char")
                    {
                        if (newParameters[pair.Key].GetType() == typeof(string))
                            itemSqlData.Append(string.Format("'{0}'", CCExtensions.GetMySQLString(value.ToString()).Trim()));
                        else
                            itemSqlData.Append(string.Format("'{0}'", value));
                    }
                    else
                        itemSqlData.Append(value);
                }
                itemSqlData.Append(",");
            }
            itemSqlData.Remove(itemSqlData.Length - 1, 1);//去掉最后一个分号
            itemSqlData.Append(")");
            this._insertBatchList.Add(itemSqlData.ToString());
        }

        /// <summary>
        /// 获取批量执行SQL语句
        /// </summary>
        /// <param name="sqlList"></param>
        /// <returns></returns>
        private string GetBatchInsertSql(List<string> sqlList)
        {
            StringBuilder insertSql = new StringBuilder("SET SESSION group_concat_max_len=102400;");
            insertSql.Append("insert into " + this._tableName + " (");
            foreach (KeyValuePair<string, Column> pair in this._allColumns)//循环所有列
            {
                if (pair.Value.Type == "timestamp")//timestamp不处理
                    continue;

                insertSql.Append(pair.Key);
                insertSql.Append(",");
            }
            insertSql.Remove(insertSql.Length - 1, 1);//去掉最后一个逗号
            insertSql.Append(") values ");
            foreach (string itemdata in sqlList)
            {
                insertSql.Append(itemdata);
                insertSql.Append(",");
            }
            insertSql.Remove(insertSql.Length - 1, 1);//去掉最后一个逗号
            insertSql.Append(";");

            return insertSql.ToString();
        }


        /// <summary>
        /// 批量insert语句提交
        /// </summary>
        /// <param name="maxCount">最大拼接条数(默认100)</param>
        /// <returns></returns>
        public int BatchInsertComplete(int maxCount = 100)
        {
            if (this._insertBatchList.Count == 0)
                return 0;

            //if (this == DALBase.Create(this._dbName, this._tableName, this._dbrwType))
            //{
            //    throw new CCError("批量插入的时候,dalbase需要传入ifforbatch参数");
            //}

            //int rows = this.ExecuteBatch(this._insertBatchList);
            //return rows;
            int rows = 0;
            HashObject parameters = new HashObject();
            parameters["profileid"] = this._profileid;
            if (_totalNeedNewId > 0) parameters[_primaryColumn] = _firstNewId > 0 ? _firstNewId : (_firstNewId = GetIdentity(_totalNeedNewId)[0]);

            int times = (int)Math.Ceiling(this._insertBatchList.Count / (maxCount * 1.0));
            for (int i = 0; i < times; i++)
            {
                List<string> arrTemp = new List<string>();
                for (int j = 0; j < maxCount; j++)
                {
                    int index = i * maxCount + j;
                    if (index >= this._insertBatchList.Count)
                        break;

                    arrTemp.Add(this._insertBatchList[index]);
                }
                string insertSql = this.GetBatchInsertSql(arrTemp);
                using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, insertSql, parameters, SqlType.CmdText))
                {
                    dbhelper.LogEnable = this.LogEnable;
                    int _rows = dbhelper.ExecuteNonQuery();
                    rows = rows + _rows;
                }
            }

            this._profileid = 0;
            this._insertBatchList = null;
            _totalNeedNewId = 0;
            _firstNewId = 0;

            return rows;
        }

        /// <summary>
        /// 将批量插入语句写入流
        /// </summary>
        /// <param name="sw"></param>
        public int WriteBatchInserSqlToStream(StreamWriter sw, bool clear = true, int maxCount = 1000)
        {
            if (this._insertBatchList.Count == 0)
                return 0;

            //if (this == DALBase.Create(this._dbName, this._tableName, this._dbrwType))
            //{
            //    throw new CCError("批量插入的时候,dalbase需要传入ifforbatch参数");
            //}
                        
            StringBuilder insertSql = new StringBuilder();
            if (_totalNeedNewId > 0)
            {
                insertSql.AppendFormat("SET @{0} = {1};\r\n", _primaryColumn, _firstNewId > 0 ? _firstNewId : (_firstNewId = GetIdentity(_totalNeedNewId)[0]));
            }
            //列名
            insertSql.Append("insert into " + this._tableName + " (");
            foreach (KeyValuePair<string, Column> pair in this._allColumns)//循环所有列
            {
                if (pair.Value.Type == "timestamp")//timestamp不处理
                    continue;

                insertSql.Append(pair.Key);
                insertSql.Append(",");
            }
            insertSql.Remove(insertSql.Length - 1, 1);//去掉最后一个逗号
            insertSql.Append(") values \r\n");
            sw.Write(insertSql.ToString());

            //值
            var len = _insertBatchList.Count; //剩余
            while (len > 0)
            {
                sw.Flush();
                insertSql.Clear();

                var count = len < maxCount ? len : maxCount; //本次读取数量

                var splitStr = ",\r\n";
                foreach (var item in _insertBatchList.Skip(_insertBatchList.Count - len).Take(count))
                {
                    insertSql.Append(item);
                    insertSql.Append(splitStr);
                }

                //后续处理
                len -= count;
                if (len == 0)
                {
                    insertSql.Remove(insertSql.Length - splitStr.Length, splitStr.Length);//去掉最后一个逗号
                    insertSql.Append(";\r\n");
                }

                //写入流
                sw.Write(insertSql.ToString());
            }

            var insertCount = _insertBatchList.Count;
            if (clear)
            {
                this._profileid = 0;
                this._insertBatchList.Clear();
                this._totalNeedNewId = 0;
                this._firstNewId = 0;
            }

            return insertCount;
        } 
        #endregion

        /// <summary>
        /// 插入（返回自增列的值）
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns>如果外部已经获取自增列，则返回-1，否则，返回自增列的值</returns>
        public long Insert(HashObject parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                throw new ArgumentNullException("parameters 不能为空!");
            }

            long result = -1;
            HashObject newParameters = CreateNewInsertParametersAutoId(parameters, out result);

            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, "insert", newParameters))
            {
                dbhelper.LogEnable = this.LogEnable;
                dbhelper.ExecuteNonQuery();
            }

            return result;
        }

        /// <summary>
        /// 传参数按主键更新数据（参数中必须包含全部主键列）
        /// </summary>
        /// <param name="parameters">全部参数</param>
        /// <param name="changecolumns">需要更新的参数</param>
        /// <param name="offsetParameters">需要偏移更新的参数（update xxx set a=a+@a...）</param>
        /// <returns></returns>
        public int Update(HashObject parameters, string[] changecolumns = null, string[] offsetParameters = null)
        {
            this.CheckForPrimary(parameters);//判断参数中是否包含主键列，如果不包含，抛出异常

            string hostColName = DALConfig.GetHostColName(this._dbName);
            HashObject whereParameters = new HashObject();
            whereParameters.Add(this._primaryColumn, parameters[this._primaryColumn]);
            whereParameters.Add(hostColName, parameters[hostColName]);

            HashObject setParameters = new HashObject();
            if (changecolumns != null)
            {
                foreach (string column in changecolumns)
                {
                    setParameters[column] = parameters[column];
                }
            }
            else
            {
                setParameters = parameters.Copy();
                setParameters.Remove(this._primaryColumn);//移除主键列
                setParameters.Remove(hostColName);//移除分区列
            }

            return UpdateByWhere(setParameters, whereParameters, offsetParameters);
        }

        /// <summary>
        ///  根据特定列的值更新
        /// </summary>
        /// <param name="setParameters">需要更新的列和值</param>
        /// <param name="whereParameters">where 条件列和值</param>
        /// <param name="offsetParameters">需要偏移更新的参数列update set a = a + @a</param>
        /// <returns></returns>
        public int UpdateByWhere(HashObject setParameters, HashObject whereParameters, string[] offsetParameters = null)
        {
            if (setParameters == null || setParameters.Count == 0)
            {
                throw new ArgumentNullException("setParameters 不能为空!");
            }
            if (whereParameters == null || whereParameters.Count == 0)
            {
                throw new ArgumentNullException("whereParameters 不能为空!");
            }

            HashObject newParameters;
            string sql = SchemaManager.GetUpdateSQL(this._dbName, this._tableName, setParameters, whereParameters, offsetParameters, out newParameters);
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sql, newParameters, SqlType.CmdText))
            {
                int rows = dbhelper.ExecuteNonQuery();
                return rows;
            }
        }

        #endregion


        #region ExecuteNonQuery  Delete
        /// <summary>
        /// 执行特定SQL语句
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <param name="sqlName">SQL名称</param>
        /// <returns>受影响的行数</returns>
        public int ExecuteNonQuery(HashObject parameters, string sqlName, SqlType sqlType = SqlType.SqlName)
        {
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters, sqlType))
            {
                dbhelper.LogEnable = this.LogEnable;
                int rows = dbhelper.ExecuteNonQuery();
                return rows;
            }
        }



        ///// <summary>
        ///// 批量执行SQL语句（组织成一个大的SQL语句，建议尽量只包含insert语句）
        ///// </summary>
        ///// <param name="sqlDataList"></param>
        ///// <returns></returns>
        //public int ExecuteBatch(List<DALObject> sqlDataList)
        //{
        //    if (sqlDataList.Count == 0)
        //        return 0;

        //    StringBuilder cmdtext = new StringBuilder();
        //    //组织SQL语句
        //    HashObject parameters = new HashObject();
        //    parameters["profileid"] = sqlDataList[0].Parameters["profileid"];
        //    foreach (DALObject obj in sqlDataList)
        //    {
        //        string sql = string.Empty;
        //        if (obj.ExSqlType == SqlType.SqlName)
        //        {
        //            SqlObj sqlobj = SchemaManager.GetDefaultSQL(this._dbName, this._tableName, obj.SqlName);
        //            sql = sqlobj.SqlText;
        //        }
        //        if (obj.Parameters != null && obj.Parameters.Count > 0)
        //        {
        //            string oneitemvalue = string.Empty;
        //            foreach (KeyValuePair<string, object> oneitem in obj.Parameters)
        //            {
        //                if (oneitem.Value != null)
        //                {
        //                    if (this._allColumns.ContainsKey(oneitem.Key) && _varcharTypes.Contains(this._allColumns[oneitem.Key].Type))
        //                        oneitemvalue = string.Format("'{0}'", oneitem.Value.ToString());
        //                    else
        //                        oneitemvalue = oneitem.Value.ToString();
        //                }
        //                sql = sql.Replace("@" + oneitem.Key, oneitemvalue);
        //            }
        //        }
        //        cmdtext.Append(sql);
        //    }


        //    using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, cmdtext.ToString(), parameters, SqlType.CmdText))
        //    {
        //        int rows = dbhelper.ExecuteNonQuery();
        //        return rows;
        //    }
        //}

        /// <summary>
        /// 删除数据(根据主键)
        /// </summary>
        /// <param name="parameters">参数（需包含主键）</param>
        /// <returns></returns>
        public bool Delete(HashObject parameters, string sqlName = "delete")
        {
            //this.CheckForPrimary(parameters);//判断参数中是否包含主键列，如果不包含，抛出异常
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters))
            {
                dbhelper.LogEnable = this.LogEnable;
                int rows = dbhelper.ExecuteNonQuery();
                return rows > 0;
            }
        }

        /// <summary>
        /// 根据主键删除（单主键）
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public bool DeleteByPrimary(HashObject parameters)
        {
            HashObject delparameters = parameters.Copy(new string[] { "profileid" });
            delparameters["#" + this._primaryColumn] = parameters[this._primaryColumn];
            bool result = this.Delete(delparameters);
            return result;
        }

        /// <summary>
        /// 判断参数中是否包含主键列，如果不包含，抛出异常
        /// </summary>
        /// <param name="parameters"></param>
        private void CheckForPrimary(HashObject parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                throw new ArgumentNullException("parameters 不能为空!");
            }

            //参数中必须包含所有主键列
            if (!parameters.Keys.Contains(this._primaryColumn))
            {
                throw new ArgumentNullException(this._primaryColumn);
            }
        }
        #endregion


        #region  Exists GetData Count GetDataList
        /// <summary>
        /// 判断当前数据是否存在
        /// </summary>
        /// <param name="whereParameters">条件参数</param>
        /// <returns></returns>
        public bool Exists(HashObject whereParameters)
        {
            string sql = SchemaManager.GetExistsSQL(this._dbName, this._tableName, whereParameters);
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sql, whereParameters, SqlType.CmdText))
            {
                dbhelper.LogEnable = this.LogEnable;
                bool result = dbhelper.Exists();
                return result;
            }
        }

        /// <summary>
        /// 判断当前数据是否存在
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <param name="sqlName">sql语句</param>
        /// <returns></returns>
        public bool Exists(HashObject parameters, string sqlName)
        {
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters))
            {
                dbhelper.LogEnable = this.LogEnable;
                bool result = dbhelper.Exists();
                return result;
            }
        }


        /// <summary>
        /// 返回单行数据
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        public HashObject GetData(HashObject parameters, string sqlName = "getdata", SqlType sqlType = SqlType.SqlName)
        {
            //this.CheckForPrimary(parameters);//判断参数中是否包含主键列，如果不包含，抛出异常

            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters, sqlType))
            {
                dbhelper.LogEnable = this.LogEnable;
                HashObject result = dbhelper.GetData();
                return result;
            }
        }

        /// <summary>
        /// 获取指定列的值（传入）
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="selectColumns"></param>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        public HashObject GetDataColumns(HashObject whereParameters, string[] selectColumns)
        {
            //this.CheckForPrimary(parameters);//判断参数中是否包含主键列，如果不包含，抛出异常
            string sql = SchemaManager.GetSelectColumnsSql(this._dbName, this._tableName, whereParameters, selectColumns);
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sql, whereParameters, SqlType.CmdText))
            {
                dbhelper.LogEnable = this.LogEnable;
                HashObject result = dbhelper.GetData();
                return result;
            }
        }

        /// <summary>
        /// 获取指定列的值（传入）
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="selectColumns"></param>
        /// <param name="sqlType"></param>
        /// <returns></returns>
        public HashObjectList GetDataListColumns(HashObject whereParameters, string[] selectColumns, int count = 0)
        {
            //this.CheckForPrimary(parameters);//判断参数中是否包含主键列，如果不包含，抛出异常
            string sql = SchemaManager.GetSelectColumnsSql(this._dbName, this._tableName, whereParameters, selectColumns);
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sql, whereParameters, SqlType.CmdText))
            {
                dbhelper.LogEnable = this.LogEnable;
                HashObjectList result = dbhelper.GetDataList(count);
                return result;
            }
        }

        ///// <summary>
        ///// 根据主键返回一条数据
        ///// </summary>
        ///// <param name="id">主键id</param>
        ///// <returns></returns>
        //public HashObject GetData(long id)
        //{
        //    HashObject parameters = new HashObject();
        //    parameters.Add(this._primaryColumn, id);

        //    HashObject result = GetData(parameters);

        //    return result;
        //}


        /// <summary>
        /// 计算符合条件的数据条数
        /// </summary>
        /// <param name="whereParameters">条件参数</param>
        /// <returns></returns>
        public int Count(HashObject whereParameters)
        {
            string sql = SchemaManager.GetCountSQL(this._dbName, this._tableName, whereParameters);
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sql, whereParameters, SqlType.CmdText))
            {
                dbhelper.LogEnable = this.LogEnable;
                object result = dbhelper.ExecuteScalarRead();
                return Convert.ToInt32(result);
            }
        }

        /// <summary>
        /// 支持ExecuteScalar,返回object
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <param name="sqlName">sql名称</param>
        /// <returns></returns>
        public object ExecuteScalarRead(HashObject parameters, string sqlName)
        {
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters))
            {
                dbhelper.LogEnable = this.LogEnable;
                object result = dbhelper.ExecuteScalarRead();
                return result;
            }
        }

        /// <summary>
        /// 返回多行数据
        /// </summary>
        /// <param name="parameters">参数</param>
        /// <param name="count">返回数量（默认20）</param>
        /// <param name="sqlName">SQL语句名称（默认：getdatalist）</param>
        /// <returns></returns>
        public HashObjectList GetDataList(HashObject parameters, string sqlName = "getdatalist", SqlType sqltype = SqlType.SqlName, int count = 0)
        {
            using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbrwType, this._dbName, this._tableName, sqlName, parameters, sqltype))
            {
                dbhelper.LogEnable = this.LogEnable;
                HashObjectList result = dbhelper.GetDataList(count);
                return result;
            }
        }


        ///// <summary>
        ///// 返回多行数据
        ///// </summary>
        ///// <param name="searchCfg">搜索配置</param>
        ///// <param name="parameters">参数</param>
        ///// <returns></returns>
        //public HashObjectList GetDataList(SearchCfg searchCfg, HashObject parameters)
        //{
        //    if (parameters == null)
        //        throw new Exception("parameters 不能为空");

        //    string sql = searchCfg.SQL;
        //    if (string.IsNullOrEmpty(sql))
        //        sql = "select #fields from " + this._tableName + " where #where ";
        //    string fields = "*";
        //    StringBuilder where = new StringBuilder("1=1");
        //    if (!string.IsNullOrEmpty(searchCfg.Fields))
        //    {
        //        fields = searchCfg.Fields;
        //    }
        //    sql = sql.Replace("#fields", fields);
        //    if (searchCfg.Where != null && searchCfg.Where.Count > 0)
        //    {
        //        #region where 处理
        //        foreach (WhereColumn column in searchCfg.Where)
        //        {
        //            if (!parameters.ContainsKey(column.Name))//参数不包含此配置
        //                continue;

        //            if (parameters[column.Name] == null || parameters[column.Name].ToString() == string.Empty)
        //                continue;

        //            where.Append(" and ");
        //            where.Append(column.Code);
        //            switch (column.CompareType)
        //            {
        //                case CompareType.Equal:
        //                    where.Append(" = ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.Greater:
        //                    where.Append(" > ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.GreaterEqual:
        //                    where.Append(" >= ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.Less:
        //                    where.Append(" < ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.LessEqual:
        //                    where.Append(" <= ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.NoEqual:
        //                    where.Append(" <> ");
        //                    where.Append("@" + column.Name);//参数
        //                    break;
        //                case CompareType.Like:
        //                    where.Append(" like '%");
        //                    where.Append(parameters[column.Name]);//参数
        //                    where.Append("%'");
        //                    break;
        //                case CompareType.LikeLeft:
        //                    where.Append(" like '");
        //                    where.Append(parameters[column.Name]);//参数
        //                    where.Append("%'");
        //                    break;
        //                //case CompareType.In:
        //                //    where.Append(" in ");
        //                //    break;
        //                default:
        //                    break;
        //            }

        //        }
        //        #endregion
        //    }
        //    sql = sql.Replace("#where", where.ToString());
        //    if (!string.IsNullOrEmpty(searchCfg.Order))
        //    {
        //        sql += " order by " + searchCfg.Order;
        //    }

        //    if (!string.IsNullOrEmpty(searchCfg.Limit))
        //    {
        //        sql += " limit " + searchCfg.Limit;
        //    }

        //    using (DbHelperWrapper dbhelper = new DbHelperWrapper(this._dbName, this._tableName, sql, parameters, SqlType.CmdText))
        //    {
        //        HashObjectList result = dbhelper.GetDataList(0);
        //        return result;
        //    }
        //}
        #endregion

        #region GetIdentity
        /// <summary>
        /// 获取自增字段的值
        /// </summary>
        /// <returns></returns>
        public long GetIdentity()
        {
            return GlobalIdentity.GetGetIdentity(this._dbName, this._tableName);
        }

        /// <summary>
        /// 获取一批自增列
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public long[] GetIdentity(int count)
        {
            return GlobalIdentity.GetGetIdentity(this._dbName, this._tableName, count);
        }

        /// <summary>
        /// 获取当前最大ID
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public long GetMaxIdentity()
        {
            return GlobalIdentity.GetGetIdentity(_dbName, _tableName);
        }
        #endregion
    }

    ///// <summary>
    ///// 搜索配置
    ///// </summary>
    //public class SearchCfg
    //{
    //    /// <summary>
    //    /// 构造一个查询条件
    //    /// </summary>
    //    /// <param name="where">where条件</param>
    //    /// <param name="fields">取出的列</param>
    //    /// <param name="order">排序条件(不需要传入"order by ")</param>
    //    /// <param name="limit">limit 数量(不需要传入"limit")</param>
    //    /// <param name="sql">sql语句（可选，默认为select #fields from tableName where #where）</param>
    //    public SearchCfg(List<WhereColumn> where = null,string fields = null, string order = null, string limit = null, string sql = null)
    //    {
    //        this.Fields = fields;
    //        this.Where = where;
    //        this.Order = order;
    //        this.Limit = limit;
    //        this.SQL = sql;            
    //    }

    //    /// <summary>
    //    /// 需要取出的字段 （为空时默认*）
    //    /// </summary>
    //    public string Fields { get; set; }

    //    /// <summary>
    //    /// where 条件
    //    /// </summary>
    //    public List<WhereColumn> Where { get; set; }

    //    /// <summary>
    //    /// 排序字段 (不需要传入"order by ")
    //    /// </summary>
    //    public string Order { get; set; }

    //    /// <summary>
    //    /// 返回的数据条数(不需要传入"limit")
    //    /// </summary>
    //    public string Limit { get; set; }

    //    /// <summary>
    //    /// 原始字符串（可选，默认为select #fields from tableName where #where）
    //    /// </summary>
    //    public string SQL { get; set; }
    //}

    //public class WhereColumn
    //{
    //    /// <summary>
    //    /// 传递参数名称
    //    /// </summary>
    //    public string Name { get; set; }
    //    /// <summary>
    //    /// 字段名称(实际列名)
    //    /// </summary>
    //    public string Code { get; set; }

    //    /// <summary>
    //    /// 比较类型
    //    /// </summary>
    //    public CompareType CompareType { get; set; }
    //}

    //public enum CompareType 
    //{ 
    //    /// <summary>
    //    /// 等于 =
    //    /// </summary>
    //    Equal = 1, 
    //    /// <summary>
    //    /// 大于 >
    //    /// </summary>
    //    Greater = 2, 
    //    /// <summary>
    //    /// 小于 <
    //    /// </summary>
    //    Less = 3,
    //    /// <summary>
    //    /// 大于等于 >=
    //    /// </summary>
    //    GreaterEqual = 4,
    //    /// <summary>
    //    /// 小于等于 <=
    //    /// </summary>
    //    LessEqual =5,
    //    /// <summary>
    //    /// 不等于 <>
    //    /// </summary>
    //    NoEqual = 6,
    //    /// <summary>
    //    /// like 
    //    /// </summary>
    //    Like = 10, 
    //    /// <summary>
    //    /// like 'XXX%'
    //    /// </summary>
    //    LikeLeft = 11, 
    //    /// <summary>
    //    /// in
    //    /// </summary>
    //    In = 12
    //}

    //public enum SqlType
    //{
    //    /// <summary>
    //    /// SQL语句名称
    //    /// </summary>
    //    SqlName = 1,
    //    /// <summary>
    //    /// 具体SQL语句
    //    /// </summary>
    //    CmdText = 2
    //}

    /// <summary>
    /// dal 执行对象（用于SQL批量执行）
    /// </summary>
    public class DALObject
    {
        public DALObject()
        {
            this.ExSqlType = DataAccess.Extend.SqlType.SqlName;//默认
        }

        public SqlType ExSqlType { get; set; }

        public string SqlName { get; set; }

        public HashObject Parameters { get; set; }
    }
}

