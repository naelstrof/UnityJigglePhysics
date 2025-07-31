using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct JiggleJobSimulate : IJob {
    // TODO: doubles are strictly a bad way to track time, probably should be ints or longs.
    public double timeStamp;
    public float3 gravity;
    
    public NativeArray<float3> transformPositions;
    public NativeArray<quaternion> transformRotations;
    public NativeArray<JiggleBoneSimulatedPoint> simulatedPoints;
    
    public NativeArray<float3> outputPositions;
    public NativeArray<quaternion> outputRotations;
    
    public NativeReference<float3> outputSimulatedRootOffset;
    public NativeReference<float3> outputSimulatedRootPosition;

    public JiggleJobSimulate(Transform[] bones, JiggleBoneSimulatedPoint[] points) {
        var boneCount = bones.Length;
        var currentPositions = new float3[boneCount];
        var currentRotations = new quaternion[boneCount];
        for (int i = 0; i < boneCount; i++) {
            currentPositions[i] = bones[i].position;
            currentRotations[i] = bones[i].rotation;
        }
        transformPositions = new NativeArray<float3>(currentPositions, Allocator.Persistent);
        transformRotations = new NativeArray<quaternion>(currentRotations, Allocator.Persistent);
        
        simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(points, Allocator.Persistent);
        
        outputPositions = new NativeArray<float3>(currentPositions, Allocator.Persistent);
        outputRotations = new NativeArray<quaternion>(currentRotations, Allocator.Persistent);

        outputSimulatedRootOffset = new NativeReference<float3>(new float3(0f), Allocator.Persistent);
        outputSimulatedRootPosition = new NativeReference<float3>(currentPositions[0], Allocator.Persistent);
        
        timeStamp = Time.timeAsDouble;
        gravity = Physics.gravity;
    }

    public void Dispose() {
        if (transformPositions.IsCreated) {
            transformPositions.Dispose();
        }
        if (transformRotations.IsCreated) {
            transformRotations.Dispose();
        }
        if (simulatedPoints.IsCreated) {
            simulatedPoints.Dispose();
        }
        if (outputPositions.IsCreated) {
            outputPositions.Dispose();
        }
        if (outputRotations.IsCreated) {
            outputRotations.Dispose();
        }
    }

    private unsafe void Cache() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            if (point.parentIndex == -1) { // virtual root particles
                var child = simulatedPoints[point.childrenIndices[0]];
                var childChild = simulatedPoints[child.childrenIndices[0]];
                if (childChild.transformIndex == -1) { // edge case where it's a singular isolated root bone
                    point.pose = transformPositions[child.transformIndex] - new float3(0f, 0.25f, 0f);;
                    point.parentPose = transformPositions[child.transformIndex] - new float3(0f, 0.5f, 0f);
                    point.desiredLengthToParent = 0.25f;
                } else {
                    var diff = transformPositions[child.transformIndex] - transformPositions[childChild.transformIndex];
                    point.pose = transformPositions[child.transformIndex] + diff;
                    point.parentPose = transformPositions[child.transformIndex] + diff*2f;
                    point.desiredLengthToParent = math.length(diff);
                }
                point.desiredConstraint = point.pose;
                point.workingPosition = point.pose;
            } else if (point.transformIndex != -1) { // "real" particles
                var parent = simulatedPoints[point.parentIndex];
                point.pose = transformPositions[point.transformIndex];
                point.parentPose = parent.pose;
                point.desiredLengthToParent = math.distance(point.pose, parent.pose);
            } else { // virtual end particles
                var parent = simulatedPoints[point.parentIndex];
                point.pose = (parent.pose*2f - parent.parentPose);
                point.parentPose = parent.pose;
                point.desiredLengthToParent = math.distance(point.pose, point.parentPose);
            }
            
            simulatedPoints[i] = point;
        }
    }

    private void VerletIntegrate() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            if (point.parentIndex == -1) {
                continue;
            }
            var parent = simulatedPoints[point.parentIndex];
            
            var delta = point.position - point.lastPosition;
            var localSpaceVelocity = delta - (parent.position - parent.lastPosition);
            var velocity = delta - localSpaceVelocity;
            if (parent.parentIndex != -1) {
                point.workingPosition = point.position + velocity * (1f - parent.parameters.airDrag) + localSpaceVelocity * (1f - parent.parameters.drag) + gravity * parent.parameters.gravityMultiplier * (float)JiggleJobManager.FIXED_DELTA_TIME_SQUARED;
            } else {
                point.workingPosition = point.position + velocity * (1f-point.parameters.airDrag) + localSpaceVelocity * (1f-point.parameters.drag) + gravity * point.parameters.gravityMultiplier * (float)JiggleJobManager.FIXED_DELTA_TIME_SQUARED;
            }
            simulatedPoints[i] = point;
        }
    }
    
    quaternion FromToRotation(float3 from, float3 to) {
        if (math.all(from == to)) {
            return quaternion.identity;
        }
        var axis = math.cross(from, to);
        var angle = math.acos(math.clamp(math.dot(from, to), -1f, 1f));
        return quaternion.AxisAngle(axis, angle);
    }
    
    float float3Angle(float3 a, float3 b) {
        return math.degrees(math.acos(math.dot(math.normalize(a), math.normalize(b))));
    }

    private unsafe void Constrain() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            if (point.parentIndex == -1) {
                continue;
            }
            var parent = simulatedPoints[point.parentIndex];
            #region Special root particle solve
            if (parent.parentIndex == -1) {
                var child = simulatedPoints[point.childrenIndices[0]];
                point.desiredConstraint = point.workingPosition = math.lerp(point.workingPosition, point.pose, point.parameters.rootElasticity * point.parameters.rootElasticity);
                var head = point.pose;
                var tail = child.pose;
                var diffasdf = head - tail;
                parent.desiredConstraint = point.desiredConstraint + diffasdf;
                simulatedPoints[point.parentIndex] = parent;
                simulatedPoints[i] = point;
                continue;
            }
            #endregion

            #region Angle Constraint
            var parentAimPose = math.normalize(point.parentPose - parent.parentPose);
            var parentAim = math.normalize(parent.desiredConstraint - parent.parentPose);
            if (parent.parentIndex != -1) {
                var parentParent = simulatedPoints[parent.parentIndex];
                parentAim = math.normalize(parent.desiredConstraint - parentParent.desiredConstraint);
            }

            var currentLength = math.length(point.workingPosition - parent.desiredConstraint);
            var from_to_rot = FromToRotation(parentAimPose, parentAim);
            var current_pose_dir = math.normalize(point.pose - point.parentPose);
            var constraintTarget = math.rotate(from_to_rot, (current_pose_dir * currentLength));

            var desiredPosition = parent.desiredConstraint + constraintTarget;

            
            var error = math.distance(point.workingPosition, parent.desiredConstraint + constraintTarget);
            error /= currentLength;
            error = math.min(error, 1.0f);
            error = math.pow(error, parent.parameters.elasticitySoften);
            point.desiredConstraint = math.lerp(point.workingPosition, desiredPosition, parent.parameters.angleElasticity * error);
            #endregion
            
            // DO COLLISIONS HERE
            
            if (point.parameters.angleLimited) { // --- Angle Limit Constraint
                float angleA_deg = point.parameters.angleLimit;
                // TODO: This should be radians instead of degrees
                float angleC_deg = float3Angle(
                    point.desiredConstraint - desiredPosition,
                    parent.desiredConstraint - desiredPosition
                ); // known included angle C

                float b = math.distance(point.parentPose, desiredPosition); // known side opposite angle B

                float angleB_deg = 180f - angleA_deg - angleC_deg;

                float angleA_rad = angleA_deg * Mathf.Deg2Rad;
                float angleB_rad = angleB_deg * Mathf.Deg2Rad;

                float a = b * math.sin(angleA_rad) / Mathf.Sin(angleB_rad);

                var correctionDir = math.normalize(desiredPosition - point.desiredConstraint);
                var correctionDistance = math.length(desiredPosition - point.desiredConstraint);

                var angleCorrectionDistance = math.max(0f, correctionDistance - a);
                var angleCorrection = (correctionDir * angleCorrectionDistance) * (1f - point.parameters.angleLimitSoften);
                point.desiredConstraint += angleCorrection;
            }

            #region Length Constraint
            var length_elasticity = parent.parameters.lengthElasticity;
            var diff = point.desiredConstraint - parent.desiredConstraint;
            var dir = math.normalize(diff);
            var forwardConstraint = math.lerp(point.desiredConstraint, parent.desiredConstraint + dir * point.desiredLengthToParent, length_elasticity);
            point.desiredConstraint = forwardConstraint;
            #endregion
            
            point.workingPosition = forwardConstraint;
            simulatedPoints[i] = point;
            continue;
            
            #region Back-propagated motion for collisions
            if (parent.parameters is { angleElasticity: 1f, lengthElasticity: 1f }) { // FIXME: Also check if collisions are disabled
                point.workingPosition = forwardConstraint;
                simulatedPoints[i] = point;
                continue;
            }

            if (point.childenCount > 0) { // Back-propagated motion specifically for collision enabled chains
                var child = simulatedPoints[point.childrenIndices[0]];
                var aim_pose = math.normalize(child.pose - point.parentPose);
                var aim = math.normalize(child.workingPosition - parent.workingPosition);
                var from_to_rot_also = FromToRotation(aim_pose, aim);
                var parent_to_self = math.normalize(point.pose - point.parentPose);
                var real_length = math.length(point.workingPosition - parent.workingPosition);
                var targetPos = math.rotate(from_to_rot_also, (parent_to_self * real_length)) + parent.workingPosition;

                var error_also = math.length(point.workingPosition - targetPos);
                error_also /= point.desiredLengthToParent;
                error_also = math.min(error_also, 1.0f);
                error_also = math.pow(error_also, parent.parameters.elasticitySoften);
                var backward_constraint = math.lerp(point.workingPosition, targetPos, (parent.parameters.angleElasticity * parent.parameters.angleElasticity * error_also));

                var child_length_elasticity = point.parameters.lengthElasticity * point.parameters.lengthElasticity;

                var cdiff = backward_constraint - child.workingPosition;
                var cdir = math.normalize(cdiff);
                backward_constraint = math.lerp(backward_constraint, child.workingPosition + cdir * child.desiredLengthToParent, child_length_elasticity * 0.5f);
                point.workingPosition = math.lerp(forwardConstraint, backward_constraint, 0.5f);
            } else {
                point.workingPosition = forwardConstraint;
            }
            simulatedPoints[i] = point;
            #endregion
        }
    }

    private void FinishStep() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            point.lastPosition = point.position;
            point.position = point.workingPosition;
            simulatedPoints[i] = point;
        }
    }

    private unsafe void ApplyPose() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            if (point.childenCount == 0) {
                continue;
            }

            var child = simulatedPoints[point.childrenIndices[0]];

            var local_pose = point.pose;
            var local_child_pose = child.pose;
            var local_child_working_position = child.workingPosition;
            var local_working_position = point.workingPosition;

            if (point.parentIndex == -1) {
                continue;
            }

            float3 cachedAnimatedVector = new float3(0f);
            float3 simulatedVector = cachedAnimatedVector;

            if (point.childenCount == 1) {
                cachedAnimatedVector = math.normalize(local_child_pose - local_pose);
                simulatedVector = math.normalize(local_child_working_position - local_working_position);
            } else {
                var cachedAnimatedVectorSum = new float3(0f);
                var simulatedVectorSum = cachedAnimatedVectorSum;
                for (var j = 0; j < point.childenCount; j++) {
                    var child_also = simulatedPoints[point.childrenIndices[j]];
                    var local_child_pose_also = child_also.pose;
                    var local_child_working_position_also = child_also.workingPosition;
                    cachedAnimatedVectorSum += math.normalize(local_child_pose_also - local_pose);
                    simulatedVectorSum += math.normalize(local_child_working_position_also - local_working_position);
                }
                cachedAnimatedVector = math.normalize(cachedAnimatedVectorSum * (1f / point.childenCount));
                simulatedVector = math.normalize(simulatedVectorSum * (1f / point.childenCount));
            }
            
            var animPoseToPhysicsPose = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector), point.parameters.blend);

            outputPositions[point.transformIndex] = point.workingPosition;
            outputRotations[point.transformIndex] = animPoseToPhysicsPose * transformRotations[point.transformIndex];
            
            simulatedPoints[i] = point;
        }
    }
    
    public void Execute() {
        Cache();
        VerletIntegrate();
        Constrain();
        FinishStep();
        ApplyPose();
        
        var simulatedPosition = outputPositions[0];
        var pose = transformPositions[0];
        outputSimulatedRootOffset.Value = simulatedPosition - pose;
        outputSimulatedRootPosition.Value = simulatedPosition;
    }
}
