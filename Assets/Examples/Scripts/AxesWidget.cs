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
using UnityEngine;
using Unity3DTiles;

public class AxesWidget : MonoBehaviour
{
    public float Scale = 1.0f;
    public enum CoordinateSpace { GLTF, Unity };
    public CoordinateSpace Space = CoordinateSpace.GLTF;

    public void Update()
    {
        var s = Vector3.one * Scale;
        if (Space == CoordinateSpace.GLTF)
        {
            var conv = UnityGLTF.Extensions.SchemaExtensions.CoordinateSpaceConversionScale;
            s.x *= conv.X;
            s.y *= conv.Y;
            s.z *= conv.Z;
        }
        var o = transform.TransformPoint(Vector3.zero);
        var x = transform.TransformPoint(Vector3.Scale(new Vector3(1, 0, 0), s));
        var y = transform.TransformPoint(Vector3.Scale(new Vector3(0, 1, 0), s));
        var z = transform.TransformPoint(Vector3.Scale(new Vector3(0, 0, 1), s));
        Unity3DTilesDebug.DrawLine(o, x, Color.red);
        Unity3DTilesDebug.DrawLine(o, y, Color.green);
        Unity3DTilesDebug.DrawLine(o, z, Color.blue);
    }
}
