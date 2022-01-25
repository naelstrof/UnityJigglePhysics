using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedPoint {

    public SimulatedPoint parent;
    public Vector3 position;
    public Vector3 previousPosition;
    public Vector3 interpolatedPosition {
        get {
            // interpolation, delayed by fixedDeltaTime
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
        }
    }
    public float lengthToParent;

    public SimulatedPoint(SimulatedPoint parent, Vector3 position) {
        this.parent = parent;
        this.position = position;
        previousPosition = position;
        if (parent == null) {
            lengthToParent = 0;
            return;
        }
        lengthToParent = Vector3.Distance(parent.position, position);
    }
    public void SnapTo(Transform t) {
        previousPosition = position;
        position = t.position;
    }
    public void ConstrainLength() {
        Vector3 diff = position - parent.position;
        Vector3 dir = diff.normalized;
        position = parent.position + dir * lengthToParent;
    }

    public void StepPhysics(float deltaTime) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 newPosition = position + (position - previousPosition) + Physics.gravity * squaredDeltaTime;
        previousPosition = position;
        position = newPosition;
    }

    public void DebugDraw(Color color, bool interpolated) {
        if (parent == null) return;
        if (interpolated) {
            Debug.DrawLine(interpolatedPosition, parent.interpolatedPosition, color);
        } else {
            Debug.DrawLine(position, parent.position, color);
        }
    }
    
}
