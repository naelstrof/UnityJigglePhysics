using System.Collections.Generic;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleJobs {
    private int treeCount;
    private int transformCount;
    
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
    
    public int GetTreeCount() => treeCount;
    public int GetTransformCount() => transformCount;
    private bool disposed = true;

    public JiggleJobs() {
        _memoryBus = new JiggleMemoryBus();
        jobSimulate = new JiggleJobSimulate();
        jobBulkTransformRead = new JiggleJobBulkTransformRead();
        jobBulkReadRoots = new JiggleJobBulkReadRoots();
        jobInterpolation = new JiggleJobInterpolation(Time.timeAsDouble);
        jobBulkColliderTransformRead = new JiggleJobBulkColliderTransformRead();
        jobTransformWrite = new JiggleJobTransformWrite();
    }
    
    public void Dispose() {
        if (disposed) {
            return;
        }
        if (hasHandleColliderRead) handleColliderRead.Complete();
        if (hasHandleBulkRead) handleBulkRead.Complete();
        if (hasHandleRootRead) handleRootRead.Complete();
        if (hasHandleSimulate) handleSimulate.Complete();
        if (hasHandleTransformWrite) handleTransformWrite.Complete();
        if (hasHandleInterpolate) handleInterpolate.Complete();
        treeCount = 0;
        transformCount = 0;
        jobBulkColliderTransformRead.Dispose();
        jobBulkTransformRead.Dispose();
        jobBulkReadRoots.Dispose();
        jobSimulate.Dispose();
        jobInterpolation.Dispose();
        jobTransformWrite.Dispose();
        _memoryBus.Dispose();
        disposed = true;
    }

    public JobHandle SchedulePoses(JobHandle dep) {
        handleRootRead = jobBulkReadRoots.ScheduleReadOnly(_memoryBus.transformRootAccessArray, 128, dep);
        hasHandleRootRead = true;
        
        jobInterpolation.currentTime = Time.timeAsDouble;
        handleInterpolate = jobInterpolation.ScheduleParallel(GetTransformCount(), 128, handleRootRead);
        
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
        
        // TODO: Use an external monobehavior to update gravity?
        var gravity = Physics.gravity;
        if (hasHandleSimulate) {
            handleSimulate.Complete();
            jobInterpolation.previousTimeStamp = jobInterpolation.timeStamp;
            jobInterpolation.timeStamp = jobSimulate.timeStamp;

            var tempPoses = jobInterpolation.previousPoses;
            jobInterpolation.previousPoses = jobInterpolation.currentPoses;
            jobInterpolation.currentPoses = jobSimulate.outputPoses;
            jobSimulate.outputPoses = tempPoses;
            
            var tempSimulatedRootOffset = jobInterpolation.previousSimulatedRootOffset;
            jobInterpolation.previousSimulatedRootOffset = jobInterpolation.currentSimulatedRootOffset;
            jobInterpolation.currentSimulatedRootOffset = jobSimulate.outputSimulatedRootOffset;
            jobSimulate.outputSimulatedRootOffset = tempSimulatedRootOffset;

            var tempSimulatedRootPosition = jobInterpolation.previousSimulatedRootPosition;
            jobInterpolation.previousSimulatedRootPosition = jobInterpolation.currentSimulatedRootPosition;
            jobInterpolation.currentSimulatedRootPosition = jobSimulate.outputSimulatedRootPosition;
            jobSimulate.outputSimulatedRootPosition = tempSimulatedRootPosition;
        }
        
        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(_memoryBus.transformAccessArray, 128);
        hasHandleBulkRead = true;

        handleColliderRead = jobBulkColliderTransformRead.ScheduleReadOnly(_memoryBus.colliderTransformAccessArray, 128);
        hasHandleColliderRead = true;
        
        var handle = JiggleTreeUtility.GetJiggleJobs().SchedulePoses(JobHandle.CombineDependencies(handleBulkRead, handleColliderRead));

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = currentTime;
        handleSimulate = jobSimulate.ScheduleParallel(GetTreeCount(), 1, JobHandle.CombineDependencies(handleBulkRead, handleColliderRead, handle));
        hasHandleSimulate = true;
    }
    
    public void Set(JiggleTree[] jiggleTrees, Transform[] colliderTransforms) {
        foreach(var tree in jiggleTrees) {
            if (tree.dirty) _memoryBus.Add(tree, new JiggleTreeStruct(tree.bones, tree.points));
        }

        for (var index = 0; index < colliderTransforms.Length; index++) {
            Debug.DrawLine(Vector3.zero, colliderTransforms[index].position, Color.red, 10f);
        }

        treeCount = _memoryBus.treeCount;
        transformCount = _memoryBus.transformCount;
        
        jobSimulate.jiggleTrees = _memoryBus.jiggleTreeStructs;
        jobSimulate.outputSimulatedRootOffset = _memoryBus.simulationOutputRootOffsets;
        jobSimulate.outputSimulatedRootPosition = _memoryBus.simulationOutputRootPositions;
        jobSimulate.outputPoses = _memoryBus.simulationOutputPoses;
        jobSimulate.inputPoses = _memoryBus.simulateInputPoses;
        jobSimulate.testColliders = _memoryBus.colliderPositions;
        
        jobBulkReadRoots.rootOutputPositions = _memoryBus.rootOutputPositions;
        
        jobBulkTransformRead.simulateInputPoses = _memoryBus.simulateInputPoses;
        jobBulkTransformRead.restPoseTransforms = _memoryBus.restPoseTransforms;
        jobBulkTransformRead.previousLocalTransforms = _memoryBus.previousLocalRestPoseTransforms;

        jobInterpolation.currentPoses = _memoryBus.interpolationCurrentPoses;
        jobInterpolation.previousPoses = _memoryBus.interpolationPreviousPoses;
        jobInterpolation.outputInterpolatedPoses = _memoryBus.interpolationOutputPoses;
        jobInterpolation.realRootPositions = _memoryBus.rootOutputPositions;
        jobInterpolation.previousSimulatedRootOffset = _memoryBus.interpolationPreviousRootOffsets;
        jobInterpolation.currentSimulatedRootOffset = _memoryBus.interpolationCurrentOffsets;
        jobInterpolation.previousSimulatedRootPosition = _memoryBus.interpolationPreviousRootPositions;
        jobInterpolation.currentSimulatedRootPosition = _memoryBus.interpolationCurrentPositions;
        
        jobBulkColliderTransformRead.positions = _memoryBus.colliderPositions;

        jobTransformWrite.inputInterpolatedPoses = _memoryBus.interpolationOutputPoses;
        jobTransformWrite.previousLocalPoses = _memoryBus.previousLocalRestPoseTransforms;
        
        disposed = false;
    }

    public void OnDrawGizmos() {
        if (hasHandleSimulate) {
            handleSimulate.Complete();
            jobSimulate.jiggleTrees.CopyTo(_memoryBus.jiggleTreeStructs);
            var count = _memoryBus.jiggleTreeStructs.Length;
            for (int i = 0; i < count; i++) {
                var tree = _memoryBus.jiggleTreeStructs[i];
                tree.OnGizmoDraw();
            }
        }
    }
}