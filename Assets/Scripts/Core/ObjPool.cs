
using System.Collections.Generic;
using UnityEngine;

namespace SW.Core
{
    public class ObjPool<T>
    {
        protected Dictionary<int, Queue<T>> _dic = new();

        protected virtual Queue<T> AcessQueue(int itemID)
        {
            if (!_dic.ContainsKey(itemID)) _dic[itemID] = new Queue<T>();
            return _dic[itemID];
        }

        public virtual bool TryEnqueue(int itemID, T obj)
        {
            var q = AcessQueue(itemID);
            if (q != null)
            {
                q.Enqueue(obj);
                return true;
            }

            return false;
        }

        public virtual T TryDequeue(int itemID)
        {
            var q = AcessQueue(itemID);
            if (q.Count > 0) return q.Dequeue();
            return default;
        }

        public virtual bool HasObj(int itemID)
        {
            return ObjCount(itemID) > 0;
        }

        public virtual bool ClearQueue(int itemID)
        {
            if (!_dic.ContainsKey(itemID)) return true;
            _dic[itemID].Clear();
            return true;
        }

        public virtual int ObjCount(int itemID)
        {
            var q = AcessQueue(itemID);
            return q.Count;
        }
    }


}