using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

public class JiggleJobs {
    private JiggleMemoryBus _memoryBus;

    public JobHandle handlePersonalColliderRead;
    public bool hasHandlePersonalColliderRead;
    
    public JobHandle handleSceneColliderRead;
    public bool hasHandleSceneColliderRead;

    public JobHandle handleBulkRead;
    public bool hasHandleBulkRead;
    
    public JobHandle handleBulkReset;
    public bool hasHandleBulkReset;

    public JobHandle handleSimulate;
    public bool hasHandleSimulate;

    public JobHandle handleTransformWrite;
    public bool hasHandleTransformWrite;

    public JobHandle handleRootRead;
    public bool hasHandleRootRead;

    public JobHandle handleInterpolate;
    public bool hasHandleInterpolate;
    
    public JobHandle handleBroadPhaseClear;
    public bool hasHandleBroadPhaseClear;
    
    public JobHandle handleBroadPhase;
    public bool hasHandleBroadPhase;

    public JiggleJobBulkColliderTransformRead jobBulkPersonalColliderTransformRead;
    public JiggleJobBulkColliderTransformRead jobBulkSceneColliderTransformRead;
    public JiggleJobBulkTransformRead jobBulkTransformRead;
    public JiggleJobBulkTransformReset jobBulkTransformReset;
    public JiggleJobSimulate jobSimulate;
    public JiggleJobBulkReadRoots jobBulkReadRoots;
    public JiggleJobInterpolation jobInterpolation;
    public JiggleJobBroadPhaseClear jobBroadPhaseClear;
    public JiggleJobBroadPhase jobBroadPhase;

    public JiggleJobTransformWrite jobTransformWrite;

    public List<IntPtr> freePointers;

    public JiggleMemoryBus GetMemoryBus() => _memoryBus;

    public JiggleJobs(double timeAsDouble, float fixedDeltaTime) {
        _memoryBus = new JiggleMemoryBus();
        jobSimulate = new JiggleJobSimulate(_memoryBus, fixedDeltaTime);
        jobBulkTransformRead = new JiggleJobBulkTransformRead(_memoryBus);
        jobBulkTransformReset = new JiggleJobBulkTransformReset(_memoryBus);
        jobBulkReadRoots = new JiggleJobBulkReadRoots(_memoryBus);
        jobInterpolation = new JiggleJobInterpolation(_memoryBus, timeAsDouble, fixedDeltaTime);
        jobBulkPersonalColliderTransformRead = new JiggleJobBulkColliderTransformRead(_memoryBus.personalColliders);
        jobBulkSceneColliderTransformRead = new JiggleJobBulkColliderTransformRead(_memoryBus.sceneColliders);
        jobTransformWrite = new JiggleJobTransformWrite(_memoryBus);
        jobBroadPhase = new JiggleJobBroadPhase(_memoryBus);
        jobBroadPhaseClear = new JiggleJobBroadPhaseClear(_memoryBus);
        freePointers = new List<IntPtr>();
    }
    
    public void SetFixedDeltaTime(float fixedDeltaTime) {
        jobSimulate.SetFixedDeltaTime(fixedDeltaTime);
        jobInterpolation.SetFixedDeltaTime(fixedDeltaTime);
    }

    public void Dispose() {
        if (hasHandleBulkRead) handleBulkRead.Complete();
        if (hasHandleBulkReset) handleBulkReset.Complete();
        if (hasHandleRootRead) handleRootRead.Complete();
        if (hasHandleSimulate) handleSimulate.Complete();
        if (hasHandleTransformWrite) handleTransformWrite.Complete();
        if (hasHandleInterpolate) handleInterpolate.Complete();
        if (hasHandlePersonalColliderRead) handlePersonalColliderRead.Complete();
        if (hasHandleSceneColliderRead) handleSceneColliderRead.Complete();
        if (hasHandleBroadPhase) handleBroadPhase.Complete();
        if (hasHandleBroadPhaseClear) handleBroadPhaseClear.Complete();
        Free();
        _memoryBus.Dispose();
    }

