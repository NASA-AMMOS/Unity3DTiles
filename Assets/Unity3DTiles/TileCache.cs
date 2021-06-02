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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity3DTiles
{
    public class TileCache
    {
        // List looks like this
        // [ unused, sentinel, used ]

        private readonly Unity3DTilesetSceneOptions sceneOptions;

        private readonly LinkedList<Unity3DTile> list = new LinkedList<Unity3DTile>();

        private readonly LinkedListNode<Unity3DTile> sentinel = new LinkedListNode<Unity3DTile>(null);

        private readonly Dictionary<Unity3DTile, LinkedListNode<Unity3DTile>> nodeLookup =
            new Dictionary<Unity3DTile, LinkedListNode<Unity3DTile>>();

        private readonly List<Unity3DTile> tmpList = new List<Unity3DTile>();

        public bool HasMaxSize
        {
            get { return sceneOptions.CacheMaxSize > 0;  }
        }

        public int MaxSize
        {
            get { return sceneOptions.CacheMaxSize;  }
        }
        
        public int TargetSize
        {
            get { return sceneOptions.CacheTargetSize;  }
        }

        public bool Full
        {
            get { return HasMaxSize && Count() >= MaxSize; }
        }

        public int Unused
        {
            get
            {
                int count = 0;
                var node = sentinel;
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
                var node = sentinel;
                while (node.Next != null)
                {
                    count++;
                    node = node.Next;
                }
                return count;
            }
        }

        public TileCache(Unity3DTilesetSceneOptions sceneOptions)
        {
            this.sceneOptions = sceneOptions;
            list.AddFirst(sentinel);
        }

        public int Count(Func<Unity3DTile, bool> predicate = null)
        {
            if (predicate == null)
            {
                return list.Count - 1;
            }
            return list.Count(t => t != null && predicate(t));
        }

        public void ForEach(Action<Unity3DTile> action)
        {
            foreach (var tile in list)
            {
                if (tile != null)
                {
                    action(tile);
                }
            }
        }

        public bool Add(Unity3DTile tile, out bool duplicate)
        {
            duplicate = false;
            if (tile == null)
            {
                return false;
            }
            if (nodeLookup.ContainsKey(tile))
            {
                duplicate = true;
                return false;
            }
            if (Full)
            {
                MarkLowPriorityUnused(t => t.FrameState.Priority > tile.FrameState.Priority, 1);
                if (UnloadUnusedContent(1, 1) == 0)
                {
                    return false;
                }
            }
            var node = new LinkedListNode<Unity3DTile>(tile);
            nodeLookup.Add(tile, node);
            list.AddLast(node);
            return true;
        }

        public bool Remove(Unity3DTile tile)
        {
            if (!nodeLookup.ContainsKey(tile))
            {
                return false;
            }
            var node = nodeLookup[tile];
            nodeLookup.Remove(tile);
            list.Remove(node);
            tile.UnloadContent();
            return true;
        }

        public void MarkUsed(Unity3DTile tile)
        {
            if (nodeLookup.ContainsKey(tile))
            {
                var node = nodeLookup[tile];
                list.Remove(node);
                list.AddLast(node);
            }
        }

        public void MarkUnused(Unity3DTile tile)
        {
            if (nodeLookup.ContainsKey(tile))
            {
                var node = nodeLookup[tile];
                list.Remove(node);
                list.AddFirst(node);
            }
        }

        public int MarkLowPriorityUnused(Func<Unity3DTile, bool> predicate, int max = -1)
        {
            //collect used nodes in tmpList and sort low priority first (higher value = lower priority)
            tmpList.Clear();
            var node = sentinel;
            while (node.Next != null)
            {
                node = node.Next;
                tmpList.Add(node.Value);
            }
            tmpList.Sort((x, y) => (int)Mathf.Sign(y.FrameState.Priority - x.FrameState.Priority));

            //mark up to max unused, from lowest to highest priority, as long as predicate holds
            int num = 0;
            for (int i = 0; i < tmpList.Count && (max < 0 || i < max) && predicate(tmpList[i]); i++)
            {
                MarkUnused(tmpList[i]);
                num++;
            }
            tmpList.Clear();
            return num;
        }

        public void MarkAllUnused()
        {
            list.Remove(sentinel);
            list.AddLast(sentinel);
        }

        public int UnloadUnusedContent(int minNum = 0, int maxNum = -1)
        {
            //collect unused nodes in tmpList and sort low priority first (higher value = lower priority)
            tmpList.Clear();
            var node = list.First;
            while (node != sentinel)
            {
                tmpList.Add(node.Value);
                node = node.Next;
            }
            tmpList.Sort((x, y) => (int)Mathf.Sign(y.FrameState.Priority - x.FrameState.Priority));

            //apply rules to determine how many to unload
            float maxRatio = Mathf.Max(0.01f, Mathf.Min(1, sceneOptions.MaxCacheUnloadRatio));
            int target = TargetSize;
            if (target <= 0 || (MaxSize > 0 && target > MaxSize))
            {
                target = MaxSize;
            }
            if (target <= 0 || target > Count())
            {
                target = Count();
            }
            int num = (int)Mathf.Ceil(target * maxRatio);
            if (minNum > 0 && num < minNum)
            {
                num = minNum;
            }
            if (maxNum > 0 && num > maxNum)
            {
                num = maxNum;
            }
            num = Math.Min(tmpList.Count, num);

            //remove and unload nodes from lowest to highest priority
            for (int i = 0; i < num; i++)
            {
                Remove(tmpList[i]);
            }
            tmpList.Clear();
            return num;
        }
    }
}
