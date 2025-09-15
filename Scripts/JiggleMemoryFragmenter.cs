using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace GatorDragonGames.JigglePhysics {

public class JiggleMemoryFragmenter {
    private struct Fragment {
        public int startIndex;
        public int count;
    }

    private int currentSize;
    private List<Fragment> fragments;

    public JiggleMemoryFragmenter(int size) {
        fragments = new List<Fragment> {
            new() {
                startIndex = 0,
                count = size
            }
        };
        currentSize = size;
    }
    
    public int GetHighestAllocatedIndex() {
        if (fragments.Count <= 0) {
            return currentSize;
        }
        var lastFragment = fragments[^1];
        return lastFragment.startIndex - 1;
    }

    public bool TryAllocate(int size, out int startIndex) {
        var fragmentCount = fragments.Count;
        for (var i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            if (fragment.count >= size) {
                startIndex = fragment.startIndex;
                if (fragment.count == size) {
                    fragments.RemoveAt(i);
                } else {
                    fragment.startIndex += size;
                    fragment.count -= size;
                    fragments[i] = fragment;
                }

                return true;
            }
        }
        startIndex = -1;
        return false;
    }

    public void Resize(int newSize) {
        Assert.IsTrue(currentSize <= newSize);
        if (fragments.Count != 0) {
            var fragment = fragments[^1];
            fragment.count += newSize - currentSize;
            fragments[^1] = fragment;
        } else {
            fragments.Add(new Fragment {
                startIndex = currentSize,
                count = newSize - currentSize
            });
        }
        currentSize = newSize;
    }

    public void Free(int startIndex, int size) {
        var fragmentCount = fragments.Count;
        #if UNITY_EDITOR
        for (int i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            var smallRange = math.min(fragment.count, size);
            var larger = math.max(fragment.startIndex, startIndex);
            var smaller = math.min(fragment.startIndex, startIndex);
            if (larger - smaller < smallRange) {
                throw new UnityException($"Double free detected at index {startIndex}, count {size}, not allowed! (overlaps with fragment at index {fragment.startIndex}, count {fragment.count})");
            }
        }
        #endif

        for (int i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            if (i + 1 < fragmentCount) {
                var nextFragment = fragments[i + 1];
                if (fragment.startIndex + fragment.count == startIndex && startIndex + size == nextFragment.startIndex) {
                    // Merge with previous and next fragment
                    fragment.count += size + nextFragment.count;
                    fragments[i] = fragment;
                    fragments.RemoveAt(i + 1);
                    return;
                }
            }

            if (fragment.startIndex + fragment.count == startIndex) {
                // Merge with previous fragment
                fragment.count += size;
                fragments[i] = fragment;
                return;
            }
            if (startIndex + size == fragment.startIndex) {
                // Merge with next fragment
                fragment.startIndex -= size;
                fragment.count += size;
                fragments[i] = fragment;
                return;
            }
        }

        // No merge, add new fragment
        var newFragment = new Fragment {
            startIndex = startIndex,
            count = size
        };
        for (int i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            if (fragment.startIndex >= startIndex + size) {
                fragments.Insert(i, newFragment);
                return;
            }
        }

        fragments.Add(newFragment);
    }

    public bool GetIsAllocated(int index) {
        if (index < 0 || index >= currentSize) {
            return false;
        }
        var fragmentCount = fragments.Count;
        for (int i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            if (index >= fragment.startIndex && index < fragment.startIndex + fragment.count) {
                return false;
            }
        }
        return true;
    }
}

}