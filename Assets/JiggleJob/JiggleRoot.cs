using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

public class MonobehaviourHider {
    public class JiggleJobData {
        private Transform[] transforms;
        private TransformAccessArray transformAccess;
        private NativeArray<int> rootPointIndices;
        private NativeArray<int> rootTransformIndices;
        private NativeArray<Matrix4x4> transformMatrices;
        private NativeArray<JiggleBoneSimulatedPoint> simulatedPoints;
        private NativeArray<Matrix4x4> outputPoses;
        private NativeArray<Matrix4x4> restPoseMatrices;
        private NativeArray<Vector3> previousLocalPositions;
        private NativeArray<Quaternion> previousLocalRotations;
        private NativeArray<bool> animated;
        private NativeArray<Matrix4x4> previousPoses;
        private NativeArray<Matrix4x4> currentPoses;
        
        private NativeArray<Vector3> previousSimulatedRootOffsets;
        private NativeArray<Vector3> currentSimulatedRootOffsets;
    
        private NativeArray<Vector3> previousSimulatedRootPositions;
        private NativeArray<Vector3> currentSimulatedRootPositions;
    
        private NativeArray<Vector3> realRootPositions;

        private JiggleJobSimulate jiggleJobSimulate;
        private JiggleJobBulkRead jiggleJobBulkRead;
        private JiggleJobPose jiggleJobPose;
        
        private bool hasSimulateHandle = false;
        private JobHandle simulateHandle;
        
        private bool hasPoseHandle = false;
        private JobHandle poseHandle;
        
        private bool hasReadHandle = false;
        private JobHandle readHandle;

        public JiggleJobData(IList<JiggleTree> trees) {
            List<Transform> transforms = new List<Transform>();
            List<Matrix4x4> transformMatrices = new List<Matrix4x4>();
            List<JiggleBoneSimulatedPoint> simulatedPoints = new List<JiggleBoneSimulatedPoint>();
            List<Vector3> previousLocalPositions = new List<Vector3>();
            List<Quaternion> previousLocalRotations = new List<Quaternion>();
            List<int> rootPointIndices = new List<int>();
            List<int> rootTransformIndices = new List<int>();
            List<bool> animated = new List<bool>();
            foreach (var tree in trees) {
                var currentTransformIndexOffset = transforms.Count;
                var currentPointIndexOffset = simulatedPoints.Count;
                for (int i = 0; i < tree.points.Length; i++) {
                    var point = tree.points[i];
                    point.transformIndex += currentTransformIndexOffset;
                    point.parentIndex += currentPointIndexOffset;
                    unsafe {
                        for(int j = 0; j < point.childenCount; j++) {
                            point.childrenIndices[j] += currentPointIndexOffset;
                        }
                    }
                    animated.Add(point.animated);
                    simulatedPoints.Add(point);
                    rootPointIndices.Add(currentPointIndexOffset+1);
                }
                
                transforms.AddRange(tree.bones);
                for (int i = 0; i < transforms.Count; i++) {
                    transformMatrices.Add(transforms[i].localToWorldMatrix);
                    previousLocalPositions.Add(transforms[i].localPosition);
                    previousLocalRotations.Add(transforms[i].localRotation);
                    rootTransformIndices.Add(currentTransformIndexOffset);
                }
            }

            this.transforms = transforms.ToArray();
            transformAccess = new TransformAccessArray(this.transforms);
            var transformMatricesArray = transformMatrices.ToArray();
            this.transformMatrices = new NativeArray<Matrix4x4>(transformMatricesArray, Allocator.Persistent);
            this.simulatedPoints = new NativeArray<JiggleBoneSimulatedPoint>(simulatedPoints.ToArray(), Allocator.Persistent);
            outputPoses = new NativeArray<Matrix4x4>(transformMatricesArray, Allocator.Persistent);
            restPoseMatrices = new NativeArray<Matrix4x4>(transformMatricesArray, Allocator.Persistent);
            this.previousLocalPositions = new NativeArray<Vector3>(previousLocalPositions.ToArray(), Allocator.Persistent);
            this.previousLocalRotations = new NativeArray<Quaternion>(previousLocalRotations.ToArray(), Allocator.Persistent);
            this.animated = new NativeArray<bool>(animated.ToArray(), Allocator.Persistent);
            previousPoses = new NativeArray<Matrix4x4>(transformMatricesArray, Allocator.Persistent);
            currentPoses = new NativeArray<Matrix4x4>(transformMatricesArray, Allocator.Persistent);
            previousSimulatedRootOffsets = new NativeArray<Vector3>(this.simulatedPoints.Length, Allocator.Persistent);
            currentSimulatedRootOffsets = new NativeArray<Vector3>(this.simulatedPoints.Length, Allocator.Persistent);
            previousSimulatedRootPositions = new NativeArray<Vector3>(this.simulatedPoints.Length, Allocator.Persistent);
            currentSimulatedRootPositions = new NativeArray<Vector3>(this.simulatedPoints.Length, Allocator.Persistent);
            realRootPositions = new NativeArray<Vector3>(this.simulatedPoints.Length, Allocator.Persistent);
            this.rootPointIndices = new NativeArray<int>(rootPointIndices.ToArray(), Allocator.Persistent);
            this.rootTransformIndices = new NativeArray<int>(rootTransformIndices.ToArray(), Allocator.Persistent);

            jiggleJobSimulate = new JiggleJobSimulate() {
                gravity = Physics.gravity,
                transformMatrices = this.transformMatrices,
                simulatedPoints = this.simulatedPoints,
                outputPoses = outputPoses,
            };
            
            jiggleJobBulkRead = new JiggleJobBulkRead() {
                matrices = this.transformMatrices,
                restPoseMatrices = this.restPoseMatrices,
                previousLocalPositions = this.previousLocalPositions,
                previousLocalRotations = this.previousLocalRotations,
                animated = this.animated
            };

            jiggleJobPose = new JiggleJobPose() {
                currentSimulatedRootOffsets = currentSimulatedRootOffsets,
                previousSimulatedRootOffsets = previousSimulatedRootOffsets,
                currentSimulatedRootPositions = currentSimulatedRootPositions,
                previousSimulatedRootPositions = previousSimulatedRootPositions,
                realRootPositions = realRootPositions,
                previousSolve = previousPoses,
                currentSolve = currentPoses,
                previousLocalPositions = this.previousLocalPositions,
                currentTime = Time.timeAsDouble
            };
        }
        private void PushBack() {
            Profiler.BeginSample("JiggleTree.Pushback");
            if (hasPoseHandle) {
                poseHandle.Complete();
            }
            jiggleJobPose.previousTimeStamp = jiggleJobPose.timeStamp;
            (jiggleJobPose.currentSolve, jiggleJobPose.previousSolve) = (jiggleJobPose.previousSolve, jiggleJobPose.currentSolve);
            //jigglePoseJob.currentSolve.CopyTo(jigglePoseJob.previousSolve);
            jiggleJobPose.timeStamp = jiggleJobSimulate.timeStamp;
            jiggleJobSimulate.outputPoses.CopyTo(jiggleJobPose.currentSolve);

            jiggleJobPose.previousSimulatedRootOffsets.CopyFrom(jiggleJobPose.currentSimulatedRootOffsets);
            // TODO: JOBIFY THIS vvv
            for (int i = 0; i < simulatedPoints.Length; i++) {
                jiggleJobPose.currentSimulatedRootOffsets[i] = simulatedPoints[rootPointIndices[i]].position - simulatedPoints[rootPointIndices[i]].pose;
            }
            jiggleJobPose.previousSimulatedRootPositions.CopyFrom(jiggleJobPose.currentSimulatedRootPositions);
            for (int i = 0; i < simulatedPoints.Length; i++) {
                jiggleJobPose.currentSimulatedRootPositions[i] = simulatedPoints[rootPointIndices[i]].position;
            }
            // TODO: JOBIFY THIS ^^^
            Profiler.EndSample();
        }

