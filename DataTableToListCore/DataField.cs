#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：ListToDataTableCore
* 项目描述 ：
* 类 名 称 ：DataField
* 类 描 述 ：
* 命名空间 ：ListToDataTableCore
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/1 2:03:58
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

namespace ListToDataTableCore
{
    /* ============================================================================== 
* 功能描述：DataField  列名称映射
* 创 建 者：jinyu 
* 创建日期：2019
* 更新时间 ：2019
* ==============================================================================*/

   public class DataFieldAttribute : Attribute
    {
        public string ColumnName { get; set; }

        public DataFieldAttribute(string name)
        {
            this.ColumnName = name;
        }
    }
}