    public JobHandle SchedulePoses(double timeAsDouble) {
        if (_memoryBus.transformCount == 0) {
            return default;
        }
        jobBulkTransformReset.UpdateArrays(_memoryBus);
        // TODO: This technically only needs to happen for root bones, as their positions are used for posing. Instead just doing a full reset because I'm lazy.
        if (hasHandleBulkReset) {
            handleBulkReset = jobBulkTransformReset.Schedule(_memoryBus.GetTransformAccessArray(), handleBulkReset);
        } else {
            handleBulkReset = jobBulkTransformReset.Schedule(_memoryBus.GetTransformAccessArray());
        }
        hasHandleBulkReset = true;

        return SchedulePoses(handleBulkReset, timeAsDouble);
    }

    private JobHandle SchedulePoses(JobHandle dep, double timeAsDouble) {
        if (_memoryBus.transformCount == 0) {
            return dep;
        }

        jobBulkReadRoots.UpdateArrays(_memoryBus);
        jobInterpolation.UpdateArrays(_memoryBus);
        jobTransformWrite.UpdateArrays(_memoryBus);

        handleRootRead = jobBulkReadRoots.ScheduleReadOnly(_memoryBus.GetTransformRootAccessArray(), 128, dep);
        hasHandleRootRead = true;

        jobInterpolation.currentTime = timeAsDouble;
        handleInterpolate = jobInterpolation.ScheduleParallel(_memoryBus.transformCount, 128, handleRootRead);
        hasHandleInterpolate = true;

        if (hasHandleBulkRead) {
            handleTransformWrite = jobTransformWrite.Schedule(_memoryBus.GetTransformAccessArray(),
                JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobTransformWrite.Schedule(_memoryBus.GetTransformAccessArray(), handleInterpolate);
        }

        hasHandleTransformWrite = true;
        return handleTransformWrite;
    }

    public void CompletePoses() {
        if (hasHandleTransformWrite) {
            handleTransformWrite.Complete();
        }
    }

    public void FreeOnComplete(IntPtr pointer) {
        freePointers.Add(pointer);
    }

    private void Free() {
        var freePointerCount = freePointers.Count;
        for (int i = 0; i < freePointerCount; i++) {
            unsafe {
                UnsafeUtility.Free((void*)freePointers[i], Allocator.Persistent);
            }
        }
        freePointers.Clear();
    }

    public void Simulate(double simulateTime, double realTime) {
        if (_memoryBus.transformCount == 0) {
            _memoryBus.CommitTrees();
            _memoryBus.CommitColliders();
            jobSimulate.UpdateArrays(_memoryBus);
            jobBulkTransformRead.UpdateArrays(_memoryBus);
            jobBulkReadRoots.UpdateArrays(_memoryBus);
            jobInterpolation.UpdateArrays(_memoryBus);
            jobBulkPersonalColliderTransformRead.UpdateArrays(_memoryBus.personalColliders);
            jobBulkSceneColliderTransformRead.UpdateArrays(_memoryBus.sceneColliders);
            jobTransformWrite.UpdateArrays(_memoryBus);
            jobBroadPhase.UpdateArrays(_memoryBus);
            jobBroadPhaseClear.UpdateArrays(_memoryBus);
            return;
        }

        // TODO: Use an external monobehavior to update gravity?
        var gravity = Physics.gravity;
        if (hasHandleSimulate) {
            handleSimulate.Complete();
            Free();
        }

        jobInterpolation.previousTimeStamp = jobInterpolation.timeStamp;
        jobInterpolation.timeStamp = jobSimulate.timeStamp;

        _memoryBus.RotateBuffers();

        _memoryBus.CommitTrees();
        _memoryBus.CommitColliders();

        jobSimulate.UpdateArrays(_memoryBus);
        jobBulkTransformReset.UpdateArrays(_memoryBus);
        jobBulkTransformRead.UpdateArrays(_memoryBus);
        jobBulkPersonalColliderTransformRead.UpdateArrays(_memoryBus.personalColliders);
        jobBulkSceneColliderTransformRead.UpdateArrays(_memoryBus.sceneColliders);
        jobBroadPhase.UpdateArrays(_memoryBus);
        jobBroadPhaseClear.UpdateArrays(_memoryBus);

        if (hasHandleSimulate) {
            handlePersonalColliderRead = jobBulkPersonalColliderTransformRead.ScheduleReadOnly( _memoryBus.GetPersonalColliderTransformAccessArray(), 128, handleSimulate);
            handleSceneColliderRead = jobBulkSceneColliderTransformRead.ScheduleReadOnly(_memoryBus.GetSceneColliderTransformAccessArray(), 128, handleSimulate);
        } else {
            handlePersonalColliderRead = jobBulkPersonalColliderTransformRead.ScheduleReadOnly( _memoryBus.GetPersonalColliderTransformAccessArray(), 128);
            handleSceneColliderRead = jobBulkSceneColliderTransformRead.ScheduleReadOnly(_memoryBus.GetSceneColliderTransformAccessArray(), 128);
        }

        hasHandlePersonalColliderRead = true;
        hasHandleSceneColliderRead = true;
        
        var colliderHandles = JobHandle.CombineDependencies(handlePersonalColliderRead, handleSceneColliderRead);
        
        handleBroadPhaseClear = jobBroadPhaseClear.Schedule();
        hasHandleBroadPhaseClear = true;
        handleBroadPhase = jobBroadPhase.Schedule(JobHandle.CombineDependencies(colliderHandles, handleBroadPhaseClear));
        hasHandleBroadPhase = true;
        
        handleBulkReset = jobBulkTransformReset.Schedule(_memoryBus.GetTransformAccessArray(), colliderHandles);
        hasHandleBulkReset = true;

        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(_memoryBus.GetTransformAccessArray(), 128, handleBulkReset);
        hasHandleBulkRead = true;

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = simulateTime;
        handleSimulate = jobSimulate.ScheduleParallel(_memoryBus.treeCount, 1, JobHandle.CombineDependencies(handleBroadPhase, handleBulkRead));
        hasHandleSimulate = true;
    }

    public void Add(JiggleTree tree) {
        _memoryBus.Add(tree);
    }

    public void Remove(JiggleTree tree) {
        _memoryBus.Remove(tree);
    }
    
    public void Add(JiggleColliderSerializable collider) {
        _memoryBus.Add(collider);
    }

    public void Remove(JiggleColliderSerializable collider) {
        _memoryBus.Remove(collider);
    }

    public void GetColliders(out JiggleCollider[] personalColliders, out JiggleCollider[] sceneColliders, out int personalColliderCount, out int sceneColliderCount) {
        _memoryBus.GetColliders(out personalColliders, out sceneColliders, out personalColliderCount, out sceneColliderCount);
    }

    public void OnDrawGizmos() {
        if (!hasHandleInterpolate || !hasHandleSimulate || !Application.isEditor) {
            return;
        }

        _memoryBus.GetResults(handleInterpolate, handleSimulate, out var poses, out var trees, out var poseCount, out var treeCount);
        for (int i = 0; i < treeCount; i++) {
            var tree = trees[i];
            for (int o = 0; o < tree.pointCount; o++) {
                unsafe {
                    var pose = poses[o+tree.transformIndexOffset];
                    var point = tree.points[o];
                    if (!pose.isVirtual) {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(pose.position, point.worldRadius);
                    } else {
                        //Gizmos.color = point.parentIndex == -1 ? Color.crimson : Color.magenta;
                        //Gizmos.DrawWireSphere(point.position, 0.025f);
                    }


                    if (point.childenCount != 0) {
                        for (int j = 0; j < point.childenCount; j++) {
                            var childPoint = tree.points[point.childrenIndices[j]];
                            var childPose = poses[point.childrenIndices[j] + tree.transformIndexOffset];
                            if (!childPose.isVirtual) {
                                Gizmos.color = Color.cyan;
                                Gizmos.DrawLine(pose.position, childPose.position);
                            } else {
                                //Gizmos.color = Color.magenta;
                                //Gizmos.DrawLine(point.position, childPoint.position);
                            }
                        }
                    }
                }
            }
        }
    }
}

}