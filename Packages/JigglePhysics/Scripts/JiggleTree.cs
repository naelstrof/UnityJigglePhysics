using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class JiggleTree {
    
    public Transform[] bones;
    public JiggleBoneSimulatedPoint[] points;
    public bool dirty { get; private set; }
    public int rootID { get; private set; }
    
    public void SetDirty() {
        dirty = true;
    }
    public void ClearDirty() => dirty = false;
    
    private bool hasJiggleTreeStruct = false;
    private JiggleTreeStruct jiggleTreeStruct;

    public JiggleTreeStruct GetStruct() {
        if (hasJiggleTreeStruct) {
            return jiggleTreeStruct;
        }
        jiggleTreeStruct = new JiggleTreeStruct(rootID,0, points);
        return jiggleTreeStruct;
    }

    public void Dispose() {
        if (hasJiggleTreeStruct) {
            jiggleTreeStruct.Dispose();
            hasJiggleTreeStruct = false;
        }
    }
    
    public JiggleTree(List<Transform> bones, List<JiggleBoneSimulatedPoint> points) {
        dirty = false;
        this.bones = bones.ToArray();
        this.points = points.ToArray();
        rootID = bones[0].GetInstanceID();
    }

    public void Set(List<Transform> bones, List<JiggleBoneSimulatedPoint> points) {
        var bonesCount = bones.Count;
        var pointsCount = points.Count;
        if (bonesCount == this.bones.Length && pointsCount == this.points.Length) {
            for (int i = 0; i < bonesCount; i++) {
                this.bones[i] = bones[i];
            }
            for (int i = 0; i < pointsCount; i++) {
                this.points[i] = points[i];
            }
        } else {
            this.bones = bones.ToArray();
            this.points = points.ToArray();
        }
        rootID = bones[0].GetInstanceID();
        if (hasJiggleTreeStruct) {
            jiggleTreeStruct.Set(rootID, this.points);
        }
        dirty = false;
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
