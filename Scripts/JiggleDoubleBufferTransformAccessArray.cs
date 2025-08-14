using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace GatorDragonGames.JigglePhysics {

public class JiggleDoubleBufferTransformAccessArray {
    private TransformAccessArray transformAccessArray;
    private TransformAccessArray transformRootAccessArray;
    private int transformCount;

    private TransformAccessArray newTransformAccessArray;
    private TransformAccessArray newTransformRootAccessArray;
    private int newTransformCount;

    public TransformAccessArray GetTransformAccessArray() => transformAccessArray;
    public TransformAccessArray GetTransformRootAccessArray() => transformRootAccessArray;

    private bool shouldClear = false;

    public JiggleDoubleBufferTransformAccessArray(int initialCapacity) {
        transformAccessArray = new TransformAccessArray(initialCapacity);
        transformRootAccessArray = new TransformAccessArray(initialCapacity);

        newTransformAccessArray = new TransformAccessArray(initialCapacity);
        newTransformRootAccessArray = new TransformAccessArray(initialCapacity);
    }

    public void Flip() {
        (transformAccessArray, newTransformAccessArray) = (newTransformAccessArray, transformAccessArray);
        (transformRootAccessArray, newTransformRootAccessArray) =
            (newTransformRootAccessArray, transformRootAccessArray);
        (transformCount, newTransformCount) = (newTransformCount, transformCount);
        shouldClear = true;
    }

    public void Dispose() {
        if (transformAccessArray.isCreated) {
            transformAccessArray.Dispose();
        }

        if (transformRootAccessArray.isCreated) {
            transformRootAccessArray.Dispose();
        }

        if (newTransformAccessArray.isCreated) {
            newTransformAccessArray.Dispose();
        }

        if (newTransformRootAccessArray.isCreated) {
            newTransformRootAccessArray.Dispose();
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
            newTransformRootAccessArray.RemoveAtSwapBack(0);
            newTransformCount--;
            removedSoFar++;
        }

        shouldClear = newTransformCount > 0;
        Profiler.EndSample();
    }

    public void GenerateNewAccessArrays(ref int currentIndex, out bool hasFinished, List<Transform> transformAccessList,
        List<Transform> transformRootAccessList, int maxAddCount = 512) {
        if (shouldClear) {
            ClearIfNeeded(maxAddCount);
            hasFinished = false;
            return;
        }

        Profiler.BeginSample("GenerateNewAccessArrays");
        var count = transformAccessList.Count;
        int addedSoFar = 0;
        for (var index = currentIndex; index < count && addedSoFar < maxAddCount; index++) {
            newTransformAccessArray.Add(transformAccessList[index]);
            newTransformRootAccessArray.Add(transformRootAccessList[index]);
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