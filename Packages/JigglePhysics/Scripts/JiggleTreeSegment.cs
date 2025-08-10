using UnityEngine;

public class JiggleTreeSegment {
    
    public Transform transform { get; private set; }
    public JiggleTree jiggleTree { get; private set; }
    public JiggleTreeSegment parent { get; private set; }
    public JiggleRig rig { get; private set; }
    
    public void SetJiggleTree(JiggleTree jiggleTree) => this.jiggleTree = jiggleTree;
    public void SetParent(JiggleTreeSegment jiggleTree) => parent = jiggleTree;

    public JiggleTreeSegment(Transform transform, JiggleRig rig) {
        this.transform = transform;
        this.rig = rig;
    }

    public void SetDirty() {
        if (jiggleTree!=null) jiggleTree.SetDirty();
        parent?.SetDirty();
        JiggleTreeUtility.SetGlobalDirty();
    }

}

