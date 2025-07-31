using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompiled]
public struct JiggleJobInterpolation : IJobFor {
    [ReadOnly] public NativeArray<float3> previousPositions;
    [ReadOnly] public NativeArray<float3> currentPositions;
    
    [ReadOnly] public NativeArray<quaternion> previousRotations;
    [ReadOnly] public NativeArray<quaternion> currentRotations;
    
    //public NativeArray<Vector3> previousLocalPositions;
    //public NativeArray<Quaternion> previousLocalRotations;
    [ReadOnly] public NativeReference<double> timeStamp;
    [ReadOnly] public NativeReference<double> previousTimeStamp;
    public double currentTime;
    
    [ReadOnly] public NativeReference<float3> previousSimulatedRootOffset;
    [ReadOnly] public NativeReference<float3> currentSimulatedRootOffset;
    
    [ReadOnly] public NativeReference<float3> previousSimulatedRootPosition;
    [ReadOnly] public NativeReference<float3> currentSimulatedRootPosition;
    
    public float3 realRootPosition;
    
    public NativeArray<float3> outputInterpolatedPositions;
    public NativeArray<quaternion> outputInterpolatedRotations;

    public JiggleJobInterpolation(JiggleJobSimulate jobSimulate, Transform[] bones) {
        var boneCount = bones.Length;
        previousPositions = new NativeArray<float3>(boneCount, Allocator.Persistent);
        previousRotations = new NativeArray<quaternion>(boneCount, Allocator.Persistent);
        currentPositions = new NativeArray<float3>(boneCount, Allocator.Persistent);
        currentRotations = new NativeArray<quaternion>(boneCount, Allocator.Persistent);
        jobSimulate.outputPositions.CopyTo(previousPositions);
        jobSimulate.outputRotations.CopyTo(previousRotations);
        jobSimulate.outputPositions.CopyTo(currentPositions);
        jobSimulate.outputRotations.CopyTo(currentRotations);
        previousSimulatedRootOffset = new NativeReference<float3>(Vector3.zero, Allocator.Persistent);
        currentSimulatedRootOffset = new NativeReference<float3>(Vector3.zero, Allocator.Persistent);
        realRootPosition = bones[0].position;
        previousSimulatedRootPosition = new NativeReference<float3>(realRootPosition, Allocator.Persistent);
        currentSimulatedRootPosition = new NativeReference<float3>(realRootPosition, Allocator.Persistent);
        currentTime = Time.timeAsDouble;
        timeStamp = new NativeReference<double>(currentTime, Allocator.Persistent);
        previousTimeStamp = new NativeReference<double>(currentTime - JiggleJobManager.FIXED_DELTA_TIME, Allocator.Persistent);
        outputInterpolatedPositions = new NativeArray<float3>(boneCount, Allocator.Persistent);
        outputInterpolatedRotations = new NativeArray<quaternion>(boneCount, Allocator.Persistent);
    }
    
    public void Dispose() {
        if (previousPositions.IsCreated) {
            previousPositions.Dispose();
        }
        if (previousRotations.IsCreated) {
            previousRotations.Dispose();
        }
        if (currentPositions.IsCreated) {
            currentPositions.Dispose();
        }
        if (currentRotations.IsCreated) {
            currentRotations.Dispose();
        }
        if (previousSimulatedRootOffset.IsCreated) {
            previousSimulatedRootOffset.Dispose();
        }
        if (currentSimulatedRootOffset.IsCreated) {
            currentSimulatedRootOffset.Dispose();
        }
        if (previousSimulatedRootPosition.IsCreated) {
            previousSimulatedRootPosition.Dispose();
        }
        if (currentSimulatedRootPosition.IsCreated) {
            currentSimulatedRootPosition.Dispose();
        }
        if (timeStamp.IsCreated) {
            timeStamp.Dispose();
        }
        if (previousTimeStamp.IsCreated) {
            previousTimeStamp.Dispose();
        }
        if (outputInterpolatedPositions.IsCreated) {
            outputInterpolatedPositions.Dispose();
        }
        if (outputInterpolatedRotations.IsCreated) {
            outputInterpolatedRotations.Dispose();
        }
    }
    
    public void Execute(int index) {
        var prevPosition = previousPositions[index];
        var prevRotation = previousRotations[index];

        var newPosition = currentPositions[index];
        var newRotation = currentRotations[index];

        var diff = timeStamp.Value - previousTimeStamp.Value;
        if (diff == 0) {
            throw new UnityException("Time difference is zero, cannot interpolate.");
        }

        // TODO: Revisit this issue after FEELING the solve in VR in context
        // The issue here is that we are having to operate 3 full frames in the past
        // which might be noticable latency
        const double timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
        var t = (currentTime-timeCorrection - previousTimeStamp.Value) / diff;
        var position = math.lerp(prevPosition, newPosition, (float)t);
        var rotation = math.slerp(prevRotation, newRotation, (float)t);
        
        var simulatedRootPosition = math.lerp(previousSimulatedRootPosition.Value, currentSimulatedRootPosition.Value, (float)t);
        var simulatedRootOffset = math.lerp(previousSimulatedRootOffset.Value, currentSimulatedRootOffset.Value, (float)t);
        
        var snapToReal = realRootPosition-simulatedRootPosition;
        outputInterpolatedPositions[index] = position + snapToReal + simulatedRootOffset;
        outputInterpolatedRotations[index] = rotation;
    }
}
