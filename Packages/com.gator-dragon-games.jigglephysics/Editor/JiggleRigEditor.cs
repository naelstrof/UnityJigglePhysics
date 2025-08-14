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
        rootElement.BindProperty(serializedObject.FindProperty("_rootBone"));
        rootElement.Q<Label>().text = "Root Transform";
        var errorSection = visualElement.Q<VisualElement>("RootTransformErrorSection");
        rootElement.RegisterValueChangedCallback(evt => {
            errorSection.style.display = script.rootTransformError ? DisplayStyle.Flex : DisplayStyle.None;
        });
        errorSection.style.display = script.rootTransformError ? DisplayStyle.Flex : DisplayStyle.None;

        var excludeRootToggleElement = visualElement.Q<Toggle>("ExcludeRootToggle");
        excludeRootToggleElement.BindProperty(serializedObject.FindProperty("_excludeRoot"));
        excludeRootToggleElement.Q<Label>().text = "Motionless Root";

        var excludedTransformsElement = visualElement.Q<PropertyField>("IgnoredTransformsField");
        excludedTransformsElement.BindProperty(serializedObject.FindProperty("_excludedTransforms"));

        visualElement.Add(script.GetInspectorVisualElement(serializedObject.FindProperty("_jiggleBoneInputParameters")));
        
        var rootSection = visualElement.Q<VisualElement>("RootSection");
        excludeRootToggleElement.RegisterValueChangedCallback(evt => {
            rootSection.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
        });

        return visualElement;
    }

    public void OnSceneGUI() {
        var script = (JiggleRig)target;
        var cam = SceneView.lastActiveSceneView.camera;
        var transforms = script.GetJiggleBoneTransforms();
        var jiggleTree = JiggleTreeUtility.CreateJiggleTree(script, null);
        var points = jiggleTree.points;
        for (var index = 0; index < points.Length; index++) {
            var simulatedPoint = points[index];
            if (simulatedPoint.parentIndex == -1) continue;
            if (!points[simulatedPoint.parentIndex].hasTransform) continue;
            DrawBone(points[simulatedPoint.parentIndex].position, simulatedPoint.position, simulatedPoint.parameters,
                cam);
        }
    }

    public void DrawBone(Vector3 boneHead, Vector3 boneTail, JiggleBoneParameters jiggleBoneParameters, Camera cam) {
        var camForward = cam.transform.forward;
        var fixedScreenSize = 0.01f;
        var toCam = cam.transform.position - boneHead;
        var distance = toCam.magnitude;
        var scale = distance * fixedScreenSize;
        scale = jiggleBoneParameters.collisionRadius;
        Handles.DrawWireDisc(boneHead, camForward, scale);
        Handles.DrawLine(boneHead, boneTail);
        var boneDirection = (boneTail - boneHead).normalized;
        var angleLimitScale = 0.05f;
        Handles.DrawWireDisc(
            boneHead + boneDirection * (angleLimitScale * Mathf.Cos(jiggleBoneParameters.angleLimit * Mathf.Deg2Rad)),
            boneDirection, angleLimitScale * Mathf.Sin(jiggleBoneParameters.angleLimit * Mathf.Deg2Rad));
    }

}

}

#endif
