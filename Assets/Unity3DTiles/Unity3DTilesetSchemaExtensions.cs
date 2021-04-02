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
using UnityGLTF.Extensions;

namespace Unity3DTiles.Schema
{
    public static class Unity3DTilesetSchemaExtensions
    {

        public static bool HasTransform(this Schema.Tile tile)
        {
            if (tile.Transform == null || tile.Transform.Count == 0)
            {
                return false;
            }
            return true;
        }

        public static Matrix4x4 UnityTransform(this Schema.Tile tile)
        {
            if (!tile.HasTransform())
            {
                return Matrix4x4.identity;
            }
            var m = new Matrix4x4();
            m.SetColumn(0, new Vector4((float)tile.Transform[0],
                                       (float)tile.Transform[1],
                                       (float)tile.Transform[2],
                                       (float)tile.Transform[3]));
            m.SetColumn(1, new Vector4((float)tile.Transform[4],
                                       (float)tile.Transform[5],
                                       (float)tile.Transform[6],
                                       (float)tile.Transform[7]));
            m.SetColumn(2, new Vector4((float)tile.Transform[8],
                                       (float)tile.Transform[9],
                                       (float)tile.Transform[10],
                                       (float)tile.Transform[11]));
            m.SetColumn(3, new Vector4((float)tile.Transform[12],
                                       (float)tile.Transform[13],
                                       (float)tile.Transform[14],
                                       (float)tile.Transform[15]));
            return m.UnityMatrix4x4ConvertFromGLTF();
        }

        public static bool IsDefined(this Schema.BoundingVolume volume)
        {
            return volume.Box.Count != 0 || volume.Sphere.Count != 0 || volume.Region.Count != 0;
        }
    }
}
