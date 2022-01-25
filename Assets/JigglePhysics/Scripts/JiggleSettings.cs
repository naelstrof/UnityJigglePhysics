using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JiggleSettings", menuName = "JiggleRig/Settings", order = 1)]
public class JiggleSettings : ScriptableObject {
    
    [Range(0f,1f)] [SerializeField] public float gravityMultiplier = 1f;
    [Range(0f,1f)] [SerializeField] public float friction = 0.5f;
    [Range(0f,1f)] [SerializeField] public float inertness = 0.5f;
    [Range(0f,1f)] [SerializeField] public float blend = 1f;
    [Range(0f,1f)] [SerializeField] public float elasticity = 0.5f;
    
}
