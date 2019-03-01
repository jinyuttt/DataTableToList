﻿#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DataTableToList
* 项目描述 ：
* 类 名 称 ：ColumnType
* 类 描 述 ：
* 命名空间 ：DataTableToList
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/1 2:13:50
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableToList
{
    /* ============================================================================== 
* 功能描述：ColumnType  列类型映射
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/

   public class ColumnTypeAttribute : Attribute
    {
        public Type ColumnType { get; set; }
        public ColumnTypeAttribute(Type type)
        {
            this.ColumnType = type;
        }
    }
}
