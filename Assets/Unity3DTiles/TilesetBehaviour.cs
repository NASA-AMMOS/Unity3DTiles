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


namespace Unity3DTiles
{

    public class TilesetBehaviour : MonoBehaviour
    {
        public Unity3DTilesetOptions TilesetOptions = new Unity3DTilesetOptions();
        public Unity3DTileset Tileset;
        public Unity3DTilesetStatistics Stats;

        public int MaxConcurrentRequests = 6;

        public virtual void Start()
        {
            MakeTileset();
        }

        public virtual void LateUpdate()
        {
            if (Tileset != null)
            {
                Tileset.Update();
            }
        }

        protected virtual void MakeTileset()
        {
            RequestManager rm = new RequestManager(MaxConcurrentRequests);
            Tileset = new Unity3DTileset(TilesetOptions, this, rm);
            Stats = Tileset.Statistics;
        }
    }
}