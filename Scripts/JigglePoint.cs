using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public class JigglePoint {
    private Vector3 position;
    private Transform transform;
    private Vector3 previousPosition;
    private double previousUpdateTime;
    private double updateTime;
    private Vector3 parentPosition;
    private Vector3 previousParentPosition;
    private struct PositionFrame {
        public Vector3 position;
        public double time;
        public PositionFrame(Vector3 position, double time) {
            this.position = position;
            this.time = time;
        }
    }
    
    private PositionFrame currentTargetAnimatedBoneFrame;
    private PositionFrame lastTargetAnimatedBoneFrame;
    public Vector3 extrapolatedPosition;
    public JigglePoint(Transform transform) {
        this.transform = transform;
        this.position = transform.position;
        previousPosition = position;
        updateTime = Time.timeAsDouble;
        previousUpdateTime = Time.timeAsDouble;
        parentPosition = position;
        previousParentPosition = parentPosition;
        currentTargetAnimatedBoneFrame = new PositionFrame(position, Time.timeAsDouble);
        lastTargetAnimatedBoneFrame = new PositionFrame(position, Time.timeAsDouble);
    }
    public void PrepareSimulate() {
        lastTargetAnimatedBoneFrame = currentTargetAnimatedBoneFrame;
        currentTargetAnimatedBoneFrame = new PositionFrame(transform.position, Time.timeAsDouble);
    }
    private Vector3 GetTargetBonePosition(PositionFrame prev, PositionFrame next, double time) {
        double diff = next.time - prev.time;
        if (diff == 0) {
            return next.position;
        }
        double t = (time - prev.time) / diff;
        return Vector3.Lerp(prev.position, next.position, (float)t);
    }
    public void Simulate(JiggleSettingsBase jiggleSettings, Vector3 force, double time) {
        parentPosition = GetTargetBonePosition(lastTargetAnimatedBoneFrame, currentTargetAnimatedBoneFrame, time);
        
        Vector3 localSpaceVelocity = (position-previousPosition) - (parentPosition-previousParentPosition);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            position, previousPosition, localSpaceVelocity, Time.fixedDeltaTime,
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Gravity),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.Friction),
            jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.AirFriction)
        );
        newPosition += force * (Time.deltaTime * jiggleSettings.GetParameter(JiggleSettingsBase.JiggleSettingParameter.AirFriction));
        newPosition = ConstrainSpring(newPosition, jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity)*jiggleSettings.GetParameter(JiggleSettings.JiggleSettingParameter.LengthElasticity));
        SetNewPosition(newPosition, time);
    }
    public void SetNewPosition(Vector3 newPosition, double time) {
        previousPosition = position;
        previousParentPosition = parentPosition;
        previousUpdateTime = updateTime;
        position = newPosition;
        updateTime = time;
    }
    public void DeriveFinalSolvePosition() {
        double t = (Time.time - previousUpdateTime) / Time.fixedDeltaTime;
        extrapolatedPosition = Vector3.LerpUnclamped(previousPosition, position, (float)t);
    }
    public Vector3 ConstrainSpring(Vector3 newPosition, float elasticity) {
        return Vector3.Lerp(newPosition, parentPosition, elasticity);
    }
    public void DrawGizmos(Color color) {
        Gizmos.color = color;
        Gizmos.DrawSphere(extrapolatedPosition, 0.15f);
    }
    public void DebugDraw(Color color) {
        Debug.DrawLine(extrapolatedPosition, transform.position, color, Time.deltaTime, false);
    }
}

}