using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class JiggleTree {
    
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;
    public bool dirty { get; private set; }
    public bool valid { get; private set; }

    public void SetDirty(bool removed) {
        dirty = true;
        valid = !removed;
        if (valid) {
            Debug.Log("Creating tree...");
        } else {
            Debug.Log("Removing tree...");
        }
    }
    public void ClearDirty() => dirty = false;
    
    private bool hasJiggleTreeStruct = false;
    private JiggleTreeStruct jiggleTreeStruct;

    public JiggleTreeStruct GetStruct() {
        if (hasJiggleTreeStruct) {
            return jiggleTreeStruct;
        }
        jiggleTreeStruct = new JiggleTreeStruct(0, points);
        return jiggleTreeStruct;
    }

    public void SetStruct(JiggleTreeStruct jiggleTreeStruct) {
        this.jiggleTreeStruct = jiggleTreeStruct;
        hasJiggleTreeStruct = true;
    }
    
    public JiggleTree(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        dirty = true;
        valid = true;
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
