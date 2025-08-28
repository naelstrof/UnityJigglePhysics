#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

namespace GatorDragonGames.JigglePhysics {
[CustomEditor(typeof(JiggleRig), true)]
public class JiggleRigEditor : Editor {
    // Required to force the inspector to use UIToolkit. Otherwise it will use IMGUI.
    public override VisualElement CreateInspectorGUI() {
        VisualElement element = new VisualElement();
        InspectorElement.FillDefaultInspector(element, serializedObject, this);
        return element;
    }
}

}

#endif
