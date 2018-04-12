using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace NZC.Common.Service
{

    /// <summary>
    /// 转换辅助类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class ConvertHelper<T> where T : new()
    {
        /// <summary>
        /// DataTable-->List
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <returns></returns>
        public static IList<T> DataTableConvertToList(DataTable dt)
        {
            IList<T> ts = new List<T>();

            // 取得泛型的类型
            Type type = typeof(T);

            // 创建类型的对象（用于比较用）
            //object convertObj = Activator.CreateInstance(type, null);

            // 反射取得类型实例的属性数组
            //PropertyInfo[] propertys = convertObj.GetType().GetProperties();
            PropertyInfo[] propertys = type.GetProperties();

            // 反射取得类型实例的字段数组
            FieldInfo[] fields = type.GetFields();

            foreach (DataRow dr in dt.Rows)
            {
                // 创建类型的对象（用于赋值用）
                //object outputObj = Activator.CreateInstance(type, null);
                T outputObj = new T();

                // 遍历字段（公共字段）
                foreach (FieldInfo fi in fields)
                {
                    // 如果DataTable的数据列中包含有对应的字段
                    if (dt.Columns.Contains(fi.Name))
                    {
                        // 取得字段的值
                        object value = dr[fi.Name];

                        if (value != DBNull.Value)
                        {
                            // 将对应字段的值赋给创建的类型实例的对应的字段
                            fi.SetValue(outputObj, value);
                        }
                    }
                }

                // 遍历属性
                foreach (PropertyInfo pi in propertys)
                {
                    // 如果DataTable的数据列中包含有对应的属性
                    if (dt.Columns.Contains(pi.Name))
                    {
                        if (!pi.CanWrite)
                        {
                            continue;
                        }

                        // 取得属性的值
                        object value = dr[pi.Name];

                        if (value != DBNull.Value)
                        {
                            // 将对应属性的值赋给创建的类型实例的对应的属性
                            pi.SetValue(outputObj, value, null);
                        }
                    }
                }

                // 添加到List中
                ts.Add((T)outputObj);
            }

            return ts;
        }


        /// <summary>
        /// DataTable-->Json
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <returns></returns>
        public static string DataTableConvertToJson(DataTable dt)
        {
            StringBuilder jsonBuilder = new StringBuilder();

            // 拼接JSON串
            jsonBuilder.Append("{\"");
            jsonBuilder.Append(dt.TableName);
            jsonBuilder.Append("\":[");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                jsonBuilder.Append("{");
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    jsonBuilder.Append("\"");
                    jsonBuilder.Append(dt.Columns[j].ColumnName);
                    jsonBuilder.Append("\":\"");
                    jsonBuilder.Append(dt.Rows[i][j].ToString());
                    jsonBuilder.Append("\",");
                }
                jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
                jsonBuilder.Append("},");
            }
            jsonBuilder.Remove(jsonBuilder.Length - 1, 1);
            jsonBuilder.Append("]");
            jsonBuilder.Append("}");

            return jsonBuilder.ToString();
        }

        /// <summary>
        /// DataSet-->Json
        /// </summary>
        /// <param name="ds">DataSet</param>
        /// <returns></returns>
        public static string DataSetConvertToJson(DataSet ds)
        {
            StringBuilder json = new StringBuilder();

            foreach (DataTable dt in ds.Tables)
            {
                // 拼接JSON串
                json.Append("{\"");
                json.Append(dt.TableName);
                json.Append("\":");
                json.Append(DataTableConvertToJson(dt));
                json.Append("}");
            }

            return json.ToString();
        }
    }
}