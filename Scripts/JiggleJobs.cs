using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

public class JiggleJobs {
    private JiggleMemoryBus _memoryBus;

    public JobHandle handleColliderRead;
    public bool hasHandleColliderRead;

    public JobHandle handleBulkRead;
    public bool hasHandleBulkRead;

    public JobHandle handleSimulate;
    public bool hasHandleSimulate;

    public JobHandle handleTransformWrite;
    public bool hasHandleTransformWrite;

    public JobHandle handleRootRead;
    public bool hasHandleRootRead;

    public JobHandle handleInterpolate;
    public bool hasHandleInterpolate;

    public JiggleJobBulkColliderTransformRead jobBulkColliderTransformRead;
    public JiggleJobBulkTransformRead jobBulkTransformRead;
    public JiggleJobSimulate jobSimulate;
    public JiggleJobBulkReadRoots jobBulkReadRoots;
    public JiggleJobInterpolation jobInterpolation;

    public JiggleJobTransformWrite jobTransformWrite;


    public JiggleJobs() {
        _memoryBus = new JiggleMemoryBus();
        jobSimulate = new JiggleJobSimulate(_memoryBus);
        jobBulkTransformRead = new JiggleJobBulkTransformRead(_memoryBus);
        jobBulkReadRoots = new JiggleJobBulkReadRoots(_memoryBus);
        jobInterpolation = new JiggleJobInterpolation(_memoryBus, Time.timeAsDouble);
        jobBulkColliderTransformRead = new JiggleJobBulkColliderTransformRead(_memoryBus);
        jobTransformWrite = new JiggleJobTransformWrite(_memoryBus);
    }

    public void Dispose() {
        if (hasHandleBulkRead) handleBulkRead.Complete();
        if (hasHandleRootRead) handleRootRead.Complete();
        if (hasHandleSimulate) handleSimulate.Complete();
        if (hasHandleTransformWrite) handleTransformWrite.Complete();
        if (hasHandleInterpolate) handleInterpolate.Complete();
        if (hasHandleColliderRead) handleColliderRead.Complete();
        _memoryBus.Dispose();
    }

    public JobHandle SchedulePoses(JobHandle dep, double timeAsDouble) {
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
            _memoryBus.Commit();
            jobSimulate.UpdateArrays(_memoryBus);
            jobBulkTransformRead.UpdateArrays(_memoryBus);
            jobBulkReadRoots.UpdateArrays(_memoryBus);
            jobInterpolation.UpdateArrays(_memoryBus);
            jobBulkColliderTransformRead.UpdateArrays(_memoryBus);
            jobTransformWrite.UpdateArrays(_memoryBus);
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

        _memoryBus.Commit();

        jobSimulate.UpdateArrays(_memoryBus);
        jobBulkTransformRead.UpdateArrays(_memoryBus);

        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(_memoryBus.GetTransformAccessArray(), 128);
        hasHandleBulkRead = true;

        handleColliderRead =
            jobBulkColliderTransformRead.ScheduleReadOnly(_memoryBus.GetColliderTransformAccessArray(), 128);
        hasHandleColliderRead = true;

        var handle = SchedulePoses(JobHandle.CombineDependencies(handleBulkRead, handleColliderRead), realTime);

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = simulateTime;
        handleSimulate = jobSimulate.ScheduleParallel(_memoryBus.treeCount, 1,
            JobHandle.CombineDependencies(handleBulkRead, handleColliderRead, handle));
        hasHandleSimulate = true;
    }

    public void Add(JiggleTree tree) {
        _memoryBus.Add(tree);
    }

    public void Remove(JiggleTree tree) {
        _memoryBus.Remove(tree.rootID);
    }

    public void OnDrawGizmos() {
        //if (hasHandleSimulate) {
        //    handleSimulate.Complete();
        //    jobSimulate.jiggleTrees.CopyTo(_memoryBus.jiggleTreeStructsArray);
        //    var count = _memoryBus.jiggleTreeStructsArray.Length;
        //     for (int i = 0; i < count; i++) {
        //         var tree = _memoryBus.jiggleTreeStructsArray[i];
        //         tree.OnGizmoDraw();
        //     }
        // }
    }
}

}