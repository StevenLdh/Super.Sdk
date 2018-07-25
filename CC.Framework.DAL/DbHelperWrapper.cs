using CC.DataAccess;
using CC.Public;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CC.DataAccess.Extend;

namespace CC.Framework.DAL
{
    //public enum DBrwType { None = 0,Read = 1, Write = 2 };

    /// <summary>
    /// DbHelper 装饰器
    /// </summary>
    internal class DbHelperWrapper : IDisposable
    {
        //private HashObject _parameters;

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DbName { get; set; }
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 参数
        /// </summary>
        private HashObject Parameters { get; set; }
        /// <summary>
        /// 替换参数
        /// </summary>
        private HashObject ReplaceParameters { get; set; }
        //{
        //    get
        //    {
        //        return _parameters;
        //    }
        //    set
        //    {
        //        if (value != null)
        //        {
        //            _parameters = new HashObject();
        //            ReplaceParameters = new HashObject();
        //            foreach (KeyValuePair<string, object> pair in value)
        //            {
        //                if (pair.Key.IndexOf('#') == 0)
        //                {
        //                    ReplaceParameters.Add(pair.Key, pair.Value);
        //                }
        //                else
        //                {
        //                    _parameters.Add(pair.Key, pair.Value);
        //                }
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 参数名称
        /// </summary>
        public string SQLName { get; set; }

        /// <summary>
        /// sql 类型: 1 SQL名称 2具体SQL语句
        /// </summary>
        public SqlType SqlType { get; set; }

        /// <summary>
        /// 具体SQL语句
        /// </summary>
        private string CmdText { get; set; }

        /// <summary>
        /// 几号分区
        /// </summary>
        private long ShardID { get; set; }

        private CommandType CmdType { get; set; }
        private string FunctionName { get; set; }
        private CommonDBLog _log;
        private ConnectionConfig _connConfig;
        private DBrwType _DBrwType;
        /// <summary>
        /// 是否记录日志，默认true
        /// </summary>
        public bool LogEnable { get; set; }




        private void PrepareExecute(string functionName)
        {
            this.FunctionName = functionName;
            this._log = new CommonDBLog();

            ////替换SQL
            //this.ReplaceSQL();
            //设置读写属性
            //this.SetRwType();

            //寻找连接字符串
            this._connConfig = ConnectionManager.GetConnectionString(this.DbName, this._DBrwType, this.ShardID);
            //设置日志
            if (this._connConfig.LogEnable && this.LogEnable)
            {
                HashObject data = new HashObject();
                var uri = "";
                if (HttpContext.Current != null)
                {
                    var reqid = "";
                    if (!HttpContext.Current.Items.Contains("cc_request_id") || HttpContext.Current.Items["cc_request_id"] == null)
                    {
                        HttpContext.Current.Items["cc_request_id"] = reqid = Guid.NewGuid().ToString("N");
                    }
                    else
                    {
                        reqid = HttpContext.Current.Items["cc_request_id"].ToString();
                    }
                    data["RequestId"] = HttpContext.Current.Items["cc_request_id"];
                    uri = HttpContext.Current.Request.Url.ToString();
                }
                else
                {
                    data["RequestId"] = System.Runtime.Remoting.Messaging.CallContext.GetData("cc.mq.flag.callback") as string;
                }
                data["FunctionName"] = functionName;
                data["dbid"] = this._connConfig.DBID;
                data["DBName"] = this._connConfig.DBName;
                data["CmdText"] = this.CmdText;
                data["TableName"] = this.TableName;
                data["SQLName"] = this.SqlType == DataAccess.Extend.SqlType.SqlName ? this.SQLName.ToLower() : string.Empty;


                data["Parameters"] = uri + "\r\n" + (this.Parameters == null ? "" : this.Parameters.ToJsonString());
                data["Success"] = 1;
                data["ErrMessage"] = null;
                data["ErrStackTrace"] = null;
                data["StartTime"] = DateTime.Now;
                if (Parameters != null)
                {
                    data["profileid"] = Parameters.GetValue("profileid", 0);
                }

                this._log.Data = data;
            }
        }

        ///// <summary>
        ///// 替换带#号参数
        ///// </summary>
        //private void ReplaceSQL()
        //{
        //    if (this.ReplaceParameters != null)
        //    {
        //        foreach (KeyValuePair<string, object> pair in this.ReplaceParameters)
        //        {
        //            this.CmdText = this.CmdText.Replace(pair.Key, pair.Value.ToString());
        //        }
        //    }
        //}

        //private void SetRwType()
        //{
        //    switch (FunctionName)
        //    {
        //        case "ExecuteNonQuery":
        //        case "ExecuteScalarWrite":
        //            this._DBrwType = DBrwType.Write;
        //            break;
        //        case "ExecuteDataSet":
        //        case "ExecuteScalarRead":
        //        case "Exists":
        //        case "GetData":
        //        case "GetDataList":
        //        case "GetDicListByGroup":
        //            this._DBrwType = DBrwType.Read;
        //            break;
        //        default:
        //            throw new Exception("读写类型未设置");
        //            break;
        //    }
        //}

