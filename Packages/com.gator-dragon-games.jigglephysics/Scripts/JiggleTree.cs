using System.Collections.Generic;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleTree {
    public Transform[] bones;
    public JiggleSimulatedPoint[] points;
    public Vector3[] restPositions;
    public Quaternion[] restRotations;
    public JigglePointParameters[] parameters;
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

        jiggleTreeJobData = new JiggleTreeJobData(rootID, 0, 0, personalColliders.Length, points, parameters);
        hasJiggleTreeStruct = true;
        return jiggleTreeJobData;
    }

    public void Dispose() {
        for(int i=0;i<points.Length;i++) {
            var bone = bones[i];
            if (bone) {
                bone.localPosition = restPositions[i];
                bone.localRotation = restRotations[i];
            }
        }
        if (hasJiggleTreeStruct) {
            jiggleTreeJobData.Dispose();
            hasJiggleTreeStruct = false;
        }
    }

    /// <summary>
    /// Immediately resamples the rest pose of the bones in the tree. This can be useful if you have modified the bones' transforms on initialization and want to control when the rest pose is sampled.
    /// This ONLY has an effect if you call it before the next Jiggle Physics update, as we only copy it once to the jobs system.
    /// </summary>
    public void ResampleRestPose() {
        for(int i=0;i<bones.Length;i++) {
            bones[i].GetLocalPositionAndRotation(out var pos, out var rot);
            restPositions[i] = pos;
            restRotations[i] = rot;
        }
    }

    public void SetColliderIndexOffset(int offset) {
        if (!hasJiggleTreeStruct) {
            GetStruct();
        }
        jiggleTreeJobData.colliderIndexOffset = (uint)offset;
    }
    
    public void SetTransformIndexOffset(int offset) {
        if (!hasJiggleTreeStruct) {
            GetStruct();
        }
        jiggleTreeJobData.transformIndexOffset = (uint)offset;
    }

    public JiggleTree(List<Transform> bones, List<JiggleSimulatedPoint> points, List<JigglePointParameters> parameters, List<Transform> personalColliderTransforms, List<JiggleCollider> personalColliders) {
        dirty = false;
        this.bones = bones.ToArray();
        restPositions = new Vector3[this.bones.Length];
        restRotations = new Quaternion[this.bones.Length];
        for(int i=0;i<this.bones.Length;i++) {
            bones[i].GetLocalPositionAndRotation(out var pos, out var rot);
            restPositions[i] = pos;
            restRotations[i] = rot;
        }
        this.points = points.ToArray();
        this.parameters = parameters.ToArray();
        this.personalColliders = personalColliders.ToArray();
        this.personalColliderTransforms = personalColliderTransforms.ToArray();
        rootID = bones[0].GetInstanceID();
    }

    public void Set(List<Transform> bones, List<JiggleSimulatedPoint> points, List<JigglePointParameters> parameters, List<Transform> personalColliderTransforms, List<JiggleCollider> personalColliders) {
        var bonesCount = bones.Count;
        var pointsCount = points.Count;
        if (bonesCount == this.bones.Length && pointsCount == this.points.Length) {
            for (int i = 0; i < bonesCount; i++) {
                this.bones[i] = bones[i];
            }
            for (int i = 0; i < pointsCount; i++) {
                this.points[i] = points[i];
            }
            for (int i = 0; i < pointsCount; i++) {
                this.parameters[i] = parameters[i];
            }
            for(int i=0;i<this.bones.Length;i++) {
                bones[i].GetLocalPositionAndRotation(out var pos, out var rot);
                restPositions[i] = pos;
                restRotations[i] = rot;
            }
        } else {
            this.bones = bones.ToArray();
            this.points = points.ToArray();
            this.parameters = parameters.ToArray();
            restPositions = new Vector3[this.bones.Length];
            restRotations = new Quaternion[this.bones.Length];
            for(int i=0;i<this.bones.Length;i++) {
                bones[i].GetLocalPositionAndRotation(out var pos, out var rot);
                restPositions[i] = pos;
                restRotations[i] = rot;
            }
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
            jiggleTreeJobData.Set(rootID, this.points, this.parameters);
        }

        dirty = false;
    }

    public void SetParameters(List<JigglePointParameters> parameters) {
        var pointsCount = points.Length;
        var parametersCount = parameters.Count;
        if (pointsCount != parametersCount) {
            Debug.LogError($"JiggleTree.SetParameters: points count {pointsCount} does not match parameters count {parametersCount}");
            return;
        }

        for (int i = 0; i < pointsCount; i++) {
            this.parameters[i] = parameters[i];
        }

        if (hasJiggleTreeStruct) {
            jiggleTreeJobData.SetParameters(this.parameters);
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

}