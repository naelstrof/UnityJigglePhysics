using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JigglePoint {
    public Vector3 position;
    public Transform transform;
    public Vector3 previousPosition;
    public Vector3 interpolatedPosition {
        get {
            // interpolation, delayed by fixedDeltaTime
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
        }
    }
    public JigglePoint(Transform transform) {
        this.transform = transform;
        this.position = transform.position;
        previousPosition = position;
    }
    public void Simulate(JiggleSettings jiggleSettings) {
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(position, previousPosition, Time.deltaTime, jiggleSettings.gravityMultiplier, jiggleSettings.friction);
        newPosition = ConstrainSpring(newPosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity);
        SetNewPosition(newPosition);
    }
    public void SetNewPosition(Vector3 newPosition) {
        previousPosition = position;
        position = newPosition;
    }
    public Vector3 ConstrainSpring(Vector3 newPosition, float elasticity) {
        return Vector3.Lerp(newPosition, transform.position, elasticity);
    }
    public void DrawGizmos(Color color) {
        Gizmos.color = color;
        Gizmos.DrawSphere(interpolatedPosition, 0.15f);
    }
    public void DebugDraw(Color color, bool interpolated) {
        if (interpolated) {
            Debug.DrawLine(interpolatedPosition, transform.position, color);
        } else {
            Debug.DrawLine(position, transform.position, color);
        }
    }
}
