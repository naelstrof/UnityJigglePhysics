using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatedPoint {

    public SimulatedPoint parent;
    public Vector3 position;
    public Vector3 previousPosition;
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

    public void StepPhysics(float deltaTime) {
        float squaredDeltaTime = deltaTime * deltaTime;
        Vector3 newPosition = position + (position - previousPosition) + Physics.gravity * squaredDeltaTime;
        previousPosition = position;
        position = newPosition;
    }

    public void DebugDraw() {
        if (parent == null) return;
        Debug.DrawLine(position, parent.position, Color.blue);
    }
    
}
