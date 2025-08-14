using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

public class JiggleCollider : MonoBehaviour {
    private int id;

    private void OnEnable() {
        id = JigglePhysics.AddSphere(transform);
    }

    private void OnDisable() {
        //JiggleTreeUtility.RemoveSphere(id);
    }
}

}