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
using UnityEngine;


namespace Unity3DTiles
{
    public class TilesetBehaviour : AbstractTilesetBehaviour
    {
        public Unity3DTilesetOptions TilesetOptions = new Unity3DTilesetOptions();
        public Unity3DTileset Tileset;

        public override bool Ready()
        {
            return Tileset != null && Tileset.Ready;
        }

        public override BoundingSphere BoundingSphere(Func<Unity3DTileset, bool> filter = null)
        {
            if (Tileset == null || (filter != null && !filter(Tileset)))
            {
                return new BoundingSphere(Vector3.zero, 0);
            }
            return Tileset.Root.BoundingVolume.BoundingSphere();
        }

        public override int DeepestDepth()
        {
            return Tileset != null ? Tileset.DeepestDepth : 0;
        }

        public override void ClearForcedTiles()
        {
            if (Tileset != null)
            {
                Tileset.Traversal.ForceTiles.Clear();
            }
        }

        public virtual void MakeTileset()
        {
            Tileset = new Unity3DTileset(TilesetOptions, this);
            Stats = Tileset.Statistics;
        }

        protected override void _start()
        {
            MakeTileset();
        }

        protected override void _lateUpdate()
        {
            if (Tileset != null)
            {
                Tileset.Update();
            }
        }

        protected override void UpdateStats()
        {
            Tileset.UpdateStats();
        }
    }
}