        /// <summary>
        /// dbhelper 包装器
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="tableName">表名称</param>
        /// <param name="sqlName">sql名称(不存在时传空)</param>
        /// <param name="parameters">参数(默认null)</param>
        /// <param name="cmdType">执行类型(默认text)</param>
        public DbHelperWrapper(DBrwType dbrwType, string dbName, string tableName, string sqlName, HashObject parameters = null, SqlType sqlType = SqlType.SqlName, CommandType cmdType = CommandType.Text)
        {
            this.DbName = dbName;
            this.TableName = tableName;
            this.SqlType = sqlType;
            this.SQLName = sqlName;
            this.CmdType = cmdType;
            this.LogEnable = true;//默认开启日志记录

            this.ReplaceParameters = new HashObject();

            //拷贝参数
            this.Parameters = new HashObject();
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    if (pair.Key.IndexOf('#') == 0)//替换参数
                    {
                        this.ReplaceParameters.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        this.Parameters.Add(pair.Key, pair.Value);
                    }
                }
            }
            //获取SQL,补充默认参数
            if (this.SqlType == SqlType.SqlName)
            {
                SqlObj sqlobj = SchemaManager.GetDefaultSQL(this.DbName, this.TableName, SQLName);
                this.CmdText = sqlobj.SqlText;
                if (sqlobj.DBrwType != DBrwType.None)//检查SQL上是否包含设置
                {
                    this._DBrwType = sqlobj.DBrwType;
                }
                else
                {
                    this._DBrwType = dbrwType;//否则取dalbase 传递过来的读写类型
                }

                foreach (KeyValuePair<string, object> pair in sqlobj.DefaultParameters)
                {
                    if (!this.ReplaceParameters.ContainsKey(pair.Key))
                        this.ReplaceParameters.Add(pair.Key, pair.Value);
                }

            }
            else
            {
                this.CmdText = SQLName;
                this._DBrwType = dbrwType;//取dalbase 传递过来的读写类型
            }

            //#替换SQL
            foreach (KeyValuePair<string, object> pair in this.ReplaceParameters)
            {
                this.CmdText = this.CmdText.Replace(pair.Key, pair.Value.ToString());
            }

            //计算分区
            this.ShardID = ShardManager.GetShardID(this.DbName, this.Parameters);
        }

        #region ExecuteNonQuery
        public int ExecuteNonQuery()
        {
            int result = 0;
            try
            {
                this.PrepareExecute("ExecuteNonQuery");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.ExecuteNonQuery(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }

            return result;
        }
        #endregion

        #region ExecuteDataSet
        public DataSet ExecuteDataSet()
        {
            DataSet result = null;

            try
            {
                this.PrepareExecute("ExecuteDataSet");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.ExecuteDataSet(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }

            return result;
        }
        #endregion

        #region  ExecuteScalarWrite  ExecuteScalarRead
        /// <summary>
        /// 在写入环境执行ExecuteScalar
        /// </summary>
        /// <returns></returns>
        public object ExecuteScalarWrite()
        {
            object result = null;

            try
            {
                this.PrepareExecute("ExecuteScalarWrite");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.ExecuteScalar(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }

            return result;
        }

        public object ExecuteScalarRead()
        {
            object result = null;

            try
            {
                this.PrepareExecute("ExecuteScalarRead");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.ExecuteScalar(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }

            return result;
        }
        #endregion

        #region Exists GetData GetDataList GetDicListByGroup
        public bool Exists()
        {
            bool result = false;

            try
            {
                this.PrepareExecute("Exists");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.Exists(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }
            return result;
        }

        public HashObject GetData()
        {
            HashObject result = null;

            try
            {
                this.PrepareExecute("GetData");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.GetData(this.CmdText, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }
            return result;
        }

        public HashObjectList GetDataList(int count)
        {
            HashObjectList result = null;

            try
            {
                this.PrepareExecute("GetDataList");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.GetDataList(this.CmdText, this.Parameters, count, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }
            return result;
        }

        public Dictionary<string, HashObjectList> GetDicListByGroup(string[] groupColName)
        {
            Dictionary<string, HashObjectList> result = null;

            try
            {
                this.PrepareExecute("GetDicListByGroup");
                DbHelper dbHelper = DbHelper.Create(this._connConfig.DBType, this._connConfig.ConnectionString);
                result = dbHelper.GetDicListByGroup(this.CmdText, groupColName, this.Parameters, this.CmdType);
            }
            catch (Exception e)
            {
                this._log.Fail(e);
            }
            return result;
        }
        #endregion

        public void Dispose()
        {
            //throw new NotImplementedException();
            _log.Dispose();
        }
    }
}
