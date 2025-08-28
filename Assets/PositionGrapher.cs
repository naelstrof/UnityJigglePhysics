using UnityEngine;

public class PositionGrapher : MonoBehaviour {
    private Vector3[] positions = new Vector3[100];
    private int index = 0;
    void FixedUpdate() {
        index = (index + 1) % positions.Length;
        positions[index] = transform.position;
    }

    private void OnDrawGizmos() {
        for (int i = 0; i < positions.Length; i++) {
            Gizmos.color = Color.HSVToRGB(i / (float)positions.Length, 0.5f, 0.5f);
            Gizmos.DrawSphere(positions[i]+(i%2)*Vector3.up*0.25f, 0.1f);
        }
    }
}
