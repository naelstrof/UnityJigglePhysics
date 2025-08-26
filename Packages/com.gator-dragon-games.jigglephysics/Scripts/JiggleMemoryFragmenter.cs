using System.Collections.Generic;
using UnityEngine.Assertions;

namespace GatorDragonGames.JigglePhysics {

public class JiggleMemoryFragmenter {
    private struct Fragment {
        public int startIndex;
        public int count;
    }

    private int startingSize;
    private List<Fragment> fragments;

    public JiggleMemoryFragmenter(int size) {
        fragments = new List<Fragment> {
            new() {
                startIndex = 0,
                count = size
            }
        };
        startingSize = size;
    }

    public bool TryAllocate(int size, out int startIndex) {
        var fragmentCount = fragments.Count;
        for (int i = 0; i < fragmentCount; i++) {
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
        Assert.IsTrue(startingSize <= newSize);
        if (fragments.Count != 0) {
            var fragment = fragments[^1];
            fragment.count += newSize - startingSize;
            fragments[^1] = fragment;
        } else {
            fragments.Add(new Fragment {
                startIndex = startingSize,
                count = newSize - startingSize
            });
        }
    }

    public void Free(int startIndex, int size) {
        var fragmentCount = fragments.Count;
        for (int i = 0; i < fragmentCount; i++) {
            var fragment = fragments[i];
            if (fragment.startIndex + fragment.count == startIndex) {
                // Merge with previous fragment
                fragment.count += size;
                fragments[i] = fragment;
                return;
            } else if (startIndex + size == fragment.startIndex) {
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

    public void CopyFrom(JiggleMemoryFragmenter other) {
        startingSize = other.startingSize;
        fragments.Clear();
        fragments.AddRange(other.fragments);
    }
}

}