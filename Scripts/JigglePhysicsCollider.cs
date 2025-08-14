using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JigglePhysicsCollider : MonoBehaviour {
    private int id;

    private void OnEnable() {
        id = JiggleTreeUtility.AddSphere(transform);
    }

    private void OnDisable() {
        //JiggleTreeUtility.RemoveSphere(id);
    }
}

}