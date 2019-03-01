﻿#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DataTableToList
* 项目描述 ：
* 类 名 称 ：DynamicDataTableToList
* 类 描 述 ：
* 命名空间 ：DataTableToList
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion



using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace DataTableToList
{
    /* ============================================================================== 
* 功能描述：EntityConverter Emit转换实体
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/

    public static class DynamicDataTableToList
    {
        public static bool IsCache = true;
        //private static ConcurrentDictionary<string, object> cache = new ConcurrentDictionary<string, object>();
        //数据类型和对应的强制转换方法的methodinfo，供实体属性赋值时调用
        private static Dictionary<Type, MethodInfo> ConvertMethods = new Dictionary<Type, MethodInfo>()
       {
           {typeof(int),typeof(Convert).GetMethod("ToInt32",new Type[]{typeof(object)})},
           {typeof(Int16),typeof(Convert).GetMethod("ToInt16",new Type[]{typeof(object)})},
           {typeof(Int64),typeof(Convert).GetMethod("ToInt64",new Type[]{typeof(object)})},
           {typeof(DateTime),typeof(Convert).GetMethod("ToDateTime",new Type[]{typeof(object)})},
           {typeof(decimal),typeof(Convert).GetMethod("ToDecimal",new Type[]{typeof(object)})},
           {typeof(Double),typeof(Convert).GetMethod("ToDouble",new Type[]{typeof(object)})},
           {typeof(Boolean),typeof(Convert).GetMethod("ToBoolean",new Type[]{typeof(object)})},
           {typeof(string),typeof(Convert).GetMethod("ToString",new Type[]{typeof(object)})}
          
       };

        //把datarow转换为实体的方法的委托定义
        public delegate T LoadDataRow<T>(DataRow dr);
        //把datareader转换为实体的方法的委托定义
        public delegate T LoadDataRecord<T>(IDataRecord dr);

        //把实体转DataRow
        public delegate void EntityToRow<T>(T obj, DataRow row);

        //emit里面用到的针对datarow的元数据信息
        private static readonly AssembleInfo dataRowAssembly = new AssembleInfo(typeof(DataRow));
        //emit里面用到的针对datareader的元数据信息
        private static readonly AssembleInfo dataRecordAssembly = new AssembleInfo(typeof(IDataRecord));

        /// <summary>
        /// 构造转换动态方法（核心代码），根据assembly可处理datarow和datareader两种转换
        /// </summary>
        /// <typeparam name="T">返回的实体类型</typeparam>
        /// <param name="assembly">待转换数据的元数据信息</param>
        /// <returns>实体对象</returns>
        private static DynamicMethod BuildMethod<T>(AssembleInfo assembly, MapColumn[] mapColumns = null)
        {
            DynamicMethod method = new DynamicMethod(assembly.MethodName + typeof(T).Name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T),
                    new Type[] { assembly.SourceType }, typeof(EntityContext).Module, true);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder result = generator.DeclareLocal(typeof(T));
            generator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);
            foreach (var column in  mapColumns)
            {
                PropertyInfo property = column.Property;
                var endIfLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Ldarg_0);
                //第一组，调用AssembleInfo的CanSetted方法，判断是否可以转换
                generator.Emit(OpCodes.Ldstr, column.ColumnName);
                generator.Emit(OpCodes.Call, assembly.CanSettedMethod);
                generator.Emit(OpCodes.Brfalse, endIfLabel);
                generator.Emit(OpCodes.Ldloc, result);
                generator.Emit(OpCodes.Ldarg_0);
            
                //第二组,属性设置
                generator.Emit(OpCodes.Ldstr, column.ColumnName);
                generator.Emit(OpCodes.Call, assembly.GetValueMethod);//获取数据库值
                if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                {
                    var cur = Nullable.GetUnderlyingType(property.PropertyType);
                    generator.Emit(OpCodes.Call, ConvertMethods[cur == null ? property.PropertyType : cur]);//调用强转方法赋值

                }
                //效果类似  Name=Convert.ToString(row["PName"]);
                else
                {
                    generator.Emit(OpCodes.Castclass, property.PropertyType);
                }
                generator.Emit(OpCodes.Call, property.GetSetMethod());//直接给属性赋值
                //效果类似  Name=row["PName"];
                generator.MarkLabel(endIfLabel);
            }
            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);
            return method;
        }

        /// <summary>
        /// 检查列;获取DataTable有列的属性集合
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="Properties">属性集合</param>
        /// <returns>有列的属性集合</returns>
        private static MapColumn[] CheckProperty(DataTable dt, PropertyInfo[] Properties)
        {
            List<MapColumn> lst = new List<MapColumn>(Properties.Length);
            foreach (var property in Properties)
            {
                string colName = property.Name;
                NoColumnAttribute noColumn = property.GetCustomAttribute<NoColumnAttribute>();
                if(noColumn!=null)
                {
                    continue;
                }
                DataFieldAttribute aliasAttr = property.GetCustomAttribute<DataFieldAttribute>();
                if (aliasAttr != null)
                {
                    colName = aliasAttr.ColumnName;
                }
                if (dt.Columns.Contains(colName))
                {
                    MapColumn column = new MapColumn() { ColumnName = colName, Property = property };
                    lst.Add(column);
                }
            }
            return lst.ToArray();

        }

        /// <summary>
        /// 检查可匹配的列
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="Properties"></param>
        /// <returns></returns>
        private static MapColumn[] CheckProperty(IDataReader reader, PropertyInfo[] Properties)
        {
            List<MapColumn> lst = new List<MapColumn>(Properties.Length);
            List<string> lstCol = new List<string>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                lstCol.Add(reader.GetName(i));
            }
            foreach (var property in Properties)
            {
                string colName = property.Name;
                DataFieldAttribute aliasAttr = property.GetCustomAttribute<DataFieldAttribute>();
                if (aliasAttr != null)
                {
                    colName = aliasAttr.ColumnName;
                }
                if (lstCol.Contains(colName))
                {
                    MapColumn column = new MapColumn() { ColumnName = colName, Property = property };
                    lst.Add(column);
                }

            }
            return lst.ToArray();
        }

       
       /// <summary>
       /// 查找DataRow转换
       /// </summary>
       /// <typeparam name="T"></typeparam>
       /// <returns></returns>
        private static LoadDataRow<T> FindDataRowMethod<T>(DataTable dt)
        {
            string key = dt.TableName;
            if (string.IsNullOrEmpty(key))
            {
                key = dt.Columns.Count + "_";
                foreach(DataColumn col in dt.Columns)
                {
                    key = key + col.ColumnName + "_";
                }
               key=key+ dataRowAssembly.MethodName + typeof(T).FullName;
            }
            LoadDataRow<T> load = null;
            object v = null;
            if (ConvertCache<string, object>.Singleton.TryGet(key, out v))
            {
                load = v as LoadDataRow<T>;
            }
            return load;
        }

        /// <summary>
        /// 创建DataRow转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="mapColumns"></param>
        /// <returns></returns>
        private static LoadDataRow<T> CreateDataRowMethod<T>(DataTable dt, MapColumn[] mapColumns)
        {
            string key = null;
            if (dt != null)
            {
                key=dt.TableName;
                if (string.IsNullOrEmpty(key))
                {
                    key = dt.Columns.Count + "_";
                    foreach (DataColumn col in dt.Columns)
                    {
                        key = key + col.ColumnName + "_";
                    }
                    key = key + dataRowAssembly.MethodName + typeof(T).FullName;
                }
            }
            LoadDataRow<T> load = (LoadDataRow<T>)BuildMethod<T>(dataRowAssembly, mapColumns).CreateDelegate(typeof(LoadDataRow<T>));
            if (key != null)
            {
                ConvertCache<string, object>.Singleton.Set(key, load);
            }
            return load;
        }

        /// <summary>
        /// 查找DataRecord转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static LoadDataRecord<T> FindDataRecordMethod<T>(IDataReader reader)
        {
            
                string key = "";
                key = reader.FieldCount + "_";
                for (int i =0;i< reader.FieldCount;i++)
                {
                    key = key + reader.GetName(i) + "_";
                }
                key = key + dataRecordAssembly.MethodName + typeof(T).FullName;
            
            LoadDataRecord<T> load = null;
            object v = null;
            if (ConvertCache<string, object>.Singleton.TryGet(key, out v))
            {
                load = v as LoadDataRecord<T>;
            }
            return load;
        }

        /// <summary>
        /// 创建DataRecord转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="mapColumns"></param>
        /// <returns></returns>
        private static LoadDataRecord<T> CreateDataRecordMethod<T>(IDataReader reader,MapColumn[] mapColumns)
        {
            string key =null;
            if (reader != null)
            {
                key = reader.FieldCount + "_";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    key = key + reader.GetName(i) + "_";
                }
                key = key + dataRecordAssembly.MethodName + typeof(T).FullName;
            }
            LoadDataRecord<T> load = (LoadDataRecord<T>)BuildMethod<T>(dataRecordAssembly, mapColumns).CreateDelegate(typeof(LoadDataRecord<T>));
            if (key != null)
            {
                ConvertCache<string, object>.Singleton.Set(key, load);
            }
            return load;
        }



        /// <summary>
        /// emit 转换实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T>  ToEntityList<T>(this DataTable dt)
        {
            List<T> list = new List<T>();
            if (dt == null || dt.Rows.Count == 0)
            {
                return list;
            }
            LoadDataRow<T> load = null;
            if(IsCache)
            {
                load= FindDataRowMethod<T>(dt);
            }
                
            if(load==null)
            {
                var properties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var mapColumns = CheckProperty(dt, properties);
                if (IsCache)
                {
                    load = CreateDataRowMethod<T>(dt, mapColumns);
                }
                else
                {
                    load = CreateDataRowMethod<T>(null, mapColumns);
                }
            }
            foreach (DataRow dr in dt.Rows)
            {
                list.Add(load(dr));
            }
            return list;
        }

        /// <summary>
        /// emit转换实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static List<T> ToEntityList<T>(this IDataReader dr)
        {
            List<T> list = new List<T>();
            LoadDataRecord<T> load = null;
            if (IsCache)
            {
                load = FindDataRecordMethod<T>(dr);
            }
           
            if(load==null)
            {
                var properties = typeof(T).GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var mapColumns = CheckProperty(dr, properties);
                if (IsCache)
                {
                    load = CreateDataRecordMethod<T>(dr, mapColumns);
                }
                else
                {
                    load = CreateDataRecordMethod<T>(null, mapColumns);
                }
               
            }
            while (dr.Read())
            {
                list.Add(load(dr));
            }
            return list;
        }

    }
}
