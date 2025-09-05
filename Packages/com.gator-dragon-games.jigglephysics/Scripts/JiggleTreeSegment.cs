using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleTreeSegment {

    public Transform transform { get; private set; }
    public JiggleTree jiggleTree { get; private set; }
    public JiggleTreeSegment parent { get; private set; }
    public JiggleRigData rig { get; private set; }

    public void SetJiggleTree(JiggleTree jiggleTree) => this.jiggleTree = jiggleTree;

    public void SetParent(JiggleTreeSegment jiggleTree) {
        parent?.SetDirty();
        parent = jiggleTree;
        parent?.SetDirty();
        JigglePhysics.SetGlobalDirty();
    }

    public JiggleTreeSegment(Transform transform, JiggleRigData rig) {
        this.transform = transform;
        this.rig = rig;
    }

    public void SetDirty() {
        if (jiggleTree is { dirty: false }) {
            JigglePhysics.ScheduleRemoveJiggleTree(jiggleTree);
            jiggleTree.SetDirty();
        }
        parent?.SetDirty();
        JigglePhysics.SetGlobalDirty();
    }

}

}