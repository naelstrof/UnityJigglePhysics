using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class JiggleJobDoubleBuffer {
    private int flips = 0;
    private bool flipped;
    public JiggleJob jobA;
    public JiggleJob jobB;
    public ref JiggleJob currentJob => ref (flipped ? ref jobB : ref jobA);
    public ref JiggleJob previousJob => ref (flipped ? ref jobA : ref jobB);
    public bool HasData() => flips >= 3;

    public void Flip() {
        flips++;
        flipped = !flipped;
        int boneCount = currentJob.bones.Length;
        for (int i = 0; i < boneCount; i++) {
            currentJob.previousOutput[i] = previousJob.output[i];
            currentJob.previousTimeStamp = previousJob.timeStamp;
        }
    }
}
