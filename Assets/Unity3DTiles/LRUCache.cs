/*
 * Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons.
 */
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity3DTiles
{
    public enum CacheRequestStatus
    {
        ADDED,
        DUPLICATE,
        FULL
    }

    /// <summary>
    /// Represents a least recently used (LRU) cache
    /// </summary>
    public class LRUCache<T> where T : class
    {
        public int MaxSize = -1; //unbounded

        // List looks like this
        // [ unused, sentinal, used ]

        LinkedList<T> list = new LinkedList<T>();
        LinkedListNode<T> sentinal = new LinkedListNode<T>(null);

        Dictionary<T, LinkedListNode<T>> nodeLookup = new Dictionary<T, LinkedListNode<T>>();

        /// <summary>
        /// Number of items in cache O(1)
        /// </summary>
        public int Count
        {
            get { return list.Count - 1; }
        }

        public bool HasMaxSize
        {
            get { return MaxSize > 0;  }
        }

        public bool Full
        {
            get { return HasMaxSize && Count >= MaxSize; }
        }

        /// <summary>
        /// Number of unused items in the cache O(N) where N is number of unused items
        /// </summary>
        public int Unused
        {
            get
            {
                int count = 0;
                LinkedListNode<T> node = this.sentinal;
                while (node.Previous != null)
                {
                    count++;
                    node = node.Previous;
                }
                return count;
            }
        }

        public int Used
        {
            get
            {
                int count = 0;
                LinkedListNode<T> node = this.sentinal;
                while (node.Next != null)
                {
                    count++;
                    node = node.Next;
                }
                return count;
            }
        }

        public LRUCache()
        {
            list.AddFirst(sentinal);
        }

        /// <summary>
        /// Adds a new element to the replacement list and marks it as used.  Returns the node for that element
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public CacheRequestStatus Add(T element)
        {
            if (nodeLookup.ContainsKey(element))
            {
                return CacheRequestStatus.DUPLICATE;
            }
            if (this.Full)
            {
                return CacheRequestStatus.FULL;
            }
            LinkedListNode<T> node = new LinkedListNode<T>(element);
            nodeLookup.Add(element, node);
            list.AddLast(node);
            return CacheRequestStatus.ADDED;
        }

        /// <summary>
        /// Marks this node as used
        /// </summary>
        /// <param name="node"></param>
        public void MarkUsed(T element)
        {
            if (nodeLookup.ContainsKey(element))
            {
                var node = nodeLookup[element];
                this.list.Remove(node);
                this.list.AddLast(node);
            }
        }

        /// <summary>
        /// Marks all nodes as unused
        /// </summary>
        public void MarkAllUnused()
        {
            this.list.Remove(sentinal);
            this.list.AddLast(sentinal);
        }

        /// <summary>
        /// Removes the least recently used element and returns it
        /// </summary>
        /// <returns></returns>
        public T RemoveLeastRecentlyUsed()
        {
            if (this.list.First == this.sentinal)
            {
                return null;
            }
            T element = this.list.First.Value;
            this.list.RemoveFirst();
            nodeLookup.Remove(element);
            return element;
        }

        /// <summary>
        /// Removes all unused elements and returns them in order of least recently used to most recently used
        /// </summary>
        /// <returns></returns>
        public List<T> RemoveUnused()
        {
            List<T> list = new List<T>();
            while (this.list.First != this.sentinal)
            {
                T element = this.list.First.Value;
                nodeLookup.Remove(element);
                this.list.RemoveFirst();
                list.Add(element);
            }
            return list;
        }

        AsyncOperation lastUnloadAssets = null;

        /// <summary>
        /// Unloads content from unused nodes
        /// </summary>
        public void UnloadUnusedContent(int targetSize, float unloadPercent, System.Func<T, float> Priority, System.Action<T> OnRemove)
        {
            if (this.Count > targetSize && this.Unused > 0)
            {
                List<T> unused = this.GetUnused();
                var sortedUnused = unused.OrderBy(node => Priority(node)).ToArray();
                int nodesToUnload = (int)(targetSize * unloadPercent);
                nodesToUnload = System.Math.Min(sortedUnused.Length, nodesToUnload);
                for (int i = 0; i < nodesToUnload; i++)
                {
                    Remove(sortedUnused[i]);
                    OnRemove?.Invoke(sortedUnused[i]);
                }
                if (lastUnloadAssets == null || lastUnloadAssets.isDone)
                {
                    lastUnloadAssets = Resources.UnloadUnusedAssets();
                } //TODO: schedule unload instead of skip
            }
        }

        /// <summary>
        /// Returns a list of unused nodes but does not remove them
        /// </summary>
        /// <returns></returns>
        public List<T> GetUnused()
        {
            List<T> result = new List<T>();
            LinkedListNode<T> curNode = this.list.First;
            while (curNode != this.sentinal)
            {
                result.Add(curNode.Value);
                curNode = curNode.Next;
            }
            return result;
        }

        /// <summary>
        /// Remove a specific node regardless of its state (used or unused) 
        /// </summary>
        /// <param name="node"></param>
        public void Remove(T element)
        {
            if (nodeLookup.ContainsKey(element))
            {
                var node = nodeLookup[element];
                nodeLookup.Remove(element);
                this.list.Remove(node);
            }
        }
    }
}
