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
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using RSG;

namespace Unity3DTiles
{
    public class Request : PriorityQueueItem<Unity3DTile>
    {
        readonly public Promise Started;
        readonly public Promise<bool> Finished;

        public Request(Unity3DTile tile, float priority, Promise started, Promise<bool> finished) : base(tile, priority)
        {
            Started = started;
            Finished = finished;
        }
    }

    public class RequestManager
    {
        private Unity3DTilesetSceneOptions sceneOptions;

        private int currentRequests;

        private PriorityQueue<Unity3DTile> priorityQueue = new PriorityQueue<Unity3DTile>();

        private float lastGC = -1;
        private bool paused;

        private const int MAX_QUEUE_SIZE = 100;
        private const int MIN_GC_PERIOD_SEC = 2;

        private long _usedMem = -1;
        public long UsedMem
        {
            get
            {
                return _usedMem > 0 ? _usedMem : Profiler.usedHeapSizeLong;
            }
            private set
            {
                _usedMem = value;
            }
        }

        private long _totalMem = -1;
        public long TotalMem
        {
            get
            {
                return _totalMem > 0 ? _totalMem : Profiler.GetTotalAllocatedMemoryLong();
            }
            private set
            {
                _totalMem = value;
            }
        }

        private string _memKind;
        public string MemKind
        {
            get
            {
                return _memKind != null ? _memKind : "heap";
            }
            private set
            {
                _memKind = value;
            }
        }

        public string GetStatus()
        {
            var sb = new StringBuilder();
            sb.Append($"{MemKind} memory: {FmtKMG(UsedMem)} used, {FmtKMG(TotalMem)} total");
            if (MemKind != "heap")
            {
                sb.Append($"\nheap memory: {FmtKMG(Profiler.usedHeapSizeLong)} used, " +
                          $"{FmtKMG(Profiler.GetTotalAllocatedMemoryLong())} total");
            }
            //sb.Append($"\npause mem threshold: {FmtKMG(sceneOptions.PauseMemThreshold)}");
            //sb.Append($"\nGC mem threshold: {FmtKMG(sceneOptions.GCMemThreshold)}");
            sb.Append($"\ntile requests: {currentRequests}/{sceneOptions.MaxConcurrentRequests} active, ");
            sb.Append($"{priorityQueue.Count(),3}/{MAX_QUEUE_SIZE} queued" + (paused ? " (paused)" : ""));
            return sb.ToString();
        }

        public RequestManager(Unity3DTilesetSceneOptions sceneOptions)
        {
            this.sceneOptions = sceneOptions;
        }

        public int RequestsInProgress()
        {
            return currentRequests;
        }

        public int QueueSize()
        {
            return priorityQueue.Count();
        }

        public bool Full()
        {
            return priorityQueue.Count() >= MAX_QUEUE_SIZE;
        }

        public void EnqueRequest(Request request)
        {
            priorityQueue.Enqueue(request);
        }

        public void Process()
        {
            if (sceneOptions.GCMemThreshold > 0 && UsedMem > sceneOptions.GCMemThreshold &&
                (lastGC < 0 || (Time.realtimeSinceStartup - lastGC) > MIN_GC_PERIOD_SEC)) {
                Debug.Log($"requesting GC, memory usage {FmtKMG(UsedMem)} > {FmtKMG(sceneOptions.GCMemThreshold)}");
                GC();
                lastGC = Time.realtimeSinceStartup;
            }
            if (sceneOptions.PauseMemThreshold > 0 && UsedMem > sceneOptions.PauseMemThreshold) {
                paused = true;
                return;
            }
            paused = false;
            while(currentRequests < sceneOptions.MaxConcurrentRequests && priorityQueue.Count() > 0)
            {
                var curItem = (Request) priorityQueue.Dequeue();
                currentRequests++;
                curItem.Finished.Then((success) =>
                {
                    currentRequests--;
                });
                curItem.Started.Resolve();
            }
        }

        public void SetMemoryUsage(long usedMem, long totalMem, string kind)
        {
            UsedMem = usedMem;
            TotalMem = totalMem;
            MemKind = kind;
        }

        public static void GC(bool heavy = true)
        {
            if (heavy)
            {
#if NET_4_6
                System.Runtime.GCSettings.LargeObjectHeapCompactionMode =
                    System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
#endif
                int maxGen = System.GC.MaxGeneration;
                //int maxGen = 3;
#if NET_4_6 && !NETFX_CORE
                System.GC.Collect(maxGen, System.GCCollectionMode.Forced, true, true);
#else
                System.GC.Collect(maxGen, System.GCCollectionMode.Forced);
#endif
            }
            else
            {
                // the docs say this defaults to a blocking collect of all generations but does not mention compaction
                // this does not include the LOH unless you set LargeObjectHeapCompactionMode as above
                System.GC.Collect();
            }
        }

        public static string FmtKMG(float b, float k = 1e3f)
        {
            if (Mathf.Abs(b) < k) return b.ToString("f0");
            else if (Mathf.Abs(b) < k*k) return string.Format("{0:f1}k", b/k);
            else if (Mathf.Abs(b) < k*k*k) return string.Format("{0:f2}M", b/(k*k));
            else return string.Format("{0:f3}G", b/(k*k*k));
        }
    }
}
