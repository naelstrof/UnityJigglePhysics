using System.Collections.Generic;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

public class JiggleJobs {
    private int treeCount;
    private int transformCount;
    
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
    public TransformAccessArray transformAccessArray;
    public TransformAccessArray transformRootAccessArray;
    public TransformAccessArray colliderTransformAccessArray;

    public JiggleJobTransformWrite jobTransformWrite;

    public int GetTreeCount() => treeCount;
    public int GetTransformCount() => transformCount;
    public void Dispose() {
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
        transformAccessArray.Dispose();
        jobTransformWrite.Dispose();
    }

    public JobHandle SchedulePoses(JobHandle dep) {
        handleRootRead = jobBulkReadRoots.ScheduleReadOnly(transformRootAccessArray, 128, dep);
        hasHandleRootRead = true;
        
        jobInterpolation.currentTime = Time.timeAsDouble;
        handleInterpolate = jobInterpolation.ScheduleParallel(GetTransformCount(), 128, handleRootRead);
        
        if (hasHandleBulkRead) {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, JobHandle.CombineDependencies(handleInterpolate, handleBulkRead));
        } else {
            handleTransformWrite = jobTransformWrite.Schedule(transformAccessArray, handleInterpolate);
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
        
        handleBulkRead = jobBulkTransformRead.ScheduleReadOnly(transformAccessArray, 128);
        hasHandleBulkRead = true;

        handleColliderRead = jobBulkColliderTransformRead.ScheduleReadOnly(colliderTransformAccessArray, 128);
        hasHandleColliderRead = true;
        
        var handle = JiggleRoot.GetJiggleJobs().SchedulePoses(JobHandle.CombineDependencies(handleBulkRead, handleColliderRead));

        jobSimulate.gravity = gravity;
        jobSimulate.timeStamp = currentTime;
        handleSimulate = jobSimulate.ScheduleParallel(GetTreeCount(), 1, JobHandle.CombineDependencies(handleBulkRead, handleColliderRead, handle));
        hasHandleSimulate = true;
    }
    
    public void Set(JiggleTree[] jiggleTrees, Transform[] colliderTransforms) {
        List<JiggleTreeStruct> structs = new List<JiggleTreeStruct>(jiggleTrees.Length);
        List<Transform> jiggledTransforms = new  List<Transform>(jiggleTrees.Length);
        List<Transform> colliderTransformList = new List<Transform>(colliderTransforms.Length);
        List<JiggleTransform> jiggleTransformPoses = new List<JiggleTransform>(jiggleTrees.Length);
        List<JiggleTransform> jiggleTransformLocalPoses = new List<JiggleTransform>(jiggleTrees.Length);
        List<Vector3> colliders = new List<Vector3>(colliderTransforms.Length);
        foreach(var tree in jiggleTrees) {
            structs.Add(tree.GetStructAndUpdateLists(jiggledTransforms, jiggleTransformPoses, jiggleTransformLocalPoses));
        }

        for (var index = 0; index < colliderTransforms.Length; index++) {
            Debug.DrawLine(Vector3.zero, colliderTransforms[index].position, Color.red, 10f);
            colliders.Add(colliderTransforms[index].position);
            colliderTransformList.Add(colliderTransforms[index]);
        }

        var jiggleTransformRootTransforms = new List<Transform>();
        foreach (var jiggleTree in jiggleTrees) {
            foreach (var t in jiggleTree.points) {
                jiggleTransformRootTransforms.Add(jiggleTree.bones[0]);
            }
        }
        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }
        transformRootAccessArray = new TransformAccessArray(jiggleTransformRootTransforms.ToArray());
        
        treeCount = structs.Count;
        transformCount = jiggledTransforms.Count;
        
        var jiggledTransformPosesArray = jiggleTransformPoses.ToArray();
        
        jobSimulate.Dispose();
        jobSimulate = new JiggleJobSimulate(structs.ToArray(), jiggledTransformPosesArray, colliders.ToArray());
        
        jobBulkReadRoots.Dispose();
        jobBulkReadRoots = new JiggleJobBulkReadRoots(jiggledTransformPosesArray);
        
        jobInterpolation.Dispose();
        jobInterpolation = new JiggleJobInterpolation(jiggledTransformPosesArray, jobBulkReadRoots);

        jobBulkTransformRead.Dispose();
        jobBulkTransformRead = new JiggleJobBulkTransformRead(jobSimulate, jiggleTransformLocalPoses.ToArray());
        
        jobBulkColliderTransformRead.Dispose();
        jobBulkColliderTransformRead = new JiggleJobBulkColliderTransformRead(jobSimulate, colliderTransformList.ToArray());
        
        jobTransformWrite = new JiggleJobTransformWrite(jobBulkTransformRead, jobInterpolation);

        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }
        transformAccessArray = new TransformAccessArray(jiggledTransforms.ToArray());
        if (colliderTransformAccessArray.isCreated) {
            colliderTransformAccessArray.Dispose();
        }
        colliderTransformAccessArray = new TransformAccessArray(colliderTransformList.ToArray());
    }
}