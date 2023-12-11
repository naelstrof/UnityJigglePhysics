using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
using UnityEngine;

namespace JigglePhysics {

    [CreateAssetMenu(fileName = "JiggleRigSimpleLOD", menuName = "JigglePhysics/JiggleRigSimpleLOD", order = 1)]
    public class JiggleRigSimpleLOD : JiggleRigLOD {

        [Tooltip("Distance to disable the jiggle rig")]
        [SerializeField] float distance;
        [Tooltip("Level of detail manager. This system will control how the jiggle rig saves performance cost.")]
        [SerializeField] float blend;
        [NonSerialized] Transform cameraTransform;

        public override bool CheckActive(Vector3 position) {
            if (cameraTransform == null) cameraTransform = Camera.main.transform;
            return Vector3.Distance(cameraTransform.position, position) < distance;
        }

        public override JiggleSettingsData AdjustJiggleSettingsData(Vector3 position, JiggleSettingsData data) {
            if (cameraTransform == null) cameraTransform = Camera.main.transform;
            var currentBlend = (Vector3.Distance(cameraTransform.position, position) - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f-currentBlend);
            Debug.Log("Intended data: "+currentBlend);
            data.blend = currentBlend;
            return data;
        }

    }

}