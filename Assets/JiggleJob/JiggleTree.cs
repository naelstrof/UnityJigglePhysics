using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class JiggleTree {
    
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;
    public bool dirty { get; private set; }

    public void SetDirty() => dirty = true;
    public void ClearDirty() => dirty = false;
    
    public unsafe JiggleTreeStruct GetStructAndUpdateLists(
        List<Transform> transforms, 
        List<JiggleTransform> jiggleTransforms, 
        List<JiggleTransform> localJiggleTransforms
        ) {
        uint pointCount = (uint)points.Length;
        JiggleTreeStruct ret = new JiggleTreeStruct() {
            pointCount = pointCount,
            transformIndexOffset = (uint)transforms.Count,
            points = (JiggleBoneSimulatedPoint*)UnsafeUtility.Malloc(Marshal.SizeOf<JiggleBoneSimulatedPoint>() * pointCount, UnsafeUtility.AlignOf<JiggleBoneSimulatedPoint>(), Allocator.Persistent ),
        };
        for(int i=0;i < pointCount; i++) {
            var point = points[i];
            if (point.hasTransform) {
                var bone = bones[i];
                transforms.Add(bone);
                bone.GetPositionAndRotation(out var position, out var rotation);
                bone.GetLocalPositionAndRotation(out var localPosition, out var localRotation);
                jiggleTransforms.Add(new JiggleTransform() {
                    isVirtual = false,
                    position = position,
                    rotation = rotation,
                });
                localJiggleTransforms.Add(new JiggleTransform() {
                    isVirtual = false,
                    position = localPosition,
                    rotation = localRotation,
                });
            } else {
                transforms.Add(bones[i]);
                jiggleTransforms.Add(new JiggleTransform() {
                    isVirtual = true,
                });
                localJiggleTransforms.Add(new JiggleTransform() {
                    isVirtual = true,
                });
            }
            ret.points[i] = point;
        }
        return ret;
    }
    
    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        dirty = true;
        var boneCount = bones.Length;
        var pointCount = points.Length;
        this.bones = new Transform[boneCount];
        this.points = new JiggleBoneSimulatedPoint[pointCount];
        for(int i=0; i < boneCount; i++) {
            this.bones[i] = bones[i];
        }
        for(int i=0; i < pointCount; i++) {
            this.points[i] = points[i];
        }
    }
    private static void DebugDrawSphere(Vector3 origin, float radius, Color color, float duration, int segments = 8) {
        float angleStep = 360f / segments;
        Vector3 prevPoint = Vector3.zero;
        Vector3 currPoint = Vector3.zero;

        // Draw circle in XY plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }

        // Draw circle in XZ plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }

        // Draw circle in YZ plane
        for (int i = 0; i <= segments; i++) {
            float angle = Mathf.Deg2Rad * i * angleStep;
            currPoint = origin + new Vector3(0, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            if (i > 0) Debug.DrawLine(prevPoint, currPoint, color, duration);
            prevPoint = currPoint;
        }
    }
    
}
