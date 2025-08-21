using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobSimulate : IJobFor {
    // TODO: doubles are strictly a bad way to track time, probably should be ints or longs.
    public double timeStamp;
    public float3 gravity;
    
    public int sceneColliderCount;

    [ReadOnly] [NativeDisableParallelForRestriction]
    public NativeArray<JiggleTransform> inputPoses;

    [NativeDisableParallelForRestriction] public NativeArray<PoseData> outputPoses;
    [NativeDisableParallelForRestriction] public NativeArray<JiggleCollider> personalColliders;
    [NativeDisableParallelForRestriction] public NativeArray<JiggleCollider> sceneColliders;
    
    [ReadOnly]
    public NativeHashMap<int2,JiggleGridCell> broadPhaseMap;

    public NativeArray<JiggleTreeJobData> jiggleTrees;

    public JiggleJobSimulate(JiggleMemoryBus bus) {
        inputPoses = bus.simulateInputPoses;
        jiggleTrees = bus.jiggleTreeStructs;
        outputPoses = bus.simulationOutputPoseData;
        personalColliders = bus.personalColliders;
        sceneColliders = bus.sceneColliders;
        timeStamp = Time.timeAsDouble;
        broadPhaseMap = bus.broadPhaseMap;
        gravity = Physics.gravity;
        sceneColliderCount = 0;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        inputPoses = bus.simulateInputPoses;
        jiggleTrees = bus.jiggleTreeStructs;
        outputPoses = bus.simulationOutputPoseData;
        personalColliders = bus.personalColliders;
        sceneColliders = bus.sceneColliders;
        sceneColliderCount = bus.sceneColliderCount;
        broadPhaseMap = bus.broadPhaseMap;
    }


    private unsafe void Cache(JiggleTreeJobData tree) {
        var lengthAccumulation = 0f;
        var maxColliderRadius = 0f;
        
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points+i;
            if (point->parentIndex == -1) {
                // virtual root particles
                var child = tree.points[point->childrenIndices[0]];
                var childChild = tree.points[child.childrenIndices[0]];
                var childPose = tree.GetInputPose(inputPoses, point->childrenIndices[0]);
                var childChildPose = tree.GetInputPose(inputPoses, child.childrenIndices[0]);
                if (!childChild.hasTransform) {
                    // edge case where it's a singular isolated root bone
                    point->pose = childPose.position - new float3(0f, 0.25f, 0f);
                    point->parentPose = childPose.position - new float3(0f, 0.5f, 0f);
                    point->desiredLengthToParent = 0.25f;
                } else {
                    var diff = childPose.position - childChildPose.position;
                    point->pose = childPose.position + diff;
                    point->parentPose = childPose.position + diff * 2f;
                    point->desiredLengthToParent = math.length(diff);
                }

                point->desiredConstraint = point->pose;
                point->workingPosition = point->pose;
            } else if (point->hasTransform) {
                // "real" particles
                var inputPose = tree.GetInputPose(inputPoses, i);
                var parent = tree.points+point->parentIndex;
                point->pose = inputPose.position;
                point->parentPose = parent->pose;
                point->desiredLengthToParent = math.distance(point->pose, parent->pose);
                var averagePointScale = (inputPose.scale.x + inputPose.scale.y + inputPose.scale.z) / 3f;
                point->worldRadius = point->parameters.collisionRadius * averagePointScale;
                maxColliderRadius = math.max(maxColliderRadius, point->worldRadius);
            } else {
                // virtual end particles
                var parent = tree.points+point->parentIndex;
                point->pose = (parent->pose * 2f - parent->parentPose);
                point->parentPose = parent->pose;
                point->desiredLengthToParent = math.distance(point->pose, point->parentPose);
            }
            lengthAccumulation += point->desiredLengthToParent;
        }

        const float extentsBuffer = 1.3f;
        tree.extents = (lengthAccumulation + maxColliderRadius)*extentsBuffer;
    }

    private unsafe void VerletIntegrate(JiggleTreeJobData tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points+i;
            if (point->parentIndex == -1) {
                continue;
            }

            var parent = tree.points+point->parentIndex;

            var delta = point->position - point->lastPosition;
            var localSpaceVelocity = delta - (parent->position - parent->lastPosition);
            var velocity = delta - localSpaceVelocity;
            if (parent->parentIndex != -1) {
                point->workingPosition = point->position + velocity * (1f - parent->parameters.airDrag) +
                                        localSpaceVelocity * (1f - parent->parameters.drag) + gravity *
                                        parent->parameters.gravityMultiplier *
                                        (float)JigglePhysics.FIXED_DELTA_TIME_SQUARED;
            } else {
                point->workingPosition = point->position + velocity * (1f - point->parameters.airDrag) +
                                        localSpaceVelocity * (1f - point->parameters.drag) + gravity *
                                        point->parameters.gravityMultiplier *
                                        (float)JigglePhysics.FIXED_DELTA_TIME_SQUARED;
            }
        }
    }

    quaternion FromToRotation(float3 from, float3 to) {
        var axis = math.cross(from, to);
        var angle = math.acos(math.clamp(math.dot(from, to), -1f, 1f));
        return quaternion.AxisAngle(axis, angle);
    }

    float float3Angle(float3 a, float3 b) {
        return math.degrees(math.acos(math.clamp(math.dot(math.normalizesafe(a, new float3(0,0,1)), math.normalizesafe(b, new float3(0,0,1))), -1f, 1f)));
    }
    

    private unsafe float3 DoDepenetration(JiggleSimulatedPoint* point, JiggleSimulatedPoint* otherPoint, JiggleCollider collider) {
        if (!collider.enabled || !point->hasTransform || !otherPoint->hasTransform) {
            return new float3(0f, 0f, 0f);
        }
        switch (collider.type) {
            case JiggleCollider.JiggleColliderType.Sphere:
                var colliderPosition = collider.localToWorldMatrix.c3.xyz;
                var boneClosestPoint = GetClosestPointOnLineSegment(
                        colliderPosition,
                        point->workingPosition,
                        otherPoint->workingPosition,
                        out var tValue
                        );
                var sphere_diff = boneClosestPoint - colliderPosition;
                var sphere_distance = math.length(sphere_diff);
                var depenetrationMagnitude = (collider.worldRadius + point->worldRadius) - sphere_distance;
                if (depenetrationMagnitude <= 0f) {
                    return new float3(0f, 0f, 0f);
                }
                var depenetrationDir = math.normalizesafe(sphere_diff, new float3(0, 0, 1));
                var depenetrationVector = depenetrationDir * depenetrationMagnitude;
                // TODO: find decent rigidbody solve instead of just pushing them both naively
                var hardness = 1f;
                var pValue = -(tValue - 0.5f * 2f);
                var ppValue = -pValue;
                pValue = math.pow(math.clamp(pValue+1f, 0f, 1f),0.5f);
                ppValue = math.pow(math.clamp(ppValue+1f, 0f, 1f),0.5f);
                if (!(otherPoint->parameters.angleElasticity == 1
                      && otherPoint->parameters.rootElasticity == 1
                      && otherPoint->parameters.lengthElasticity == 1)) {
                    //point->desiredConstraint = math.lerp(point->desiredConstraint, point->desiredConstraint + depenetrationVector, hardness);
                    return depenetrationVector * hardness * pValue;
                }
                break;
        }
        return new float3(0f, 0f, 0f);
    }
    
    private float3 GetClosestPointOnLineSegment(float3 inputPoint, float3 segmentPoint1, float3 segmentPoint2, out float tValue) {
        tValue = 0f;
        var segment = segmentPoint2 - segmentPoint1;
        var segmentLengthSq = math.dot(segment, segment);
        if (segmentLengthSq == 0f) {
            return segmentPoint1;
        }
        tValue = math.dot(inputPoint - segmentPoint1, segment) / segmentLengthSq;
        tValue = math.clamp(tValue, 0f, 1f);
        return segmentPoint1 + tValue * segment;
    }

    private unsafe void Constrain(JiggleTreeJobData tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points+i;

            if (point->parentIndex == -1) {
                continue;
            }

            var parent = tree.points+point->parentIndex;

            #region Special root particle solve

            if (parent->parentIndex == -1) {
                var child = tree.points[point->childrenIndices[0]];
                point->desiredConstraint = point->workingPosition = math.lerp(point->workingPosition, point->pose,
                    point->parameters.rootElasticity * point->parameters.rootElasticity);
                var head = point->pose;
                var tail = child.pose;
                var diffasdf = head - tail;
                parent->desiredConstraint = point->desiredConstraint + diffasdf;
                continue;
            }

            #endregion

            #region Collisions
            
            // TODO: to convert a float to a grid location we just cast, but this always rounds towards zero. Probably should be a math.round()
            int extentRange = (int)tree.extents;
            for (int x = -extentRange; x < extentRange; x++) {
                for (int y = -extentRange; y < extentRange; y++) {
                    if (broadPhaseMap.TryGetValue(JiggleGridCell.GetKey(tree.points[0].position)+new int2(x,y), out var gridCell)) {
                        for (int index = 0; index < gridCell.count; index++) {
                            var collisionDepenetration = new float3(0f, 0f, 0f);
                            collisionDepenetration = DoDepenetration(point, parent, sceneColliders[gridCell.colliderIndices[index]]);
                            var maxDepenetrationMagnitude = math.length(collisionDepenetration);
                            for (int childIndex = 0; childIndex < point->childenCount; childIndex++) {
                                var child = tree.points + point->childrenIndices[childIndex];
                                var newCollisionDepenetration = DoDepenetration(point, child, sceneColliders[gridCell.colliderIndices[index]]);
                                maxDepenetrationMagnitude = math.max(maxDepenetrationMagnitude, math.length(newCollisionDepenetration));
                                collisionDepenetration += newCollisionDepenetration;
                                collisionDepenetration = math.normalizesafe(collisionDepenetration, new float3(0,0,1)) * maxDepenetrationMagnitude;
                            }
                            point->workingPosition += collisionDepenetration;
                        }
                    }
                }
            }

            for (int index = (int)tree.colliderIndexOffset; index < tree.colliderCount; index++) {
                var collisionDepenetration = new float3(0f, 0f, 0f);
                collisionDepenetration = DoDepenetration(point, parent, personalColliders[index]);
                var maxDepenetrationMagnitude = math.length(collisionDepenetration);
                for (int childIndex = 0; childIndex < point->childenCount; childIndex++) {
                    var child = tree.points + point->childrenIndices[childIndex];
                    var newCollisionDepenetration = DoDepenetration(point, child, personalColliders[index]);
                    maxDepenetrationMagnitude = math.max(maxDepenetrationMagnitude, math.length(newCollisionDepenetration));
                    collisionDepenetration += newCollisionDepenetration;
                    collisionDepenetration = math.normalizesafe(collisionDepenetration, new float3(0,0,1)) * maxDepenetrationMagnitude;
                }
                point->workingPosition += collisionDepenetration;
            }

            #endregion
            
            #region Angle Constraint
            
            var length_elasticity = parent->parameters.lengthElasticity;
            var parentAimPose = math.normalizesafe(point->parentPose - parent->parentPose, new float3(0,0,1));
            var parentAim = math.normalizesafe(parent->desiredConstraint - parent->parentPose, new float3(0,0,1));
            if (parent->parentIndex != -1) {
                var parentParent = tree.points+parent->parentIndex;
                parentAim = math.normalizesafe(parent->desiredConstraint - parentParent->desiredConstraint, new float3(0,0,1));
            }

            var currentLength = math.length(point->workingPosition - parent->desiredConstraint);
            var from_to_rot = FromToRotation(parentAimPose, parentAim);
            var constraintTarget = math.rotate(from_to_rot, point->pose - point->parentPose);

            var desiredPosition = parent->desiredConstraint + constraintTarget;

            var error = math.distance(point->workingPosition, desiredPosition);
            if (currentLength != 0) {
                error /= currentLength;
            }
            error = math.min(error, 1.0f);
            error = math.pow(error, parent->parameters.elasticitySoften);
            point->desiredConstraint = math.lerp(point->workingPosition, desiredPosition,
                parent->parameters.angleElasticity * error);

            var offsetFromParent = point->desiredConstraint - parent->desiredConstraint;
            var offsetFromParentNormalized = math.normalizesafe(offsetFromParent, new float3(0, 0, 1));
            point->desiredConstraint = parent->desiredConstraint + math.lerp(offsetFromParent, offsetFromParentNormalized * point->desiredLengthToParent, length_elasticity);

            var forwardConstraint = point->desiredConstraint;
            
            #endregion
            
            if (point->parameters.angleLimited) {
                // --- Angle Limit Constraint
                float angleA_deg = point->parameters.angleLimit;
                // TODO: This should be radians instead of degrees
                float angleC_deg = float3Angle(
                    point->desiredConstraint - desiredPosition,
                    parent->desiredConstraint - desiredPosition
                ); // known included angle C

                float b = math.distance(point->parentPose, desiredPosition); // known side opposite angle B

                float angleB_deg = 180f - angleA_deg - angleC_deg;

                float angleA_rad = angleA_deg * Mathf.Deg2Rad;
                float angleB_rad = angleB_deg * Mathf.Deg2Rad;

                var oppo = math.sin(angleB_rad);
                float a = oppo == 0f ? 0f : b * math.sin(angleA_rad) / oppo;

                var correctionDir = math.normalizesafe(desiredPosition - point->desiredConstraint, new float3(0,0,1));
                var correctionDistance = math.length(desiredPosition - point->desiredConstraint);

                var angleCorrectionDistance = math.max(0f, correctionDistance - a);
                var angleCorrection =
                    (correctionDir * angleCorrectionDistance) * (1f - point->parameters.angleLimitSoften);
                point->desiredConstraint += angleCorrection;
            }

            // TODO: Early out if collisions are disabled (or don't for a more accurate solve)

            //point->workingPosition = forwardConstraint;
            //continue;

            #region Back-propagated motion for collisions

            if (parent->parameters is { angleElasticity: 1f, lengthElasticity: 1f }) {
                // FIXME: Also check if collisions are disabled
                point->workingPosition = forwardConstraint;
                continue;
            }

            if (point->childenCount > 0) {
                // Back-propagated motion specifically for collision enabled chains
                var child = tree.points[point->childrenIndices[0]];
                var aim_pose = math.normalizesafe(child.pose - point->parentPose, new float3(0,0,1));
                var aim = math.normalizesafe(child.workingPosition - parent->workingPosition, new float3(0,0,1));
                var from_to_rot_also = FromToRotation(aim_pose, aim);
                var parent_to_self = math.normalizesafe(point->pose - point->parentPose);
                var real_length = math.length(point->workingPosition - parent->workingPosition);
                var targetPos = math.rotate(from_to_rot_also, (parent_to_self * real_length)) + parent->workingPosition;

                var error_also = math.length(point->workingPosition - targetPos);
                if (real_length != 0) {
                    error_also /= real_length;
                }
                error_also = math.min(error_also, 1.0f);
                error_also = math.pow(error_also, parent->parameters.elasticitySoften);
                var backward_constraint = math.lerp(point->workingPosition, targetPos,
                    (parent->parameters.angleElasticity * parent->parameters.angleElasticity * error_also));

                var child_length_elasticity = point->parameters.lengthElasticity * point->parameters.lengthElasticity;

                var cdiff = backward_constraint - child.workingPosition;
                var cdir = math.normalizesafe(cdiff);
                backward_constraint = math.lerp(backward_constraint,
                    child.workingPosition + cdir * child.desiredLengthToParent, child_length_elasticity * 0.5f);
                var notFoldedBack = 0f;//math.clamp(-math.dot(math.normalizesafe(point->workingPosition - parent->workingPosition), aim)*2f,0f,1f);
                point->workingPosition = math.lerp(forwardConstraint, backward_constraint, 0.5f * notFoldedBack);
            } else {
                point->workingPosition = forwardConstraint;
            }

            #endregion
        }
    }

    private unsafe void FinishStep(JiggleTreeJobData tree) {
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points+i;
            point->lastPosition = point->position;
            point->position = point->workingPosition;
        }
    }

    private unsafe void ApplyPose(JiggleTreeJobData tree) {
        var rootSimulationPosition = tree.points[1].workingPosition;
        var rootPose = tree.GetInputPose(inputPoses, 1).position;
        for (int i = 0; i < tree.pointCount; i++) {
            var point = tree.points+i;
            if (point->childenCount <= 0) {
                continue;
            }

            var child = tree.points[point->childrenIndices[0]];

            var local_pose = point->pose;
            var local_child_pose = child.pose;
            var local_child_working_position = child.workingPosition;
            var local_working_position = point->workingPosition;

            if (point->parentIndex == -1) {
                continue;
            }

            float3 cachedAnimatedVector = new float3(0f);
            float3 simulatedVector = cachedAnimatedVector;

            if (point->childenCount <= 1) {
                cachedAnimatedVector = math.normalizesafe(local_child_pose - local_pose, new float3(0,0,1));
                simulatedVector = math.normalizesafe(local_child_working_position - local_working_position, new float3(0,0,1));
            } else {
                var cachedAnimatedVectorSum = new float3(0f);
                var simulatedVectorSum = cachedAnimatedVectorSum;
                for (var j = 0; j < point->childenCount; j++) {
                    var child_also = tree.points[point->childrenIndices[j]];
                    var local_child_pose_also = child_also.pose;
                    var local_child_working_position_also = child_also.workingPosition;
                    cachedAnimatedVectorSum += math.normalizesafe(local_child_pose_also - local_pose, new float3(0,0,1));
                    simulatedVectorSum +=
                        math.normalizesafe(local_child_working_position_also - local_working_position, new float3(0,0,1));
                }

                cachedAnimatedVector = math.normalizesafe(cachedAnimatedVectorSum * (1f / point->childenCount), new float3(0,0,1));
                simulatedVector = math.normalizesafe(simulatedVectorSum * (1f / point->childenCount), new float3(0,0,1));
            }

            var animPoseToPhysicsPose = math.slerp(quaternion.identity,
                FromToRotation(cachedAnimatedVector, simulatedVector), point->parameters.blend);

            var transform = new JiggleTransform() {
                isVirtual = !point->hasTransform,
                position = point->workingPosition,
                rotation = math.mul(animPoseToPhysicsPose, tree.GetInputPose(inputPoses, i).rotation),
            };
            tree.WriteOutputPose(outputPoses, i, transform, rootSimulationPosition - rootPose, rootSimulationPosition);
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

}