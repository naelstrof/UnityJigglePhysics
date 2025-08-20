using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {
public unsafe struct JiggleTreeJobData {
    public bool Equals(JiggleTreeJobData other) {
        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object obj) {
        return obj is JiggleTreeJobData other && Equals(other);
    }

    public override int GetHashCode() {
        return unchecked((int)(long)points);
    }

    public static bool operator ==(JiggleTreeJobData left, JiggleTreeJobData right) => left.Equals(right);
    public static bool operator !=(JiggleTreeJobData left, JiggleTreeJobData right) => !left.Equals(right);

    public int rootID;
    public uint pointCount;
    public uint transformIndexOffset;
    public uint colliderIndexOffset;
    public uint colliderCount;
    public float extents;
    public JiggleSimulatedPoint* points;

    public JiggleTreeJobData(int rootID, int transformIndexOffset, int colliderIndexOffset, int colliderCount, JiggleSimulatedPoint[] inputPoints) {
        this.rootID = rootID;
        pointCount = (uint)inputPoints.Length;
        this.colliderIndexOffset = (uint)colliderIndexOffset;
        this.transformIndexOffset = (uint)transformIndexOffset;
        this.colliderCount = (uint)colliderCount;
        points = (JiggleSimulatedPoint*)UnsafeUtility.Malloc(
            Marshal.SizeOf<JiggleSimulatedPoint>() * pointCount,
            UnsafeUtility.AlignOf<JiggleSimulatedPoint>(),
            Allocator.Persistent
        );
        fixed (JiggleSimulatedPoint* src = inputPoints) {
            UnsafeUtility.MemCpy(points, src, sizeof(JiggleSimulatedPoint) * pointCount);
        }
        extents = 1f;
    }

    public void Set(int rootID, JiggleSimulatedPoint[] inputPoints) {
        this.rootID = rootID;
        if (inputPoints.Length == pointCount) {
            fixed (JiggleSimulatedPoint* src = inputPoints) {
                UnsafeUtility.MemCpy(points, src, sizeof(JiggleSimulatedPoint) * pointCount);
            }
        } else {
            Dispose();
            pointCount = (uint)inputPoints.Length;
            points = (JiggleSimulatedPoint*)UnsafeUtility.Malloc(
                Marshal.SizeOf<JiggleSimulatedPoint>() * pointCount,
                UnsafeUtility.AlignOf<JiggleSimulatedPoint>(),
                Allocator.Persistent
            );
        }
    }

    public void Dispose() {
        if (points != null) {
            UnsafeUtility.Free(points, Allocator.Persistent);
            points = null;
        }
    }

    public void OnGizmoDraw() {
        for (int i = 0; i < pointCount; i++) {
            var point = points[i];
            if (point.hasTransform) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(point.position, point.worldRadius);
            } else {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(point.position, point.worldRadius);
            }

            if (point.childenCount != 0) {
                var child = points[point.childrenIndices[0]];
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(point.position, child.position);
            }
        }
    }
}
}