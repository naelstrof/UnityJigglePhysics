#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

namespace GatorDragonGames.JigglePhysics {

[CustomEditor(typeof(JiggleRig), true)]
public class JiggleRigEditor : Editor {

    public override VisualElement CreateInspectorGUI() {

        var script = (JiggleRig)target;

        var visualTreeAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                AssetDatabase.GUIDToAssetPath("3b91b5cf6b975bd4d83d8a940258c420"));
        var visualElement = new VisualElement();
        visualTreeAsset.CloneTree(visualElement);

        var rootElement = visualElement.Q<ObjectField>("RootField");
        rootElement.objectType = typeof(Transform);
        rootElement.BindProperty(serializedObject.FindProperty("_rootBone"));
        rootElement.Q<Label>().text = "Root Transform";
        var errorSection = visualElement.Q<VisualElement>("RootTransformErrorSection");
        rootElement.RegisterValueChangedCallback(evt => {
            errorSection.style.display = script.GetHasRootTransformError() ? DisplayStyle.Flex : DisplayStyle.None;
        });
        errorSection.style.display = script.GetHasRootTransformError() ? DisplayStyle.Flex : DisplayStyle.None;

        var excludeRootToggleElement = visualElement.Q<Toggle>("ExcludeRootToggle");
        excludeRootToggleElement.BindProperty(serializedObject.FindProperty("excludeRoot"));
        excludeRootToggleElement.Q<Label>().text = "Motionless Root";

        var excludedTransformsElement = visualElement.Q<PropertyField>("IgnoredTransformsField");
        excludedTransformsElement.BindProperty(serializedObject.FindProperty("excludedTransforms"));

        var personalCollidersElement = visualElement.Q<PropertyField>("PersonalCollidersField");
        personalCollidersElement.BindProperty(serializedObject.FindProperty("jiggleColliders"));

        visualElement.Add(script.GetInspectorVisualElement(serializedObject.FindProperty("jiggleTreeInputParameters")));
        
        var rootSection = visualElement.Q<VisualElement>("RootSection");
        excludeRootToggleElement.RegisterValueChangedCallback(evt => {
            rootSection.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        });

        return visualElement;
    }

    public void OnSceneGUI() {
        var script = (JiggleRig)target;
        if (!script.GetIsValid()) {
            return;
        }
        var cam = SceneView.lastActiveSceneView.camera;
        var jiggleTree = JigglePhysics.CreateJiggleTree(script, null);
        var points = jiggleTree.points;
        for (var index = 0; index < points.Length; index++) {
            var simulatedPoint = points[index];
            if (simulatedPoint.parentIndex == -1) continue;
            if (!points[simulatedPoint.parentIndex].hasTransform) continue;
            DrawBone(points[simulatedPoint.parentIndex].position, simulatedPoint.position, jiggleTree.bones[index].lossyScale, points[simulatedPoint.parentIndex].parameters,
                cam);
        }
    }

    public void DrawBone(Vector3 boneHead, Vector3 boneTail, Vector3 boneScale, JigglePointParameters jigglePointParameters, Camera cam) {
        var camForward = cam.transform.forward;
        var fixedScreenSize = 0.01f;
        var toCam = cam.transform.position - boneHead;
        var distance = toCam.magnitude;
        var scale = distance * fixedScreenSize;
        scale = jigglePointParameters.collisionRadius * (boneScale.x + boneScale.y + boneScale.z)/3f;
        Handles.DrawWireDisc(boneHead, camForward, scale);
        Handles.DrawLine(boneHead, boneTail);
        var boneDirection = (boneTail - boneHead).normalized;
        var angleLimitScale = 0.05f;
        Handles.DrawWireDisc(
            boneHead + boneDirection * (angleLimitScale * Mathf.Cos(jigglePointParameters.angleLimit * Mathf.Deg2Rad)),
            boneDirection, angleLimitScale * Mathf.Sin(jigglePointParameters.angleLimit * Mathf.Deg2Rad));
    }

}

}

#endif
