using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct JiggleJob : IJob {
    // TODO: doubles are strictly a bad way to track time, probably should be ints or longs.
    public double timeStamp;
    public Vector3 gravity;
    
    public NativeArray<Matrix4x4> transformMatrices;
    public NativeArray<JiggleBoneSimulatedPoint> simulatedPoints;
    public NativeArray<Vector3> debug;
    public NativeArray<Matrix4x4> output;

    private unsafe void Cache() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            if (point.parentIndex == -1) { // virtual root particles
                var child = simulatedPoints[point.childrenIndices[0]];
                var childChild = simulatedPoints[child.childrenIndices[0]];
                if (childChild.transformIndex == -1) { // edge case where it's a singular isolated root bone
                    point.pose = transformMatrices[child.transformIndex].GetPosition() - Vector3.up*0.25f;
                    point.parentPose = transformMatrices[child.transformIndex].GetPosition() - Vector3.up*0.5f;
                    point.desiredLengthToParent = 0.25f;
                } else {
                    var diff = transformMatrices[child.transformIndex].GetPosition() - transformMatrices[childChild.transformIndex].GetPosition();
                    point.pose = transformMatrices[child.transformIndex].GetPosition() + diff;
                    point.parentPose = transformMatrices[child.transformIndex].GetPosition() + diff*2f;
                    point.desiredLengthToParent = diff.magnitude;
                }
                point.desiredConstraint = point.pose;
                point.workingPosition = point.pose;
            } else if (point.transformIndex != -1) { // "real" particles
                var parent = simulatedPoints[point.parentIndex];
                point.pose = transformMatrices[point.transformIndex].GetPosition();
                point.parentPose = parent.pose;
                point.desiredLengthToParent = Vector3.Distance(point.pose, parent.pose);
            } else { // virtual end particles
                var parent = simulatedPoints[point.parentIndex];
                point.pose = (parent.pose*2f - parent.parentPose);
                point.parentPose = parent.pose;
                point.desiredLengthToParent = Vector3.Distance(point.pose, point.parentPose);
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
                point.desiredConstraint = point.workingPosition = Vector3.Lerp(point.workingPosition, point.pose, point.parameters.rootElasticity * point.parameters.rootElasticity);
                var head = transformMatrices[point.transformIndex].GetPosition();
                var tail = transformMatrices[child.transformIndex].GetPosition();
                var diffasdf = head - tail;
                parent.desiredConstraint = point.desiredConstraint + diffasdf;
                simulatedPoints[point.parentIndex] = parent;
                simulatedPoints[i] = point;
                continue;
            }
            #endregion

            #region Angle Constraint
            var parentAimPose = (point.parentPose - parent.parentPose).normalized;
            var parentAim = (parent.desiredConstraint - parent.parentPose).normalized;
            if (parent.parentIndex != -1) {
                var parentParent = simulatedPoints[parent.parentIndex];
                parentAim = (parent.desiredConstraint - parentParent.desiredConstraint).normalized;
            }

            var currentLength = (point.workingPosition - parent.desiredConstraint).magnitude;
            var from_to_rot = Quaternion.FromToRotation(parentAimPose, parentAim);
            var current_pose_dir = (point.pose - point.parentPose).normalized;
            var constraintTarget = from_to_rot * (current_pose_dir * currentLength);

            var error = (point.workingPosition - (parent.desiredConstraint + constraintTarget)).magnitude;
            error /= point.desiredLengthToParent;
            error = Mathf.Min(error, 1.0f);
            error = Mathf.Pow(error, parent.parameters.elasticitySoften * parent.parameters.elasticitySoften);
            point.desiredConstraint = Vector3.Lerp(point.workingPosition, parent.desiredConstraint + constraintTarget, parent.parameters.angleElasticity * parent.parameters.angleElasticity * error);
            #endregion
            
            // DO COLLISIONS HERE


            #region Length Constraint
            var length_elasticity = parent.parameters.lengthElasticity * parent.parameters.lengthElasticity;
            var diff = point.desiredConstraint - parent.desiredConstraint;
            var dir = diff.normalized;
            var forwardConstraint = Vector3.Lerp(point.desiredConstraint, parent.desiredConstraint + dir * point.desiredLengthToParent, length_elasticity);
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
                var aim_pose = (child.pose - point.parentPose).normalized;
                var aim = (child.workingPosition - parent.workingPosition).normalized;
                var from_to_rot_also = Quaternion.FromToRotation(aim_pose, aim);
                var parent_to_self = (point.pose - point.parentPose).normalized;
                var real_length = (point.workingPosition - parent.workingPosition).magnitude;
                var targetPos = (from_to_rot_also * (parent_to_self * real_length)) + parent.workingPosition;

                var error_also = (point.workingPosition - targetPos).magnitude;
                error_also /= point.desiredLengthToParent;
                error_also = Mathf.Min(error_also, 1.0f);
                error_also = Mathf.Pow(error_also, parent.parameters.elasticitySoften * parent.parameters.elasticitySoften);
                var backward_constraint = Vector3.Lerp(point.workingPosition, targetPos, (parent.parameters.angleElasticity * parent.parameters.angleElasticity * error_also));

                var child_length_elasticity = point.parameters.lengthElasticity * point.parameters.lengthElasticity;

                var cdiff = backward_constraint - child.workingPosition;
                var cdir = cdiff.normalized;
                backward_constraint = Vector3.Lerp(backward_constraint, child.workingPosition + cdir * child.desiredLengthToParent, child_length_elasticity * 0.5f);
                point.workingPosition = Vector3.Lerp(forwardConstraint, backward_constraint, 0.5f);
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

            Vector3 cachedAnimatedVector = Vector3.zero;
            Vector3 simulatedVector = Vector3.zero;

            if (point.childenCount == 1) {
                cachedAnimatedVector = (local_child_pose - local_pose).normalized;
                simulatedVector = (local_child_working_position - local_working_position).normalized;
            } else {
                var cachedAnimatedVectorSum = Vector3.zero;
                var simulatedVectorSum = Vector3.zero;
                for (var j = 0; j < point.childenCount; j++) {
                    var child_also = simulatedPoints[point.childrenIndices[j]];
                    var local_child_pose_also = child_also.pose;
                    var local_child_working_position_also = child_also.workingPosition;
                    cachedAnimatedVectorSum += (local_child_pose_also - local_pose).normalized;
                    simulatedVectorSum += (local_child_working_position_also - local_working_position).normalized;
                    cachedAnimatedVector = (cachedAnimatedVectorSum * (1f / point.childenCount)).normalized;
                }
                simulatedVector = (simulatedVectorSum * (1f / point.childenCount)).normalized;
            }

            var animPoseToPhysicsPose = Quaternion.Slerp(Quaternion.FromToRotation(cachedAnimatedVector, simulatedVector), Quaternion.identity, 1f - point.parameters.blend);

            var mat = transformMatrices[point.transformIndex];
            var rot = mat.rotation;
            var scale = mat.lossyScale;
            output[point.transformIndex] = Matrix4x4.TRS(point.workingPosition, rot*animPoseToPhysicsPose, scale);
            simulatedPoints[i] = point;
        }  
    }

    void UpdateDebug() {
        int simulatedPointCount = simulatedPoints.Length;
        for (int i = 0; i < simulatedPointCount; i++) {
            var point = simulatedPoints[i];
            debug[i] = point.position;
        }
    }
    
    public void Execute() {
        try {
            Cache();
            VerletIntegrate();
            Constrain();
            FinishStep();
            ApplyPose();
            UpdateDebug();
        } catch (Exception e) {
            Debug.LogException(e);
            throw;
        }
    }
}
