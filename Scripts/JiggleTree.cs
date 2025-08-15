using System.Collections.Generic;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleTree {

    public Transform[] bones;
    public JiggleSimulatedPoint[] points;
    public Transform[] personalColliderTransforms;
    public JiggleCollider[] personalColliders;
    public bool dirty { get; private set; }
    public int rootID { get; private set; }

    public void SetDirty() {
        dirty = true;
    }

    public void ClearDirty() => dirty = false;

    private bool hasJiggleTreeStruct = false;
    private JiggleTreeJobData jiggleTreeJobData;

    public JiggleTreeJobData GetStruct() {
        if (hasJiggleTreeStruct) {
            return jiggleTreeJobData;
        }

        jiggleTreeJobData = new JiggleTreeJobData(rootID, 0, 0, personalColliders.Length, points);
        return jiggleTreeJobData;
    }

    public void Dispose() {
        if (hasJiggleTreeStruct) {
            jiggleTreeJobData.Dispose();
            hasJiggleTreeStruct = false;
        }
    }

    public JiggleTree(List<Transform> bones, List<JiggleSimulatedPoint> points, List<Transform> personalColliderTransforms, List<JiggleCollider> personalColliders) {
        dirty = false;
        this.bones = bones.ToArray();
        this.points = points.ToArray();
        this.personalColliders = personalColliders.ToArray();
        this.personalColliderTransforms = personalColliderTransforms.ToArray();
        rootID = bones[0].GetInstanceID();
    }

    public void Set(List<Transform> bones, List<JiggleSimulatedPoint> points, List<Transform> personalColliderTransforms, List<JiggleCollider> personalColliders) {
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
        
        
        var personalColliderTransformsCount = personalColliderTransforms.Count;
        var personalCollidersCount = personalColliders.Count;
        if (personalCollidersCount == this.personalColliders.Length && personalColliderTransformsCount == this.personalColliderTransforms.Length) {
            for (int i = 0; i < personalCollidersCount; i++) {
                this.personalColliders[i] = personalColliders[i];
            }
            for (int i = 0; i < personalColliderTransformsCount; i++) {
                this.personalColliderTransforms[i] = personalColliderTransforms[i];
            }
        } else {
            this.personalColliders = personalColliders.ToArray();
            this.personalColliderTransforms = personalColliderTransforms.ToArray();
        }

        rootID = bones[0].GetInstanceID();
        if (hasJiggleTreeStruct) {
            jiggleTreeJobData.Set(rootID, this.points);
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

}