        public void Simulate(double currentTime) {
            Profiler.BeginSample("JiggleTree.Simulate");
            Profiler.BeginSample("JiggleTree.CompletePreviousJob");
            if (hasSimulateHandle) {
                simulateHandle.Complete();
                //DrawDebug(jiggleJob);
                PushBack();
            }
            Profiler.EndSample();
            Profiler.BeginSample("JiggleTree.PrepareJobs");
            jiggleJobSimulate.timeStamp = currentTime;
            jiggleJobSimulate.gravity = Physics.gravity;
            Profiler.EndSample();
            Profiler.BeginSample("JiggleTree.ScheduleJobs");
            readHandle = hasPoseHandle ? jiggleJobBulkRead.Schedule(transformAccess, poseHandle) : jiggleJobBulkRead.Schedule(transformAccess);
            hasReadHandle = true;
            simulateHandle = jiggleJobSimulate.Schedule(readHandle);
            hasSimulateHandle = true;
            Profiler.EndSample();
            Profiler.EndSample();
        }
        
        public void SchedulePose() {
            Profiler.BeginSample("JiggleTree.SchedulePose");
            // TODO: THIS SUCKS, PLEASE JOBIFY OR SOMETHIN
            for(int i=0;i<transforms.Length; i++) {
                realRootPositions[i] = transforms[rootTransformIndices[i]].position;
            }
            jiggleJobPose.currentTime = Time.timeAsDouble;
            poseHandle = hasReadHandle ? jiggleJobPose.Schedule(transformAccess, readHandle) : jiggleJobPose.Schedule(transformAccess);
            hasPoseHandle = true;
            Profiler.EndSample();
        }

        public void CompletePose() {
            Profiler.BeginSample("JiggleTree.CompletePose");
            if (hasPoseHandle) {
                poseHandle.Complete();
            }
            Profiler.EndSample();
        }
    }

    [DisallowMultipleComponent]
    public class JiggleRoot : MonoBehaviour {
        private static List<JiggleRoot> jiggleRoots;
        private static bool dirty = false;
        private static List<JiggleTree> jiggleTrees;
        private static JiggleJobData jobData;

