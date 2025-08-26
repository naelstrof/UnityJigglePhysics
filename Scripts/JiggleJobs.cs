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


    public JiggleJobs() {
        _memoryBus = new JiggleMemoryBus();
        jobSimulate = new JiggleJobSimulate(_memoryBus);
        jobBulkTransformRead = new JiggleJobBulkTransformRead(_memoryBus);
        jobBulkTransformReset = new JiggleJobBulkTransformReset(_memoryBus);
        jobBulkReadRoots = new JiggleJobBulkReadRoots(_memoryBus);
        jobInterpolation = new JiggleJobInterpolation(_memoryBus, Time.timeAsDouble);
        jobBulkPersonalColliderTransformRead = new JiggleJobBulkColliderTransformRead(_memoryBus.personalColliders);
        jobBulkSceneColliderTransformRead = new JiggleJobBulkColliderTransformRead(_memoryBus.sceneColliders);
        jobTransformWrite = new JiggleJobTransformWrite(_memoryBus);
        jobBroadPhase = new JiggleJobBroadPhase(_memoryBus);
        jobBroadPhaseClear = new JiggleJobBroadPhaseClear(_memoryBus);
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
        _memoryBus.Dispose();
    }

    public JobHandle SchedulePoses(double timeAsDouble) {
        if (_memoryBus.transformCount == 0) {
            return default;
        }
        jobBulkTransformReset.UpdateArrays(_memoryBus);
        // TODO: This technically only needs to happen for root bones, as their positions are used for posing. Instead just doing a full reset because I'm lazy.
        handleBulkReset = jobBulkTransformReset.Schedule(_memoryBus.GetTransformAccessArray());
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
        
        handleBulkReset = jobBulkTransformReset.Schedule(_memoryBus.GetTransformAccessArray());
        hasHandleBulkReset = true;

        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(_memoryBus.GetTransformAccessArray(), 128, handleBulkReset);
        hasHandleBulkRead = true;

        handlePersonalColliderRead = jobBulkPersonalColliderTransformRead.ScheduleReadOnly(_memoryBus.GetPersonalColliderTransformAccessArray(), 128);
        hasHandlePersonalColliderRead = true;
        
        handleSceneColliderRead = jobBulkSceneColliderTransformRead.ScheduleReadOnly(_memoryBus.GetSceneColliderTransformAccessArray(), 128);
        hasHandleSceneColliderRead = true;

        var handle = SchedulePoses(handleBulkReset, realTime);
        
        var colliderHandles = JobHandle.CombineDependencies(handlePersonalColliderRead, handleSceneColliderRead);
        
        var broadPhaseClearHandle = jobBroadPhaseClear.Schedule();
        var broadPhaseHandle = jobBroadPhase.Schedule(JobHandle.CombineDependencies(colliderHandles, broadPhaseClearHandle));

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = simulateTime;
        handleSimulate = jobSimulate.ScheduleParallel(_memoryBus.treeCount, 1,
            JobHandle.CombineDependencies(broadPhaseHandle, colliderHandles, handle));
        hasHandleSimulate = true;
    }

    public void Add(JiggleTree tree) {
        _memoryBus.Add(tree);
    }

    public void Remove(JiggleTree tree) {
        _memoryBus.Remove(tree.rootID);
    }
    
    public void Add(JiggleColliderSerializable collider) {
        _memoryBus.Add(collider);
    }

    public void Remove(JiggleColliderSerializable collider) {
        _memoryBus.Remove(collider);
    }

    public void OnDrawGizmos() {
        if (!hasHandleSimulate || !Application.isEditor) return;
        handleSimulate.Complete();
        jobSimulate.jiggleTrees.CopyTo(_memoryBus.jiggleTreeStructs);
        for (int i = 0; i < _memoryBus.treeCount; i++) {
            var tree = _memoryBus.jiggleTreeStructs[i];
            tree.OnGizmoDraw();
        }
    }
}

}