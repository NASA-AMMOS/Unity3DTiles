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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using RSG;

namespace Unity3DTiles
{
    public class Request
    {
        readonly public Unity3DTile Tile;
        readonly public Promise Started;
        readonly public Promise<bool> Finished;

        public Request(Unity3DTile tile, Promise started, Promise<bool> finished)
        {
            Tile = tile;
            Started = started;
            Finished = finished;
        }
    }

    public class RequestManager : IEnumerable<Unity3DTile>
    {
        private readonly Unity3DTilesetSceneOptions sceneOptions;

        private Queue<Request> queue = new Queue<Request>();
        private HashSet<Unity3DTile> activeDownloads = new HashSet<Unity3DTile>();
        private List<Request> tmpList = new List<Request>();
        private HashSet<Unity3DTile> tmpSet = new HashSet<Unity3DTile>();

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

        public IEnumerator<Unity3DTile> GetEnumerator()
        {
            foreach (var request in queue)
            {
                yield return request.Tile;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
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
            sb.Append($"\ntile requests: {activeDownloads.Count}/{sceneOptions.MaxConcurrentRequests} active, ");
            sb.Append(queue.Count.ToString("d3") + $"/{MAX_QUEUE_SIZE} queued" + (paused ? " (paused)" : ""));
            return sb.ToString();
        }

        public RequestManager(Unity3DTilesetSceneOptions sceneOptions)
        {
            this.sceneOptions = sceneOptions;
        }

        public int Count(Func<Unity3DTile, bool> predicate = null)
        {
            if (predicate == null)
            {
                return queue.Count;
            } 
            int num = 0;
            foreach (var request in queue)
            {
                if (predicate(request.Tile))
                {
                    num++;
                }
            }
            return num;
        }

        public int CountActiveDownloads(Func<Unity3DTile, bool> predicate = null)
        {
            if (predicate == null)
            {
                return activeDownloads.Count;
            }
            return activeDownloads.Count(predicate);
        }

        public void ForEachQueuedDownload(Action<Unity3DTile> action)
        {
            foreach (var request in queue)
            {
                action(request.Tile);
            }
        }

        public void ForEachActiveDownload(Action<Unity3DTile> action)
        {
            foreach (var tile in activeDownloads)
            {
                action(tile);
            }
        }

        public void EnqueRequest(Request request)
        {
            request.Tile.ContentState = Unity3DTileContentState.LOADING;
            queue.Enqueue(request);
        }

        public void Process(int maxNewRequests)
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

            //re-sort queue from high to low priority (high priority = low value)
            //because priorities may change from frame to frame
            //also cull any duplicates and drop any requests that exceed MAX_QUEUE_SIZE
            tmpList.Clear();
            tmpSet.Clear();
            tmpList.AddRange(queue);
            queue.Clear();
            tmpList.Sort((x, y) => (int)Mathf.Sign(x.Tile.FrameState.Priority - y.Tile.FrameState.Priority));
            tmpSet.Clear();
            for (int i = 0; i < tmpList.Count; i++)
            {
                if (queue.Count < MAX_QUEUE_SIZE)
                {
                    if (!tmpSet.Contains(tmpList[i].Tile))
                    {
                        queue.Enqueue(tmpList[i]);
                        tmpSet.Add(tmpList[i].Tile);
                    }
                }
                else
                {
                    tmpList[i].Tile.ContentState = Unity3DTileContentState.UNLOADED;
                }
            }
            tmpList.Clear();
            tmpSet.Clear();

            int newRequests = 0;
            while (activeDownloads.Count < sceneOptions.MaxConcurrentRequests && queue.Count > 0 &&
                   (maxNewRequests < 0 || newRequests < maxNewRequests))
            {
                var request = queue.Dequeue();
                activeDownloads.Add(request.Tile);
                newRequests++;
                request.Finished.Then((success) => { activeDownloads.Remove(request.Tile); });
                request.Started.Resolve();
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
