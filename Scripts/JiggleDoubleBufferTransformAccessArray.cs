using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace GatorDragonGames.JigglePhysics {

public class JiggleDoubleBufferTransformAccessArray {
    
    private TransformAccessArray transformAccessArray;
    private int transformCount;

    private TransformAccessArray newTransformAccessArray;
    private int newTransformCount;

    public TransformAccessArray GetTransformAccessArray() => transformAccessArray;

    private bool shouldClear = false;

    public JiggleDoubleBufferTransformAccessArray(int initialCapacity) {
        transformAccessArray = new TransformAccessArray(initialCapacity);
        newTransformAccessArray = new TransformAccessArray(initialCapacity);
    }

    public void Flip() {
        (transformAccessArray, newTransformAccessArray) = (newTransformAccessArray, transformAccessArray);
        (transformCount, newTransformCount) = (newTransformCount, transformCount);
        shouldClear = true;
    }

    public void Dispose() {
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }

        if (newTransformAccessArray.isCreated) {
            newTransformAccessArray.Dispose();
        }
    }

    public void ClearIfNeeded(int maxRemoveCount = 512) {
        Profiler.BeginSample("ClearAccessArrays");
        if (!shouldClear) {
            Profiler.EndSample();
            return;
        }

        int removedSoFar = 0;
        for (int i = 0; i < newTransformCount && removedSoFar < maxRemoveCount; i++) {
            newTransformAccessArray.RemoveAtSwapBack(0);
            newTransformCount--;
            removedSoFar++;
        }

        shouldClear = newTransformCount > 0;
        Profiler.EndSample();
    }

    public void GenerateNewAccessArrays(ref int currentIndex, out bool hasFinished, List<Transform> transformAccessList, Transform dummyTransform, int maxAddCount = 512) {
        if (shouldClear) {
            ClearIfNeeded(maxAddCount);
            hasFinished = false;
            return;
        }

        Profiler.BeginSample("GenerateNewAccessArrays");
        var count = transformAccessList.Count;
        int addedSoFar = 0;
        for (var index = currentIndex; index < count && addedSoFar < maxAddCount; index++) {
            var transform = transformAccessList[index];
            if (!transform) {
                newTransformAccessArray.Add(dummyTransform);
            } else {
                newTransformAccessArray.Add(transform);
            }

            addedSoFar++;
        }

        currentIndex += addedSoFar;

        if (currentIndex == count) {
            newTransformCount = count;
            hasFinished = true;
            Profiler.EndSample();
            return;
        }

        hasFinished = false;
        Profiler.EndSample();
    }
}

}