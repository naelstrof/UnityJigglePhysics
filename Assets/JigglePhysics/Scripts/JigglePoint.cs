using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigglePhysics {
public class JigglePoint {
    private Transform transform;
    private PositionSignal particleSignal;
    private PositionSignal animatedBoneSignal;
    public Vector3 extrapolatedPosition;
    private Vector3? preTeleportPosition;
    public JigglePoint(Transform transform) {
        this.transform = transform;
        var pos = transform.position;
        particleSignal = new PositionSignal(pos, Time.timeAsDouble);
        animatedBoneSignal = new PositionSignal(pos, Time.timeAsDouble);
    }
    public void PrepareSimulate() {
        animatedBoneSignal.SetPosition(transform.position, Time.timeAsDouble);
    }
    public void Simulate(JiggleSettingsData jiggleSettings, Vector3 force, double time) {
        var animatedBonePosition = animatedBoneSignal.SamplePosition(time);
        var animatedBonePositionPrev = animatedBoneSignal.SamplePosition(time-Time.fixedDeltaTime);
        Vector3 localSpaceVelocity = (particleSignal.GetCurrent()-particleSignal.GetPrevious()) - (animatedBonePosition-animatedBonePositionPrev);
        Vector3 newPosition = JiggleBone.NextPhysicsPosition(
            particleSignal.GetCurrent(), particleSignal.GetPrevious(), localSpaceVelocity, Time.fixedDeltaTime,
            jiggleSettings.gravityMultiplier,
            jiggleSettings.friction,
            jiggleSettings.airDrag
        );
        newPosition += force * (Time.deltaTime * jiggleSettings.airDrag);
        newPosition = ConstrainSpring(newPosition, animatedBonePosition, jiggleSettings.lengthElasticity*jiggleSettings.lengthElasticity);
        particleSignal.SetPosition(newPosition, time);
    }

    public void MatchAnimationInstantly() {
        var time = Time.timeAsDouble;
        var position = transform.position;
        particleSignal.FlattenSignal(time, position);
        animatedBoneSignal.FlattenSignal(time, position);
    }

    public void PrepareTeleport() {
        preTeleportPosition = transform.position;
    }
    public void FinishTeleport() {
        if (!preTeleportPosition.HasValue) {
            MatchAnimationInstantly();
            return;
        }

        var position = transform.position;
        Vector3 offset = position - preTeleportPosition.Value;
        particleSignal.OffsetSignal(offset);
        animatedBoneSignal.FlattenSignal(Time.timeAsDouble, position);
    }

    public void DeriveFinalSolvePosition() {
        extrapolatedPosition = particleSignal.SamplePosition(Time.timeAsDouble-Time.fixedDeltaTime);
    }
    public Vector3 ConstrainSpring(Vector3 newPosition, Vector3 animatedBonePosition, float elasticity) {
        return Vector3.Lerp(newPosition, animatedBonePosition, elasticity);
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