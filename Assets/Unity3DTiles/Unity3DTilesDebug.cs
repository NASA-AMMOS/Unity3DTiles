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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity3DTiles
{
    public class Unity3DTilesDebug
    {
        public static int Layer = 5;

        public static GameObject go;

        public static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            //UnityEngine.Debug.DrawLine(start, end, color); //works only in editor with gizmos enabled
            GetComponent().DrawLine(start, end, color);
        }

        private static DebugDrawBehaviour GetComponent()
        {
            if (go == null)
            {
                go = new GameObject("DebugDraw");
                go.AddComponent<DebugDrawBehaviour>();
            }
            return go.GetComponent<DebugDrawBehaviour>();
        }
    }

    public class DebugDrawBehaviour : MonoBehaviour
    {
        private struct Line
        {
            public Vector3 start;
            public Vector3 end;
            public Color color;
        }
        private List<Line> lines = new List<Line>();
        private Mesh lineMesh;
        private List<Vector3> lineVerts = new List<Vector3>();
        private List<Color> lineColors = new List<Color>();
        private List<int> lineIndices = new List<int>();

        public void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            lines.Add(new Line() { start = start, end = end, color = color });
        }

        public void Awake()
        {
            lineMesh = new Mesh();
            lineMesh.indexFormat = IndexFormat.UInt32;
            lineMesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = lineMesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial =
                new Material(Shader.Find("Hidden/Internal-Colored"));
        }

		public void LateUpdate()
		{
            if (lines.Count > 0)
            {
                lineVerts.Clear();
                lineColors.Clear();
                lineIndices.Clear();

                foreach (var line in lines)
                {
                    lineVerts.Add(line.start);
                    lineColors.Add(line.color);
                    lineIndices.Add(lineVerts.Count - 1);

                    lineVerts.Add(line.end);
                    lineColors.Add(line.color);
                    lineIndices.Add(lineVerts.Count - 1);
                }
                lines.Clear();

                lineMesh.Clear();

				lineMesh.SetVertices(lineVerts);
				lineMesh.SetColors(lineColors);

#if UNITY_2019_3_OR_NEWER                
				lineMesh.SetIndices(lineIndices, MeshTopology.Lines, submesh: 0);
#else
				lineMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, submesh: 0);
#endif
            }
            else
            {
                lineMesh.Clear();
            }
        }

		public void OnDestroy()
		{
            Unity3DTilesDebug.go = null;
		}
    }
}
