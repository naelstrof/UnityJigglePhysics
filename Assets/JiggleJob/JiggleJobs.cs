using System.Collections.Generic;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleJobs {
    private int treeCount;
    private int transformCount;
    
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
    
    public JiggleJobBulkTransformRead jobBulkTransformRead;
    public JiggleJobSimulate jobSimulate;
    public JiggleJobBulkReadRoots jobBulkReadRoots;
    public JiggleJobInterpolation jobInterpolation;
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    
    public JiggleJobTransformWrite jobTransformWrite;

    public int GetTreeCount() => treeCount;
    public int GetTransformCount() => transformCount;
    public void Dispose() {
        if (hasHandleBulkRead) handleBulkRead.Complete();
        if (hasHandleRootRead) handleRootRead.Complete();
        if (hasHandleSimulate) handleSimulate.Complete();
        if (hasHandleTransformWrite) handleTransformWrite.Complete();
        if (hasHandleInterpolate) handleInterpolate.Complete();
        treeCount = 0;
        transformCount = 0;
        jobBulkTransformRead.Dispose();
        jobBulkReadRoots.Dispose();
        jobSimulate.Dispose();
        jobInterpolation.Dispose();
        transformAccessArray.Dispose();
        jobTransformWrite.Dispose();
    }

    public void SchedulePoses() {
        handleRootRead = jobBulkReadRoots.ScheduleReadOnly(transformRootAccessArray, 128);
        hasHandleRootRead = true;
        
        jobInterpolation.currentTime = Time.timeAsDouble;
        handleInterpolate = jobInterpolation.ScheduleParallel(GetTransformCount(), 128, handleRootRead);
        
        if (hasHandleBulkRead) {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, handleInterpolate);
        }

        hasHandleTransformWrite = true;
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
        
        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(transformAccessArray, 128);
        hasHandleBulkRead = true;

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = currentTime;
        handleSimulate = jobSimulate.ScheduleParallel(GetTreeCount(), 1, handleBulkRead);
        hasHandleSimulate = true;
    }
    
    public void Set(JiggleTree[] jiggleTrees) {
        List<JiggleTreeStruct> structs = new List<JiggleTreeStruct>(jiggleTrees.Length);
        List<Transform> jiggledTransforms = new  List<Transform>(jiggleTrees.Length);
        List<JiggleTransform> jiggleTransformPoses = new List<JiggleTransform>(jiggleTrees.Length);
        List<JiggleTransform> jiggleTransformLocalPoses = new List<JiggleTransform>(jiggleTrees.Length);
        foreach(var tree in jiggleTrees) {
            structs.Add(tree.GetStructAndUpdateLists(jiggledTransforms, jiggleTransformPoses, jiggleTransformLocalPoses));
        }

        var jiggleTrasnformRootTransforms = new List<Transform>();
        foreach (var jiggleTree in jiggleTrees) {
            foreach (var t in jiggleTree.points) {
                jiggleTrasnformRootTransforms.Add(jiggleTree.bones[0]);
            }
        }
        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
        transformRootAccessArray = new TransformAccessArray(jiggleTrasnformRootTransforms.ToArray());
        
        treeCount = structs.Count;
        transformCount = jiggledTransforms.Count;
        
        var jiggledTransformPosesArray = jiggleTransformPoses.ToArray();
        
        jobSimulate.Dispose();
        jobSimulate = new JiggleJobSimulate(structs.ToArray(), jiggledTransformPosesArray);
        
        jobBulkReadRoots.Dispose();
        jobBulkReadRoots = new JiggleJobBulkReadRoots(jiggledTransformPosesArray);
        
        jobInterpolation.Dispose();
        jobInterpolation = new JiggleJobInterpolation(jiggledTransformPosesArray, jobBulkReadRoots);

        jobBulkTransformRead.Dispose();
        jobBulkTransformRead = new JiggleJobBulkTransformRead(jobSimulate, jiggleTransformLocalPoses.ToArray());
        
        jobTransformWrite = new JiggleJobTransformWrite(jobBulkTransformRead, jobInterpolation);

        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        transformAccessArray = new TransformAccessArray(jiggledTransforms.ToArray());
    }
}