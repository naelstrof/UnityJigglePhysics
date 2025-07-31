using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompiled]
public struct JiggleJobInterpolation : IJobFor {
    [ReadOnly] public NativeArray<Matrix4x4> previousSolve;
    [ReadOnly] public NativeArray<Matrix4x4> currentSolve;
    //public NativeArray<Vector3> previousLocalPositions;
    //public NativeArray<Quaternion> previousLocalRotations;
    [ReadOnly] public NativeReference<double> timeStamp;
    [ReadOnly] public NativeReference<double> previousTimeStamp;
    public double currentTime;
    
    [ReadOnly] public NativeReference<Vector3> previousSimulatedRootOffset;
    [ReadOnly] public NativeReference<Vector3> currentSimulatedRootOffset;
    
    [ReadOnly] public NativeReference<Vector3> previousSimulatedRootPosition;
    [ReadOnly] public NativeReference<Vector3> currentSimulatedRootPosition;
    
    public Vector3 realRootPosition;
    
    public NativeArray<Vector3> outputInterpolatedPositions;
    public NativeArray<Quaternion> outputInterpolatedRotations;

    public JiggleJobInterpolation(JiggleJobSimulate jobSimulate, Transform[] bones) {
        var boneCount = bones.Length;
        previousSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent);
        currentSolve = new NativeArray<Matrix4x4>(boneCount, Allocator.Persistent);
        jobSimulate.output.CopyTo(previousSolve);
        jobSimulate.output.CopyTo(currentSolve);
        previousSimulatedRootOffset = new NativeReference<Vector3>(Vector3.zero, Allocator.Persistent);
        currentSimulatedRootOffset = new NativeReference<Vector3>(Vector3.zero, Allocator.Persistent);
        realRootPosition = bones[0].position;
        previousSimulatedRootPosition = new NativeReference<Vector3>(realRootPosition, Allocator.Persistent);
        currentSimulatedRootPosition = new NativeReference<Vector3>(realRootPosition, Allocator.Persistent);
        currentTime = Time.timeAsDouble;
        timeStamp = new NativeReference<double>(currentTime, Allocator.Persistent);
        previousTimeStamp = new NativeReference<double>(currentTime - JiggleJobManager.FIXED_DELTA_TIME, Allocator.Persistent);
        outputInterpolatedPositions = new NativeArray<Vector3>(boneCount, Allocator.Persistent);
        outputInterpolatedRotations = new NativeArray<Quaternion>(boneCount, Allocator.Persistent);
    }
    
    public void Dispose() {
        if (previousSolve.IsCreated) {
            previousSolve.Dispose();
        }
        if (currentSolve.IsCreated) {
            currentSolve.Dispose();
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
        var prevPosition = previousSolve[index].GetPosition();
        var prevRotation = previousSolve[index].rotation;

        var newPosition = currentSolve[index].GetPosition();
        var newRotation = currentSolve[index].rotation;

        var diff = timeStamp.Value - previousTimeStamp.Value;
        if (diff == 0) {
            throw new UnityException("Time difference is zero, cannot interpolate.");
        }

        // TODO: Revisit this issue after FEELING the solve in VR in context
        // The issue here is that we are having to operate 3 full frames in the past
        // which might be noticable latency
        const double timeCorrection = JiggleJobManager.FIXED_DELTA_TIME * 2f;
        var t = (currentTime-timeCorrection - previousTimeStamp.Value) / diff;
        var position = Vector3.LerpUnclamped(prevPosition, newPosition, (float)t);
        var rotation = Quaternion.SlerpUnclamped(prevRotation, newRotation, (float)t);
        
        var simulatedRootPosition = Vector3.LerpUnclamped(previousSimulatedRootPosition.Value, currentSimulatedRootPosition.Value, (float)t);
        var simulatedRootOffset = Vector3.LerpUnclamped(previousSimulatedRootOffset.Value, currentSimulatedRootOffset.Value, (float)t);
        
        var snapToReal = realRootPosition-simulatedRootPosition;
        outputInterpolatedPositions[index] = position + snapToReal + simulatedRootOffset;
        outputInterpolatedRotations[index] = rotation;
    }
}
