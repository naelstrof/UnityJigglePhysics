using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct JiggleJobSimulate : IJobFor {
    // TODO: doubles are strictly a bad way to track time, probably should be ints or longs.
    public double timeStamp;
    public float3 gravity;
    
    [ReadOnly][NativeDisableParallelForRestriction]
    public NativeArray<JiggleTransform> inputPoses;
    [NativeDisableParallelForRestriction]
    public NativeArray<PoseData> outputPoses;
    [NativeDisableParallelForRestriction]
    public NativeArray<float3> testColliders;
    
    public NativeArray<JiggleTreeStruct> jiggleTrees;
    
    public JiggleJobSimulate(JiggleMemoryBus bus) {
        inputPoses = bus.simulateInputPoses;
        jiggleTrees = bus.jiggleTreeStructs;
        outputPoses = bus.simulationOutputPoseData;
        testColliders = bus.colliderPositions;
        timeStamp = Time.timeAsDouble;
        gravity = Physics.gravity;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        inputPoses = bus.simulateInputPoses;
        jiggleTrees = bus.jiggleTreeStructs;
        outputPoses = bus.simulationOutputPoseData;
        testColliders = bus.colliderPositions;
    }


    private unsafe void Cache(JiggleTreeStruct tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points[i];
            if (point.parentIndex == -1) { // virtual root particles
                var child = tree.points[point.childrenIndices[0]];
                var childChild = tree.points[child.childrenIndices[0]];
                var childPose = tree.GetInputPose(inputPoses, point.childrenIndices[0]);
                var childChildPose = tree.GetInputPose(inputPoses, child.childrenIndices[0]);
                if (!childChild.hasTransform) { // edge case where it's a singular isolated root bone
                    point.pose = childPose.position - new float3(0f, 0.25f, 0f);
                    point.parentPose = childPose.position - new float3(0f, 0.5f, 0f);
                    point.desiredLengthToParent = 0.25f;
                } else {
                    var diff = childPose.position - childChildPose.position;
                    point.pose = childPose.position + diff;
                    point.parentPose = childPose.position + diff*2f;
                    point.desiredLengthToParent = math.length(diff);
                }
                point.desiredConstraint = point.pose;
                point.workingPosition = point.pose;
            } else if (point.hasTransform) { // "real" particles
                var parent = tree.points[point.parentIndex];
                point.pose = tree.GetInputPose(inputPoses, i).position;
                point.parentPose = parent.pose;
                point.desiredLengthToParent = math.distance(point.pose, parent.pose);
            } else { // virtual end particles
                var parent = tree.points[point.parentIndex];
                point.pose = (parent.pose*2f - parent.parentPose);
                point.parentPose = parent.pose;
                point.desiredLengthToParent = math.distance(point.pose, point.parentPose);
            }
            tree.points[i] = point;
        }
    }

    private unsafe void VerletIntegrate(JiggleTreeStruct tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points[i];
            if (point.parentIndex == -1) {
                continue;
            }
            var parent = tree.points[point.parentIndex];
            
            var delta = point.position - point.lastPosition;
            var localSpaceVelocity = delta - (parent.position - parent.lastPosition);
            var velocity = delta - localSpaceVelocity;
            if (parent.parentIndex != -1) {
                point.workingPosition = point.position + velocity * (1f - parent.parameters.airDrag) + localSpaceVelocity * (1f - parent.parameters.drag) + gravity * parent.parameters.gravityMultiplier * (float)JiggleJobManager.FIXED_DELTA_TIME_SQUARED;
            } else {
                point.workingPosition = point.position + velocity * (1f-point.parameters.airDrag) + localSpaceVelocity * (1f-point.parameters.drag) + gravity * point.parameters.gravityMultiplier * (float)JiggleJobManager.FIXED_DELTA_TIME_SQUARED;
            }
            tree.points[i] = point;
        }
    }
    
    quaternion FromToRotation(float3 from, float3 to) {
        var axis = math.cross(from, to);
        var angle = math.acos(math.clamp(math.dot(from, to), -1f, 1f));
        return quaternion.AxisAngle(axis, angle);
    }
    
    float float3Angle(float3 a, float3 b) {
        return math.degrees(math.acos(math.clamp(math.dot(math.normalizesafe(a), math.normalizesafe(b)), -1f, 1f)));
    }

    private unsafe void Constrain(JiggleTreeStruct tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points[i];
            
            if (point.parentIndex == -1) {
                continue;
            }
            var parent = tree.points[point.parentIndex];
            #region Special root particle solve
            if (parent.parentIndex == -1) {
                var child = tree.points[point.childrenIndices[0]];
                point.desiredConstraint = point.workingPosition = math.lerp(point.workingPosition, point.pose, point.parameters.rootElasticity * point.parameters.rootElasticity);
                var head = point.pose;
                var tail = child.pose;
                var diffasdf = head - tail;
                parent.desiredConstraint = point.desiredConstraint + diffasdf;
                tree.points[point.parentIndex] = parent;
                tree.points[i] = point;
                continue;
            }
            #endregion
            
            #region Angle Constraint
            var parentAimPose = math.normalizesafe(point.parentPose - parent.parentPose);
            var parentAim = math.normalizesafe(parent.desiredConstraint - parent.parentPose);
            if (parent.parentIndex != -1) {
                var parentParent = tree.points[parent.parentIndex];
                parentAim = math.normalizesafe(parent.desiredConstraint - parentParent.desiredConstraint);
            }

            var currentLength = math.length(point.workingPosition - parent.desiredConstraint);
            var from_to_rot = FromToRotation(parentAimPose, parentAim);
            var current_pose_dir = math.normalizesafe(point.pose - point.parentPose);
            var constraintTarget = math.rotate(from_to_rot, (current_pose_dir * currentLength));

            var desiredPosition = parent.desiredConstraint + constraintTarget;

            
            var error = math.distance(point.workingPosition, parent.desiredConstraint + constraintTarget);
            error /= currentLength;
            error = math.min(error, 1.0f);
            error = math.pow(error, parent.parameters.elasticitySoften);
            point.desiredConstraint = math.lerp(point.workingPosition, desiredPosition, parent.parameters.angleElasticity * error);
            #endregion

            #region Collisions

            var colliderCount = testColliders.Length;
            for (int index=0; index < colliderCount; index++) {
                var colliderPosition = testColliders[index];
                var vectorFromCollider = point.desiredConstraint - colliderPosition;
                var distanceToCollider = math.length(vectorFromCollider);
                var minDistance = point.parameters.collisionRadius + 1f;
                if (distanceToCollider < minDistance) {
                    var correctionDir = math.normalizesafe(vectorFromCollider);
                    var correctionDistance = minDistance - distanceToCollider;
                    point.desiredConstraint += correctionDir * correctionDistance * (1f - 0.5f); // TODO: soften
                }
            }
            #endregion
            
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

                var oppo = math.sin(angleB_rad);
                float a = oppo == 0f ? 0f : b * math.sin(angleA_rad) / oppo;

                var correctionDir = math.normalizesafe(desiredPosition - point.desiredConstraint);
                var correctionDistance = math.length(desiredPosition - point.desiredConstraint);

                var angleCorrectionDistance = math.max(0f, correctionDistance - a);
                var angleCorrection = (correctionDir * angleCorrectionDistance) * (1f - point.parameters.angleLimitSoften);
                point.desiredConstraint += angleCorrection;
            }
            
            #region Length Constraint
            var length_elasticity = parent.parameters.lengthElasticity;
            var diff = point.desiredConstraint - parent.desiredConstraint;
            var dir = math.normalizesafe(diff);
            var forwardConstraint = math.lerp(point.desiredConstraint, parent.desiredConstraint + dir * point.desiredLengthToParent, length_elasticity);
            point.desiredConstraint = forwardConstraint;
            #endregion
            
            // TODO: Early out if collisions are disabled
            
            #region Back-propagated motion for collisions
            if (parent.parameters is { angleElasticity: 1f, lengthElasticity: 1f }) { // FIXME: Also check if collisions are disabled
                point.workingPosition = forwardConstraint;
                tree.points[i] = point;
                continue;
            }

            if (point.childenCount > 0) { // Back-propagated motion specifically for collision enabled chains
                var child = tree.points[point.childrenIndices[0]];
                var aim_pose = math.normalizesafe(child.pose - point.parentPose);
                var aim = math.normalizesafe(child.workingPosition - parent.workingPosition);
                var from_to_rot_also = FromToRotation(aim_pose, aim);
                var parent_to_self = math.normalizesafe(point.pose - point.parentPose);
                var real_length = math.length(point.workingPosition - parent.workingPosition);
                var targetPos = math.rotate(from_to_rot_also, (parent_to_self * real_length)) + parent.workingPosition;

                var error_also = math.length(point.workingPosition - targetPos);
                error_also /= real_length;
                error_also = math.min(error_also, 1.0f);
                error_also = math.pow(error_also, parent.parameters.elasticitySoften);
                var backward_constraint = math.lerp(point.workingPosition, targetPos, (parent.parameters.angleElasticity * parent.parameters.angleElasticity * error_also));

                var child_length_elasticity = point.parameters.lengthElasticity * point.parameters.lengthElasticity;

                var cdiff = backward_constraint - child.workingPosition;
                var cdir = math.normalizesafe(cdiff);
                backward_constraint = math.lerp(backward_constraint, child.workingPosition + cdir * child.desiredLengthToParent, child_length_elasticity * 0.5f);
                point.workingPosition = math.lerp(forwardConstraint, backward_constraint, 0.5f);
            } else {
                point.workingPosition = forwardConstraint;
            }
            tree.points[i] = point;
            #endregion
        }
    }

    private unsafe void FinishStep(JiggleTreeStruct tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points[i];
            point.lastPosition = point.position;
            point.position = point.workingPosition;
            tree.points[i] = point;
        }
    }

    private unsafe void ApplyPose(JiggleTreeStruct tree) {
        var rootSimulationPosition = tree.points[1].workingPosition;
        var rootPose = tree.GetInputPose(inputPoses, 1).position;
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points[i];
            if (point.childenCount <= 0) {
                continue;
            }

            var child = tree.points[point.childrenIndices[0]];

            var local_pose = point.pose;
            var local_child_pose = child.pose;
            var local_child_working_position = child.workingPosition;
            var local_working_position = point.workingPosition;

            if (point.parentIndex == -1) {
                continue;
            }

            float3 cachedAnimatedVector = new float3(0f);
            float3 simulatedVector = cachedAnimatedVector;

            if (point.childenCount <= 1) {
                cachedAnimatedVector = math.normalizesafe(local_child_pose - local_pose);
                simulatedVector = math.normalizesafe(local_child_working_position - local_working_position);
            } else {
                var cachedAnimatedVectorSum = new float3(0f);
                var simulatedVectorSum = cachedAnimatedVectorSum;
                for (var j = 0; j < point.childenCount; j++) {
                    var child_also = tree.points[point.childrenIndices[j]];
                    var local_child_pose_also = child_also.pose;
                    var local_child_working_position_also = child_also.workingPosition;
                    cachedAnimatedVectorSum += math.normalizesafe(local_child_pose_also - local_pose);
                    simulatedVectorSum += math.normalizesafe(local_child_working_position_also - local_working_position);
                }
                cachedAnimatedVector = math.normalizesafe(cachedAnimatedVectorSum * (1f / point.childenCount));
                simulatedVector = math.normalizesafe(simulatedVectorSum * (1f / point.childenCount));
            }
            
            var animPoseToPhysicsPose = math.slerp(quaternion.identity, FromToRotation(cachedAnimatedVector, simulatedVector), point.parameters.blend);

            tree.WriteOutputPose(outputPoses, i, new JiggleTransform() {
                isVirtual = !point.hasTransform,
                position = point.workingPosition,
                rotation = math.mul(animPoseToPhysicsPose, tree.GetInputPose(inputPoses, i).rotation),
            }, rootSimulationPosition - rootPose, rootSimulationPosition);
            tree.points[i] = point;
        }
    }


    public void Execute(int index) {
        var tree = jiggleTrees[index];
        Cache(tree);
        VerletIntegrate(tree);
        Constrain(tree);
        FinishStep(tree);
        ApplyPose(tree);
    }
}
