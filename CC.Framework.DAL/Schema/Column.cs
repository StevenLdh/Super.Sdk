using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CC.Framework.DAL
{
    public class Column
    {
        //<COLUMN Name ="tid" type="bigint" Default="" IsPrimary="true" />


        public string ColumnName { get; set; }
        public string Type { get; set; }
        public string Default { get; set; }
        public bool IsPrimary { get; set; }

        public Column()
        {
        }

        public object GetDefaultValue()
        {
            try
            {
                switch (Type)
                {
                    //数字类型
                    case "tinyint":
                    case "smallint":
                    case "mediumint":
                    case "int":
                    case "bigint":
                    case "bool":
                    case "boolean":
                        return string.IsNullOrEmpty(Default) ? 0 : Convert.ToInt32(Default);
                    case "char":
                    case "varchar":
                    case "blob":
                    case "text":
                    case "binary":
                        return Default;
                    case "datetime":
                    case "date":
                    case "time":
                        return CC.Public.CCDate.MinValue;
                    case "decimal":
                    case "numeric":
                    case "double":
                    case "float":
                        return string.IsNullOrEmpty(Default) ? 0 : Convert.ToDouble(Default);

                    default:
                        throw new Exception(string.Format("暂不支持此类型,column:{0},type:{1}", ColumnName, Type));
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("获取默认值错误，字段[{0}],类型[{1}]", ColumnName, Type), e);
            }
        }
    }
}
