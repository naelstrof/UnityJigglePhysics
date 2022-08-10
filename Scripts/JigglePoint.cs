using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public class JigglePoint {
    public Vector3 position;
    public Transform transform;
    public Vector3 previousPosition;
    private float previousUpdateTime;
    private float updateTime;
    private Vector3 parentPosition;
    private Vector3 previousParentPosition;

    public Vector3 interpolatedPosition {
        get {
            float diff = updateTime - previousUpdateTime;
            if (diff == 0) {
                return position;
            }
            float t = (Time.time - previousUpdateTime) / diff;
            return Vector3.LerpUnclamped(previousPosition, position, t);
        }
    }
    public JigglePoint(Transform transform) {
        this.transform = transform;
        this.position = transform.position;
        previousPosition = position;
        updateTime = Time.time;
        previousUpdateTime = Time.time;
        parentPosition = position;
        previousParentPosition = parentPosition;
    }
    public void PrepareSimulate() {
        previousParentPosition = parentPosition;
        parentPosition = transform.position;
    }
    public void Simulate(JiggleSettingsBase jiggleSettings, Vector3 force) {
        Vector3 localSpaceVelocity = (position-previousPosition) - (parentPosition-previousParentPosition);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            position, previousPosition, localSpaceVelocity, Time.deltaTime,
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Gravity),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Friction),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AirFriction)
        );
        newPosition += force * (Time.deltaTime * jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.AirFriction));
        newPosition = ConstrainSpring(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity));
        SetNewPosition(newPosition);
    }
    public void SetNewPosition(Vector3 newPosition) {
        previousPosition = position;
        previousUpdateTime = updateTime;
        position = newPosition;
        updateTime = Time.time;
    }
    public Vector3 ConstrainSpring(Vector3 newPosition, float elasticity) {
        return Vector3.Lerp(newPosition, parentPosition, elasticity);
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