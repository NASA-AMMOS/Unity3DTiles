/*
 * "Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons."
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity3DTiles
{

    public enum IntersectionType
    {
        OUTSIDE,
        INSIDE,
        INTERSECTING,
    }


    /// <summary>
    /// This strucutre is used as an optimization when culling a bounding volume with a view frustum
    /// If a bounding volume is enclosed within a parent volume, then we only need to check planes 
    /// that intersected the parent volume.  We can skip a plane if the volume is completly inside.
    /// We also track if the volume is completly outside any of the planes, in which case we know the
    /// volume is not inside the frustum.
    /// See https://cesium.com/blog/2015/08/04/fast-hierarchical-culling/
    /// </summary>
    public struct PlaneClipMask
    {
        /// <summary>
        /// Represents a code where all planes are marked as intersecting
        /// </summary>
        const int MASK_INTERSECTING = 0;
        const int MASK_INSIDE = (0x3F << 6);

        /// <summary>
        /// Bitwise mask where each bit corresponds to a clipping plane
        /// bit i is 0 iff the node MIGHT intersect plane[i]
        /// bit i is 1 if the node is DEFINITELY inside plane[i]
        /// There are 6 bits in the order: far (MSB), near, top, bottom, right, left (LSB)
        /// </summary>
        int code;

        /// <summary>
        /// True if any the volume being checked is outside of any plane, in which case it is fully outside the frustum
        /// </summary>
        bool anyOutside;

        public IntersectionType Intersection
        {
            get
            {
                if (anyOutside)
                {
                    // If the volume is fully outside any plane, then it is outside
                    return IntersectionType.OUTSIDE;
                }
                else if (code == MASK_INSIDE)
                {
                    // If the volume is inside all 6 planes then it is fully inside the volume
                    return IntersectionType.INSIDE;
                }
                else
                {
                    // Otherwise we are intersecting or don't have enough information to know for sure
                    return IntersectionType.INTERSECTING;
                }
            }
        }

        /// <summary>
        /// Returns a default mask which indicates that all planes must be checked for intersection with the frustum
        /// </summary>
        /// <returns></returns>
        public static PlaneClipMask GetDefaultMask()
        {
            return new PlaneClipMask();
        }

        /// <summary>
        /// Returns true if this node might intersect the given frustum plane (i.e. needs to be checked)
        /// Returns false if this node is fully inside the given frustum plane and can be skipped
        /// </summary>
        /// <param name="planeIdx"></param>
        /// <returns></returns>
        public bool Intersecting(int planeIdx)
        {
            // Return true if we are not inside this plane
            return (code & (1 << planeIdx)) == 0;
        }

        /// <summary>
        /// Mark this node as being completly inside the plane, subsequent intersection checks will not use this plane
        /// </summary>
        /// <param name="planeIdx"></param>
        public void Set(int planeIdx, IntersectionType intersection)
        {
            if (intersection == IntersectionType.OUTSIDE)
            {
                anyOutside = true;
            }
            else if (intersection == IntersectionType.INSIDE)
            {
                // Set the bit i to 1 for inside
                code |= (1 << planeIdx);
            }
            else
            {
                // Set bit i to 0 for intersecting
                code &= ~(1 << planeIdx);
            }
        }

    }

    public abstract class Unity3DTileBoundingVolume
    {

        public abstract IntersectionType IntersectPlane(Plane plane);

        public abstract bool Contains(Vector3 point);

        public abstract float MinDistanceTo(Vector3 point);

        public abstract float CenterDistanceTo(Ray ray);

        public abstract void DebugDraw(Color c, Transform t);

        public abstract BoundingSphere BoundingSphere();

        public abstract float Volume();

        public abstract string SizeString();

        public PlaneClipMask IntersectPlanes(Plane[] planes)
        {
            return IntersectPlanes(planes, PlaneClipMask.GetDefaultMask());
        }

        public PlaneClipMask IntersectPlanes(Plane[] planes, PlaneClipMask mask)
        {
            if (mask.Intersection != IntersectionType.INTERSECTING)
            {
                return mask;
            }

            for (var i = 0; i < planes.Length; ++i)
            {
                if (mask.Intersecting(i))
                {
                    IntersectionType value = this.IntersectPlane(planes[i]);
                    mask.Set(i, value);
                    if(value == IntersectionType.OUTSIDE)
                    {
                        break;
                    }
                }
            }
            return mask;
        }
    }

    /// <summary>
    /// Port from 
    /// https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Core/OrientedBoundingBox.js
    /// and
    /// https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Scene/TileOrientedBoundingBox.js 
    /// </summary>
    public class TileOrientedBoundingBox : Unity3DTileBoundingVolume
    {
        public Vector3 Center;
        public Vector3 HalfAxesX;
        public Vector3 HalfAxesY;
        public Vector3 HalfAxesZ;

        public TileOrientedBoundingBox(Vector3 center, Vector3 halfAxesX, Vector3 halfAxesY, Vector3 halfAxesZ)
        {
            this.Center = center;
            this.HalfAxesX = halfAxesX;
            this.HalfAxesY = halfAxesY;
            this.HalfAxesZ = halfAxesZ;
        }

        public override void DebugDraw(Color col, Transform t)
        {
            Vector3 a = Center + HalfAxesX + HalfAxesY + HalfAxesZ;
            Vector3 b = Center - HalfAxesX + HalfAxesY + HalfAxesZ;
            Vector3 c = Center + HalfAxesX - HalfAxesY + HalfAxesZ;
            Vector3 d = Center - HalfAxesX - HalfAxesY + HalfAxesZ;
            Vector3 e = Center + HalfAxesX + HalfAxesY - HalfAxesZ;
            Vector3 f = Center - HalfAxesX + HalfAxesY - HalfAxesZ;
            Vector3 g = Center + HalfAxesX - HalfAxesY - HalfAxesZ; 
            Vector3 h = Center - HalfAxesX - HalfAxesY - HalfAxesZ;

            a = t.TransformPoint(a);
            b = t.TransformPoint(b);
            c = t.TransformPoint(c);
            d = t.TransformPoint(d);
            e = t.TransformPoint(e);
            f = t.TransformPoint(f);
            g = t.TransformPoint(g);
            h = t.TransformPoint(h);

            Unity3DTilesDebug.DrawLine(a, b, col);
            Unity3DTilesDebug.DrawLine(a, c, col);
            Unity3DTilesDebug.DrawLine(c, d, col);
            Unity3DTilesDebug.DrawLine(b, d, col);
            Unity3DTilesDebug.DrawLine(e, f, col);
            Unity3DTilesDebug.DrawLine(e, g, col);
            Unity3DTilesDebug.DrawLine(g, h, col);
            Unity3DTilesDebug.DrawLine(f, h, col);
            Unity3DTilesDebug.DrawLine(a, e, col);
            Unity3DTilesDebug.DrawLine(b, f, col);
            Unity3DTilesDebug.DrawLine(c, g, col);
            Unity3DTilesDebug.DrawLine(d, h, col);
        }

        public override bool Contains(Vector3 point)
        {
            return MinDistanceTo(point) == 0;
        }

        public override float MinDistanceTo(Vector3 point)
        {
            var offset = point - this.Center;

            var u = HalfAxesX;
            var v = HalfAxesY;
            var w = HalfAxesZ;

            var uHalf = u.magnitude;
            var vHalf = v.magnitude;
            var wHalf = w.magnitude;

            u.Normalize();
            v.Normalize();
            w.Normalize();

            Vector3 pPrime = new Vector3();
            pPrime.x = Vector3.Dot(offset, u);
            pPrime.y = Vector3.Dot(offset, v);
            pPrime.z = Vector3.Dot(offset, w);

            float distanceSquared = 0.0f;
            float d;

            if (pPrime.x < -uHalf)
            {
                d = pPrime.x + uHalf;
                distanceSquared += d * d;
            }
            else if (pPrime.x > uHalf)
            {
                d = pPrime.x - uHalf;
                distanceSquared += d * d;
            }

            if (pPrime.y < -vHalf)
            {
                d = pPrime.y + vHalf;
                distanceSquared += d * d;
            }
            else if (pPrime.y > vHalf)
            {
                d = pPrime.y - vHalf;
                distanceSquared += d * d;
            }

            if (pPrime.z < -wHalf)
            {
                d = pPrime.z + wHalf;
                distanceSquared += d * d;
            }
            else if (pPrime.z > wHalf)
            {
                d = pPrime.z - wHalf;
                distanceSquared += d * d;
            }
            return Mathf.Sqrt(distanceSquared);
        }
        
        public override float CenterDistanceTo(Ray ray)
        {
            return Vector3.Distance(this.Center, ray.origin + Vector3.Project(this.Center - ray.origin, ray.direction));
        }

        public override IntersectionType IntersectPlane(Plane plane)
        {
            Vector3 normal = plane.normal;
            var u = HalfAxesX;
            var v = HalfAxesY;
            var w = HalfAxesZ;

            float radEffective = Mathf.Abs(normal.x * u.x + normal.y * u.y + normal.z * u.z) +
                                 Mathf.Abs(normal.x * v.x + normal.y * v.y + normal.z * v.z) +
                                 Mathf.Abs(normal.x * w.x + normal.y * w.y + normal.z * w.z);
            var distanceToPlane = Vector3.Dot(normal, this.Center) + plane.distance;

            if (distanceToPlane <= -radEffective)
            {
                // The entire box is on the negative side of the plane normal
                return IntersectionType.OUTSIDE;
            }
            else if (distanceToPlane >= radEffective)
            {
                // The entire box is on the positive side of the plane normal
                return IntersectionType.INSIDE;
            }
            return IntersectionType.INTERSECTING;
        }

        public override BoundingSphere BoundingSphere()
        {
            return new TileBoundingSphere(this).BoundingSphere();
        }

        public override float Volume()
        {
            var rx = HalfAxesX.magnitude;
            var ry = HalfAxesY.magnitude;
            var rz = HalfAxesZ.magnitude;
            return (2 * rx) * (2 * ry) * (2 * rz);
        }

        public override string SizeString()
        {
            var rx = HalfAxesX.magnitude;
            var ry = HalfAxesY.magnitude;
            var rz = HalfAxesZ.magnitude;
            return string.Format("{0:f3}x{1:f3}x{2:f3}", 2 * rx, 2 * ry, 2 * rz);
        }

        public void Transform(Matrix4x4 transform)
        {
            this.Center = transform.MultiplyPoint(this.Center);
            this.HalfAxesX = transform.MultiplyVector(this.HalfAxesX);
            this.HalfAxesY = transform.MultiplyVector(this.HalfAxesY);
            this.HalfAxesZ = transform.MultiplyVector(this.HalfAxesZ);
        }
    }

    public class TileBoundingSphere : Unity3DTileBoundingVolume
    {
        public Vector3 Center;
        public float Radius;

        public TileBoundingSphere(Vector3 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        public TileBoundingSphere(TileOrientedBoundingBox box)
        {
            this.Center = box.Center;
            var u = box.HalfAxesX;
            var v = box.HalfAxesY;
            var w = box.HalfAxesZ;
            this.Radius = (u + v + w).magnitude;
        }

        public void Transform(Matrix4x4 transform)
        {
            this.Center = transform.MultiplyPoint(this.Center);
            //var scale = transform.lossyScale;   // Change to lossyScale in future versions of unity
            Vector3 scale = new Vector3(
                transform.GetColumn(0).magnitude,
                transform.GetColumn(1).magnitude,
                transform.GetColumn(2).magnitude
            );
            float maxScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            this.Radius *= maxScale;
        }

        public override IntersectionType IntersectPlane(Plane plane)
        {
            float distanceToPlane = Vector3.Dot(plane.normal, this.Center) + plane.distance;

            if (distanceToPlane < -this.Radius)
            {
                // The center point is negative side of the plane normal
                return IntersectionType.OUTSIDE;
            }
            else if (distanceToPlane < this.Radius)
            {
                // The center point is positive side of the plane, but radius extends beyond it; partial overlap
                return IntersectionType.INTERSECTING;
            }
            return IntersectionType.INSIDE;
        }

        public override bool Contains(Vector3 point)
        {
            return Vector3.Distance(this.Center, point) <= this.Radius;
        }

        public override float MinDistanceTo(Vector3 point)
        {
            return Mathf.Max(0.0f, Vector3.Distance(this.Center, point) - this.Radius);
        }

        public override float CenterDistanceTo(Ray ray)
        {
            return Vector3.Distance(this.Center, ray.origin + Vector3.Project(this.Center - ray.origin, ray.direction));
        }

        public override BoundingSphere BoundingSphere()
        {
            return new BoundingSphere(Center, Radius);
        }

        public override float Volume()
        {
            return (4.0f / 3.0f) * Mathf.PI * Radius * Radius * Radius;
        }

        public override string SizeString()
        {
            return string.Format("r={0:f3}", Radius);
        }

        public override void DebugDraw(Color c, Transform t)
        {
            throw new NotImplementedException();
        }
    }

    // TODO: Add support for bounding regions
    public class TileBoundingRegion : Unity3DTileBoundingVolume
    {

        public TileBoundingRegion()
        {
            throw new NotImplementedException();
        }

        public override void DebugDraw(Color c, Transform t)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override float MinDistanceTo(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override float CenterDistanceTo(Ray ray)
        {
            throw new NotImplementedException();
        }

        public override IntersectionType IntersectPlane(Plane plane)
        {
            throw new NotImplementedException();
        }

        public override BoundingSphere BoundingSphere()
        {
            throw new NotImplementedException();
        }

        public override float Volume()
        {
            throw new NotImplementedException();
        }

        public override string SizeString()
        {
            throw new NotImplementedException();
        }
    }
}
