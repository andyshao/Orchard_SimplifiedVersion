using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PTG.Common.Services
{
    /// <summary>
    /// DataTable转换为List&lt;Model&gt;
    /// </summary>
    public static partial class DataTableToListModel<T> where T : new()
    {
        public static IList<T> ConvertToModel(DataTable dt)
        {
            //定义集合
            IList<T> ts = new List<T>();
            T t = new T();
            string tempName = "";
            //获取此模型的公共属性
            PropertyInfo[] propertys = t.GetType().GetProperties();
            foreach (DataRow row in dt.Rows)
            {
                t = new T();
                foreach (PropertyInfo pi in propertys)
                {
                    tempName = pi.Name;
                    //检查DataTable是否包含此列
                    if (dt.Columns.Contains(tempName))
                    {
                        //判断此属性是否有set
                        if (!pi.CanWrite)
                            continue;
                        object value = row[tempName];
                        if (value != DBNull.Value)
                            pi.SetValue(t, value, null);
                    }
                }
                ts.Add(t);
            }
            return ts;
        }
        /// <summary>
        /// 将DataRow读取到的一行 转为 Model
        /// </summary>
        /// <param name="dr">DataRow</param>
        /// <returns>泛型实体</returns>
        public static T ToModel(DataRow dr)
        {
            // 获得此模型的类型
            Type type = typeof(T);
            string tempName = "";
            T t = new T();
            // 获得此模型的公共属性
            PropertyInfo[] propertys = t.GetType().GetProperties();
            DataTable dt = dr.Table;
            foreach (PropertyInfo pi in propertys)
            {
                tempName = pi.Name;
                if (dt.Columns.Contains(tempName))
                {
                    // 判断此属性是否有Setter
                    if (!pi.CanWrite)
                        continue;
                    object value = dr[tempName];
                    if (value != DBNull.Value)
                    {
                        if (pi.PropertyType.IsEnum)
                        {
                            pi.SetValue(t, Enum.Parse(pi.PropertyType, value.ToString().Trim(), true), null);
                        }
                        else if (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                        {
                            pi.SetValue(t, Convert.ChangeType(value, System.Nullable.GetUnderlyingType(pi.PropertyType)), null);
                        }
                        else
                        {
                            pi.SetValue(t, Convert.ChangeType(value, pi.PropertyType), null);
                        }
                    }
                }
            }
            return t;
        }
    }
}
