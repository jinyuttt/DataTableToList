#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：ListToDataTableCore
* 项目描述 ：
* 类 名 称 ：ConvertCache
* 类 描 述 ：
* 命名空间 ：ListToDataTableCore
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/1 11:04:22
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion



using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ListToDataTableCore
{
    /* ============================================================================== 
* 功能描述：ConvertCache  缓存，可以根据使用频率和最新使用维护
* 创 建 者：jinyu 
* 创建日期：2019
* 更新时间 ：2019
* ==============================================================================*/

    public  class ConvertCache<TKey,TValue>
    {
        public static int MaxSize = 100000;
        public static float Scale = 0.75f;
        
        /// <summary>
        /// 单例
        /// </summary>
        private static readonly Lazy<ConvertCache<TKey, TValue>> instance = new Lazy<ConvertCache<TKey, TValue>>();

        /// <summary>
        /// 缓存池
        /// </summary>
        private readonly Dictionary<TKey, CacheEntity<TValue>> _cache = null;

        /// <summary>
        /// 使用顺序控制
        /// </summary>
        private LinkedList<TKey> _linkedList = null;

        /// <summary>
        /// 保持索引
        /// </summary>
        private Dictionary<TKey, LinkedListNode<TKey>> _dicLink = null;

        /// <summary>
        /// 锁
        /// </summary>
        private ReaderWriterLockSlim lockSlim = null;

        private volatile bool isRun = false;

        /// <summary>
        /// 移除类型
        /// </summary>
        public  CacheType CacheRemove { get; set; }

        /// <summary>
        /// 单例
        /// </summary>
        public static ConvertCache<TKey, TValue> Singleton
        {
            get { return instance.Value; }
        }

        public ConvertCache()
        {
            lockSlim = new ReaderWriterLockSlim();
            _cache = new Dictionary<TKey, CacheEntity<TValue>>();
            _linkedList = new LinkedList<TKey>();
            _dicLink = new Dictionary<TKey, LinkedListNode<TKey>>();
        }

        /// <summary>
        /// 保持缓存,不会精确控制
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        public void Set(TKey key,TValue obj)
        {
            
            try
            {
                lockSlim.EnterWriteLock();
                if (CacheType.LUR == CacheRemove)
                {
                    var item=  _linkedList.AddFirst(key);
                    LinkedListNode<TKey> old = null;
                    if(_dicLink.TryGetValue(key, out old))
                    {
                        _linkedList.Remove(old);
                    }
                    _dicLink[key] = item;
                }
                _cache[key] = new CacheEntity<TValue>(obj);
                if(IsRemove())
                {
                    isRun = true;
                    Trime();
                }
            }
            finally
            {
                
                    lockSlim.ExitWriteLock();
            }
           
        }

        /// <summary>
        /// 获取缓存实体
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="obj">缓存对象</param>
        /// <returns></returns>
        public bool TryGet(TKey key,out TValue obj)
        {
            obj = default(TValue);
            CacheEntity<TValue> v;
            try
            {
                lockSlim.EnterReadLock();
                if (_cache.TryGetValue(key, out v))
                {
                    if (CacheType.LUR == CacheRemove)
                    {
                        _linkedList.AddFirst(key);
                    }
                    obj = v.Entity;
                    return true;
                }
            }
            finally
            {

                lockSlim.ExitReadLock();
            }
            return false;
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _linkedList.Clear();
        }


        /// <summary>
        /// 触发移除
        /// </summary>
        /// <returns></returns>
        private bool IsRemove()
        {
            if((_dicLink.Count>MaxSize||_cache.Count>MaxSize)&&!isRun)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清理缓存，更新数据
        /// </summary>
        private void Trime()
        {
            if(CacheType.Rate==CacheRemove)
            {
                var keys= _cache.Keys.ToList();
                List<CacheEntitySort<TKey>> lst = new List<CacheEntitySort<TKey>>(_cache.Count);
                foreach(var item in keys)
                {
                    CacheEntity<TValue> entity;
                    if(_cache.TryGetValue(item,out entity))
                    {
                        CacheEntitySort<TKey> sort = new CacheEntitySort<TKey>() { key = item, rate = entity.UseRate };
                        lst.Add(sort);
                    }
                }
                //
                lst.Sort((x, y) => { return x.rate.CompareTo(y.rate); });
                int num = (int)(MaxSize * Scale);
                for(int i=0;i<num;i++)
                {
                    _cache.Remove(lst[i].key);
                }

            }
            else
            {
                //直接清理
                int num = (int)(MaxSize * Scale);
                try
                {
                    lockSlim.EnterWriteLock();
                    for(int i=0;i<num;i++)
                    {
                        _dicLink.Remove(_linkedList.Last.Value);
                        _linkedList.RemoveLast();
                    }
                     
                }
                finally
                {

                    lockSlim.ExitWriteLock();
                }
            }
            isRun = false;
        }

    }
}
