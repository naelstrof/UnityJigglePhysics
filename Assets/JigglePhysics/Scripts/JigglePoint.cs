using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public class JigglePoint {
    public Vector3 position;
    public Transform transform;
    public Vector3 previousPosition;
    private Vector3 parentPosition;
    private Vector3 parentPreviousPosition;
    public Vector3 interpolatedPosition {
        get {
            float timeSinceLastUpdate = Time.time-Time.fixedTime;
            // interpolation, delayed by fixedDeltaTime
            // return Vector3.Lerp(previousPosition, position, timeSinceLastUpdate/Time.fixedDeltaTime);
            // extrapolation
            return Vector3.Lerp(position, position+(position-previousPosition), timeSinceLastUpdate/Time.fixedDeltaTime);
        }
    }
    public JigglePoint(Transform transform) {
        this.transform = transform;
        this.position = transform.position;
        previousPosition = position;
        parentPreviousPosition = position;
        parentPosition = position;
    }
    public void PrepareSimulate() {
        parentPreviousPosition = parentPosition;
        parentPosition = transform.position;
    }
    public void Simulate(JiggleSettingsBase jiggleSettings) {
        Vector3 localSpaceVelocity = (position-previousPosition) - (parentPosition-parentPreviousPosition);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            position, previousPosition, localSpaceVelocity, Time.deltaTime,
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Gravity),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Friction),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AirFriction)
        );
        newPosition = ConstrainSpring(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity));
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
            Debug.DrawLine(interpolatedPosition, transform.position, color, Time.deltaTime, false);
        } else {
            Debug.DrawLine(position, transform.position, color, Time.deltaTime, false);
        }
    }
}

}