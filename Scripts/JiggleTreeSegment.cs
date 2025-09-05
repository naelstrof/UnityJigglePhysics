using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleTreeSegment {

    public Transform transform { get; private set; }
    public JiggleTree jiggleTree { get; private set; }
    public JiggleTreeSegment parent { get; private set; }
    public JiggleRigData rig { get; private set; }

    public void SetParent(JiggleTreeSegment jiggleTree) {
        parent?.SetDirty();
        parent = jiggleTree;
        parent?.SetDirty();
        JigglePhysics.SetGlobalDirty();
    }

    public JiggleTreeSegment(Transform transform, JiggleRigData rig) {
        this.transform = transform;
        this.rig = rig;
        jiggleTree = JigglePhysics.CreateJiggleTree(rig, null);
    }
    
    public void RegenerateJiggleTreeIfNeeded() {
        if (jiggleTree.dirty) {
            jiggleTree = JigglePhysics.CreateJiggleTree(rig, jiggleTree);
        }
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