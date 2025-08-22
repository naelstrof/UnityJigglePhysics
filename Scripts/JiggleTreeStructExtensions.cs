using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace GatorDragonGames.JigglePhysics {
public static class JiggleTreeStructExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JiggleTransform GetInputPose(this JiggleTreeJobData self, NativeArray<JiggleTransform> inputPoses,
        int index) {
        return inputPoses[index + (int)self.transformIndexOffset];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteOutputPose(this JiggleTreeJobData self, NativeArray<PoseData> outputPoses, int index, JiggleTransform pose, float3 rootOffset, float3 rootPosition) {
        var old = outputPoses[index + (int)self.transformIndexOffset];
        if (old.pose.isVirtual) {
            return;
        }
        pose.isVirtual = old.pose.isVirtual;
        old.pose = pose;
        old.rootOffset = rootOffset;
        old.rootPosition = rootPosition;

        outputPoses[index + (int)self.transformIndexOffset] = old;
    }
}
}