using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "JiggleSettings", menuName = "JiggleRig/Settings", order = 1)]
public class JiggleSettings : ScriptableObject {
    
    [Range(0f,1f)] [SerializeField] public float gravityMultiplier = 1f;
    [Range(0f,1f)] [SerializeField] public float friction = 0.5f;
    [Range(0f,1f)] [SerializeField] public float airFriction = 0.5f;
    [Range(0f,1f)] [SerializeField] public float inertness = 0.5f;
    [Range(0f,1f)] [SerializeField] public float blend = 1f;
    [Range(0f,1f)] [SerializeField] public float angleElasticity = 0.5f;
    [Range(0f,1f)] [SerializeField] public float lengthElasticity = 0.5f;
    
}
