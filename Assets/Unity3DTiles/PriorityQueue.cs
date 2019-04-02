using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity3DTiles
{

    public class PriorityQueueItem<T>
    {
        readonly public T Data;
        readonly public float Priority;
   
        public PriorityQueueItem(T data, float priority)
        {
            this.Data = data;
            this.Priority = priority;
        }

    }

    //https://visualstudiomagazine.com/Articles/2012/11/01/Priority-Queues-with-C.aspx
    public class PriorityQueue<T>
    {
        private List<PriorityQueueItem<T>> data;
        
        public PriorityQueue()
        {
            this.data = new List<PriorityQueueItem<T>>();
        }

        public void Enqueue(PriorityQueueItem<T> item)
        {
            data.Add(item);
            int ci = data.Count - 1; // child index; start at end
            while (ci > 0)
            {
                int pi = (ci - 1) / 2; // parent index
                if (data[ci].Priority - data[pi].Priority >= 0)
                {
                    break; // child item is larger than (or equal) parent so we're done
                }
                PriorityQueueItem<T> tmp = data[ci];
                data[ci] = data[pi];
                data[pi] = tmp;
                ci = pi;
            }
        }

        public PriorityQueueItem<T> Dequeue()
        {
            // assumes pq is not empty; up to calling code
            int li = data.Count - 1; // last index (before removal)
            PriorityQueueItem<T> frontItem = data[0];   // fetch the front
            data[0] = data[li];
            data.RemoveAt(li);

            --li; // last index (after removal)
            int pi = 0; // parent index. start at front of pq
            while (true)
            {
                int ci = pi * 2 + 1; // left child index of parent
                if (ci > li)
                {
                    break;  // no children so done
                }
                int rc = ci + 1;     // right child
                if (rc <= li && data[rc].Priority - data[ci].Priority < 0)
                {
                    // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                    ci = rc;
                }
                if (data[pi].Priority - data[ci].Priority <= 0)
                {
                    break; // parent is smaller than (or equal to) smallest child so done
                }
                PriorityQueueItem<T> tmp = data[pi];// swap parent and child
                data[pi] = data[ci];
                data[ci] = tmp; 
                pi = ci;
            }
            return frontItem;
        }

        public PriorityQueueItem<T> Peek()
        {
            PriorityQueueItem<T> frontItem = data[0];
            return frontItem;
        }

        public int Count()
        {
            return data.Count;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < data.Count; ++i)
            {
                s += data[i].ToString() + " ";
            }
            s += "count = " + data.Count;
            return s;
        }

        public bool IsConsistent()
        {
            // is the heap property true for all data?
            if (data.Count == 0)
            {
                return true;
            }
            int li = data.Count - 1; // last index
            for (int pi = 0; pi < data.Count; ++pi) // each parent index
            {
                int lci = 2 * pi + 1; // left child index
                int rci = 2 * pi + 2; // right child index

                if (lci <= li && data[pi].Priority - data[lci].Priority > 0)
                {
                    return false; // if lc exists and it's greater than parent then bad.
                }
                if (rci <= li && data[pi].Priority - data[rci].Priority > 0)
                {
                    return false; // check the right child too.
                }
            }
            return true; // passed all checks
        } 
    } 
}