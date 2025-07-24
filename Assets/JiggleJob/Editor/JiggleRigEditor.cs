using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(JiggleRig), true)]
public class JiggleRigEditor : Editor {
    
    public override VisualElement CreateInspectorGUI() {
        
        var script = (JiggleRig)target;

        var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("3b91b5cf6b975bd4d83d8a940258c420"));
        var visualElement = new VisualElement();
        visualTreeAsset.CloneTree(visualElement);
        
        var rootElement = visualElement.Q<ObjectField>("RootField");
        rootElement.BindProperty(serializedObject.FindProperty("_rootBone"));
        rootElement.Q<Label>().text = "Root Transform";
        
        visualElement.Add(script.GetInspectorVisualElement(serializedObject.FindProperty("_jiggleBoneInputParameters")));

        return visualElement;
    }

}