        public JiggleRig rig;
        public static void SetDirty() => dirty = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() {
            jiggleRoots = new List<JiggleRoot>();
            jiggleTrees = new List<JiggleTree>();
            dirty = true;
        }
        
        private static List<JiggleRoot> GetSuperRoots() {
            List<JiggleRoot> roots = new List<JiggleRoot>();
            List<JiggleRoot> parents = new List<JiggleRoot>();
            foreach (var root in jiggleRoots) {
                root.GetComponentsInParent(false, parents);
                if (parents.Count <= 1) { // No parent or only self
                    roots.Add(root);
                }
            }
            return roots;
        }

        private static void Visit(Transform t, List<Transform> transforms, List<JiggleBoneSimulatedPoint> points, int parentIndex, JiggleRoot lastRoot, out int newIndex) {
            if (t.TryGetComponent(out JiggleRoot newRoot)) {
                lastRoot = newRoot;
            }
            transforms.Add(t);
            var parameters = lastRoot.rig.GetJiggleBoneParameter(0.5f);
            if (lastRoot.rig.rootExcluded && t == lastRoot.transform) {
                parameters = new JiggleBoneParameters() {
                    angleElasticity = 1f,
                    lengthElasticity = 1f,
                    rootElasticity = 1f,
                    elasticitySoften = 0f
                };
            }
            points.Add(new JiggleBoneSimulatedPoint() { // Regular point
                position = t.position,
                lastPosition = t.position,
                childenCount = t.childCount == 0 ? 1 : t.childCount,
                parameters = parameters,
                parentIndex = parentIndex,
                transformIndex = transforms.Count-1,
                animated = true,
            });
            newIndex = points.Count - 1;
            
            if (t.childCount == 0) {
                points.Add(new JiggleBoneSimulatedPoint() { // virtual projected tip
                    position = t.position,
                    lastPosition = t.position,
                    childenCount = 0,
                    parameters = lastRoot.rig.GetJiggleBoneParameter(1f),
                    parentIndex = newIndex,
                    transformIndex = -1,
                    animated = false,
                });
            } else {
                for (int i = 0; i < t.childCount; i++) {
                    var child = t.GetChild(i);
                    Visit(child, transforms, points, newIndex, lastRoot, out int childIndex);
                    unsafe { // WEIRD
                        var record = points[newIndex];
                        record.childrenIndices[i] = childIndex;
                        points[newIndex] = record;
                    }
                }
            }
        }
        
        private static JiggleJobData GetJiggleJobData() {
            if (!dirty) {
                return jobData;
            }
            // TODO: Cleanup previous trees, or reuse them.
            var newJiggleTrees = new List<JiggleTree>();
            var superRoots = GetSuperRoots();
            foreach (var superRoot in superRoots) {
                List<Transform> jiggleTreeTransforms = new List<Transform>();
                List<JiggleBoneSimulatedPoint> jiggleTreePoints = new List<JiggleBoneSimulatedPoint>();
                // TODO: CHECK FOR DIRTY MEMBER, USE OLD JIGGLE TREE IF NOT DIRTY
                var found = false;
                foreach (var jiggleTree in jiggleTrees) {
                    if (jiggleTree.bones[0] == superRoot.transform) {
                        Debug.Log("FOUND EXISTING JIGGLE TREE");
                        if (!jiggleTree.dirty) {
                            newJiggleTrees.Add(jiggleTree);
                            found = true;
                            break;
                        }
                    }
                }
                if (found) continue;
                jiggleTreePoints.Add(new JiggleBoneSimulatedPoint() { // Back projected virtual root
                    childenCount = 1,
                    parameters = superRoot.rig.GetJiggleBoneParameter(0f),
                    parentIndex = -1,
                    transformIndex = -1,
                    animated = false,
                });
                Visit(superRoot.transform, jiggleTreeTransforms, jiggleTreePoints, 0, superRoot, out int childIndex);
                unsafe {
                    var rootPoint = jiggleTreePoints[0];
                    rootPoint.childrenIndices[0] = childIndex;
                    jiggleTreePoints[0] = rootPoint;
                }
                newJiggleTrees.Add(new JiggleTree(jiggleTreeTransforms.ToArray(), jiggleTreePoints.ToArray()));
                newJiggleTrees[^1].dirty = false;
            }

            jiggleTrees = newJiggleTrees;
            jobData = new JiggleJobData(jiggleTrees);
            dirty = false;
            return jobData;
        }
        public static void Simulate(double currentTime) {
            GetJiggleJobData().Simulate(currentTime);
        }
        public static void SchedulePose() {
            GetJiggleJobData().SchedulePose();
        }
        public static void CompletePose() {
            GetJiggleJobData().CompletePose();
        }
        
        private void OnEnable() {
            if (jiggleRoots.Contains(this)) {
                return;
            }
            jiggleRoots.Add(this);
            dirty = true;
        }
        private void OnDisable() {
            if (!jiggleRoots.Contains(this)) {
                return;
            }
            jiggleRoots.Remove(this);
            dirty = true;
        }
    }
}
