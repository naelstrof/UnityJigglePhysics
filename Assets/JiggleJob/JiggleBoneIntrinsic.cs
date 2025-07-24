using UnityEngine;

public unsafe struct JiggleBoneIntrinsic {
    public const int MAX_CHILDREN = 16;
    public int parentIndex;
    public fixed int childrenIndices[MAX_CHILDREN];
    public int childenCount;
}
