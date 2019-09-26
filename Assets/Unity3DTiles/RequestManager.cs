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
using RSG;

namespace Unity3DTiles
{

    public class Request : PriorityQueueItem<Unity3DTile>
    {
        readonly public Promise Started;
        readonly public Promise<bool> Finished;

        public Request(Unity3DTile tile, float priority, Promise started, Promise<bool> finished) : base(tile, priority)
        {
            this.Started = started;
            this.Finished = finished;
        }
    }

    public class RequestManager
    {
        int maxConcurrentRequests;
        int currentRequests;

        PriorityQueue<Unity3DTile> priorityQueue;

        const int MAX_QUEUE_SIZE = 100;

        public RequestManager(int maxConcurrentRequests)
        {
            this.currentRequests = 0;
            this.maxConcurrentRequests = maxConcurrentRequests;
            this.priorityQueue = new PriorityQueue<Unity3DTile>();
        }

        public int RequestsInProgress()
        {
            return currentRequests;
        }

        public int QueueSize()
        {
            return this.priorityQueue.Count();
        }

        public bool Full()
        {
            return this.priorityQueue.Count() >= MAX_QUEUE_SIZE;
        }

        public void EnqueRequest(Request request)
        {
            priorityQueue.Enqueue(request);
        }

        public void Process()
        {
            while(this.currentRequests < this.maxConcurrentRequests && this.priorityQueue.Count() > 0)
            {
                var curItem = (Request) this.priorityQueue.Dequeue();
                this.currentRequests++;
                curItem.Finished.Then((success) =>
                {
                    this.currentRequests--;
                });
                curItem.Started.Resolve();
            }
        }


    }
}
