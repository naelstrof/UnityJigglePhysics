using System.Collections.Generic;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

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
        _memoryBus.Dispose();
    }

    public JobHandle SchedulePoses(JobHandle dep) {
        if (_memoryBus.transformCount == 0) {
            return default;
        }
        jobBulkReadRoots.UpdateArrays(_memoryBus);
        jobInterpolation.UpdateArrays(_memoryBus);
        jobTransformWrite.UpdateArrays(_memoryBus);
        
        handleRootRead = jobBulkReadRoots.ScheduleReadOnly(_memoryBus.transformRootAccessArray, 128, dep);
        hasHandleRootRead = true;
        
        jobInterpolation.currentTime = Time.timeAsDouble;
        handleInterpolate = jobInterpolation.ScheduleParallel(_memoryBus.transformCount, 128, handleRootRead);
        
        if (hasHandleBulkRead) {
            handleTransformWrite = jobTransformWrite.Schedule(_memoryBus.transformAccessArray, JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobTransformWrite.Schedule(_memoryBus.transformAccessArray, handleInterpolate);
        }

        hasHandleTransformWrite = true;
        return handleTransformWrite;
    }

    public void CompletePoses() {
        if (hasHandleTransformWrite) {
            handleTransformWrite.Complete();
        }
    }
    
    public void Simulate(double currentTime) {
        if (_memoryBus.transformCount == 0) {
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
        
        jobSimulate.UpdateArrays(_memoryBus);
        jobBulkTransformRead.UpdateArrays(_memoryBus);
        
        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(_memoryBus.transformAccessArray, 128);
        hasHandleBulkRead = true;

        //handleColliderRead = jobBulkColliderTransformRead.ScheduleReadOnly(_memoryBus.colliderTransformAccessArray, 128);
        //hasHandleColliderRead = true;
        
        var handle = SchedulePoses(JobHandle.CombineDependencies(handleBulkRead, handleColliderRead));

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = currentTime;
        handleSimulate = jobSimulate.ScheduleParallel(_memoryBus.treeCount, 1, JobHandle.CombineDependencies(handleBulkRead, handleColliderRead, handle));
        hasHandleSimulate = true;
    }
    
    public void Set(JiggleTree[] jiggleTrees, Transform[] colliderTransforms) {
        // Gonna sweep everyone's feet, gotta sync
        if (hasHandleBulkRead) handleBulkRead.Complete();
        if (hasHandleRootRead) handleRootRead.Complete();
        if (hasHandleSimulate) handleSimulate.Complete();
        if (hasHandleTransformWrite) handleTransformWrite.Complete();
        if (hasHandleInterpolate) handleInterpolate.Complete();
        
        foreach(var tree in jiggleTrees) {
            if (tree.dirty && tree.valid) _memoryBus.Add(tree);
            if (!tree.valid) {
                Debug.Log("GUH???");
            }
            if (tree.dirty && !tree.valid) _memoryBus.Remove(tree);
        }

        for (var index = 0; index < colliderTransforms.Length; index++) {
            Debug.DrawLine(Vector3.zero, colliderTransforms[index].position, Color.red, 10f);
        }
    }

    public void OnDrawGizmos() {
        /*if (hasHandleSimulate) {
            handleSimulate.Complete();
            jobSimulate.jiggleTrees.CopyTo(_memoryBus.jiggleTreeStructs);
            var count = _memoryBus.jiggleTreeStructs.Length;
            for (int i = 0; i < count; i++) {
                var tree = _memoryBus.jiggleTreeStructs[i];
                tree.OnGizmoDraw();
            }
        }*/
    }
}