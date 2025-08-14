using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public struct JiggleTransform {
    public bool isVirtual;
    public float3 position;
    public quaternion rotation;
    public static JiggleTransform Lerp(JiggleTransform a, JiggleTransform b, float t) {
        return new JiggleTransform() {
            isVirtual = a.isVirtual,
            position = math.lerp(a.position, b.position, t),
            rotation = math.slerp(a.rotation, b.rotation, t),
        };
    }
}

public unsafe struct JiggleTreeStruct {
    public bool Equals(JiggleTreeStruct other) {
        return GetHashCode() == other.GetHashCode();
    }

    public override bool Equals(object obj) {
        return obj is JiggleTreeStruct other && Equals(other);
    }

    public override int GetHashCode() {
        return unchecked((int) (long) points);
    }
    
    public static bool operator == (JiggleTreeStruct left, JiggleTreeStruct right) => left.Equals(right);
    public static bool operator != (JiggleTreeStruct left, JiggleTreeStruct right) => !left.Equals(right);

    public int rootID;
    public uint pointCount;
    public uint transformIndexOffset;
    public JiggleBoneSimulatedPoint* points;

    public JiggleTreeStruct(int rootID, int indexOffset, JiggleBoneSimulatedPoint[] inputPoints) {
        this.rootID = rootID;
        pointCount = (uint) inputPoints.Length;
        transformIndexOffset = (uint)indexOffset;
        points = (JiggleBoneSimulatedPoint*) UnsafeUtility.Malloc(
            Marshal.SizeOf<JiggleBoneSimulatedPoint>() * pointCount,
            UnsafeUtility.AlignOf<JiggleBoneSimulatedPoint>(),
            Allocator.Persistent
            );
        fixed (JiggleBoneSimulatedPoint* src = inputPoints) {
            UnsafeUtility.MemCpy(points, src, sizeof(JiggleBoneSimulatedPoint) * pointCount);
        }
    }

    public void Set(int rootID, JiggleBoneSimulatedPoint[] inputPoints) {
        this.rootID = rootID;
        if (inputPoints.Length == pointCount) {
            fixed (JiggleBoneSimulatedPoint* src = inputPoints) {
                UnsafeUtility.MemCpy(points, src, sizeof(JiggleBoneSimulatedPoint) * pointCount);
            }
        } else {
            Dispose();
            pointCount = (uint) inputPoints.Length;
            points = (JiggleBoneSimulatedPoint*) UnsafeUtility.Malloc(
                Marshal.SizeOf<JiggleBoneSimulatedPoint>() * pointCount,
                UnsafeUtility.AlignOf<JiggleBoneSimulatedPoint>(),
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
                Gizmos.DrawWireSphere(point.position, 0.1f);
            } else {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(point.position, 0.1f);
            }

            if (point.childenCount != 0) {
                var child = points[point.childrenIndices[0]];
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(point.position, child.position);
            }
        }
    }
}

public static class JiggleTreeStructExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JiggleTransform GetInputPose(this JiggleTreeStruct self, NativeArray<JiggleTransform> inputPoses, int index) {
        return inputPoses[index + (int)self.transformIndexOffset];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteOutputPose(this JiggleTreeStruct self, NativeArray<PoseData> outputPoses,
        int index, JiggleTransform pose, float3 rootOffset, float3 rootPosition) {
        var old = outputPoses[index + (int)self.transformIndexOffset];
        pose.isVirtual = old.pose.isVirtual;
        old.pose = pose;
        old.rootOffset = rootOffset;
        old.rootPosition = rootPosition;

        outputPoses[index + (int)self.transformIndexOffset] = old;
    }
}