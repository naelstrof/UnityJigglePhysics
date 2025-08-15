using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace GatorDragonGames.JigglePhysics {

[Serializable]
public struct BoneNormalizedDistanceFromRoot {
    public Transform bone;
    public float normalizedDistanceFromRoot;
}

public class JiggleRig : MonoBehaviour {
    [SerializeField] protected Transform _rootBone;
    [SerializeField] protected bool _advanced;
    [SerializeField] protected bool _animated;
    [SerializeField] protected bool _excludeRoot;
    [SerializeField] protected JiggleTreeInputParameters jiggleTreeInputParameters;
    [SerializeField] protected List<Transform> _excludedTransforms = new List<Transform>();
    [SerializeField, HideInInspector] List<BoneNormalizedDistanceFromRoot> _boneNormalizedDistanceFromRootList;
    [SerializeField] List<JiggleColliderSerializable> jiggleColliders = new List<JiggleColliderSerializable>();

    private JiggleTreeSegment _jiggleTreeSegment;
    bool isValid = false;
    public bool rootExcluded => _excludeRoot;
    public Transform rootBone => _rootBone;
    public bool CheckExcluded(Transform t) => _excludedTransforms.Contains(t);
    
    public void GetJiggleColliders(List<JiggleCollider> colliders) {
        colliders.Clear();
        foreach (var collider in jiggleColliders) {
            colliders.Add(collider.collider);
        }
    }
    
    public void GetJiggleColliderTransforms(List<Transform> colliderTransforms) {
        colliderTransforms.Clear();
        foreach (var collider in jiggleColliders) {
            colliderTransforms.Add(collider.transform);
        }
    }

    public float GetNormalizedDistanceFromRoot(Transform t) {
        var entry = _boneNormalizedDistanceFromRootList.Find(x => x.bone == t);
        return entry.bone ? entry.normalizedDistanceFromRoot : 0f;
    }

    public bool normalizedDistanceFromRootListIsValid => _boneNormalizedDistanceFromRootList != null &&
                                                         _boneNormalizedDistanceFromRootList.Count > 0;

    public bool rootTransformError => !(!_rootBone || isValid);

    private void OnEnable() {
        _jiggleTreeSegment ??= new JiggleTreeSegment(_rootBone, this);
        _jiggleTreeSegment.SetDirty();
        JigglePhysics.AddJiggleTreeSegment(_jiggleTreeSegment);
    }

    private void OnDisable() {
        JigglePhysics.RemoveJiggleTreeSegment(_jiggleTreeSegment);
    }

    public JigglePointParameters GetJiggleBoneParameter(float normalizedDistanceFromRoot) {
        return jiggleTreeInputParameters.ToJigglePointParameters(normalizedDistanceFromRoot);
    }

    public Transform[] GetJiggleBoneTransforms() {
        var transforms = _rootBone.GetComponentsInChildren<Transform>();
        return transforms;
    }

    void OnValidate() {
        isValid = _rootBone.IsChildOf(gameObject.transform);
        if (_excludedTransforms == null) _excludedTransforms = new List<Transform>();
        ValidateCurve(ref jiggleTreeInputParameters.stiffnessCurve);
        ValidateCurve(ref jiggleTreeInputParameters.angleLimitCurve);
        ValidateCurve(ref jiggleTreeInputParameters.stretchCurve);
        ValidateCurve(ref jiggleTreeInputParameters.dragCurve);
        ValidateCurve(ref jiggleTreeInputParameters.airDragCurve);
        ValidateCurve(ref jiggleTreeInputParameters.gravityCurve);
        ValidateCurve(ref jiggleTreeInputParameters.collisionRadiusCurve);
        BuildNormalizedDistanceFromRootList();
    }

    public void BuildNormalizedDistanceFromRootList() {
        JigglePhysics.VisitForLength(_rootBone, this, _rootBone.position, 0f, out var totalLength);
        _boneNormalizedDistanceFromRootList = new List<BoneNormalizedDistanceFromRoot>();
        VisitAndSetNormalizedDistanceFromRoot(_rootBone, _rootBone.position, 0f, totalLength);
    }

    public void VisitAndSetNormalizedDistanceFromRoot(Transform t, Vector3 lastPosition, float currentLength, float totalLength) {
        if (CheckExcluded(t)) {
            return;
        }
        currentLength += Vector3.Distance(lastPosition, t.position);
        _boneNormalizedDistanceFromRootList.Add(new BoneNormalizedDistanceFromRoot() {
            bone = t,
            normalizedDistanceFromRoot = currentLength / totalLength
        });
        var validChildrenCount = JigglePhysics.GetValidChildrenCount(t, this);
        for (int i = 0; i < validChildrenCount; i++) {
            var child = JigglePhysics.GetValidChild(t, this, i);
            VisitAndSetNormalizedDistanceFromRoot(child, t.position, currentLength, totalLength);
        }
    }

    void ValidateCurve(ref AnimationCurve animationCurve) {
        if (animationCurve == null || animationCurve.length == 0) {
            animationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        }
    }

#if UNITY_EDITOR
    public VisualElement GetInspectorVisualElement(SerializedProperty serializedProperty) {
        var visualTreeAsset =
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                AssetDatabase.GUIDToAssetPath("c35a2123f4d44dd469ccb24af7a0ce20"));
        var visualElement = new VisualElement();
        visualTreeAsset.CloneTree(visualElement);
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "StiffnessControl",
            nameof(JiggleTreeInputParameters.stiffness),
            nameof(JiggleTreeInputParameters.stiffnessCurve),
            "Stiffness"
        );

        var angleLimitToggleElement = visualElement.Q<Toggle>("AngleLimitToggle");
        angleLimitToggleElement.BindProperty(
            serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.angleLimitToggle)));
        angleLimitToggleElement.Q<Label>().text = "Angle Limit";

        var angleLimitSection = visualElement.Q<VisualElement>("AngleLimitSection");
        angleLimitToggleElement.RegisterValueChangedCallback(evt => {
            angleLimitSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "AngleLimitControl",
            nameof(JiggleTreeInputParameters.angleLimit),
            nameof(JiggleTreeInputParameters.angleLimitCurve),
            "Angle Limit"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "AngleLimitSoftenSlider",
            nameof(JiggleTreeInputParameters.angleLimitSoften),
            "Angle Limit Soften"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "SoftenSlider",
            nameof(JiggleTreeInputParameters.soften),
            "Soften"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "RootStretchSlider",
            nameof(JiggleTreeInputParameters.rootStretch),
            "Root Stretch"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "StretchControl",
            nameof(JiggleTreeInputParameters.stretch),
            nameof(JiggleTreeInputParameters.stretchCurve),
            "Stretch"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "DragControl",
            nameof(JiggleTreeInputParameters.drag),
            nameof(JiggleTreeInputParameters.dragCurve),
            "Drag"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "AirDragControl",
            nameof(JiggleTreeInputParameters.airDrag),
            nameof(JiggleTreeInputParameters.airDragCurve),
            "Air Drag"
        );

        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "GravityControl",
            nameof(JiggleTreeInputParameters.gravity),
            nameof(JiggleTreeInputParameters.gravityCurve),
            "Gravity"
        );

        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "CollisionRadiusControl",
            nameof(JiggleTreeInputParameters.collisionRadius),
            nameof(JiggleTreeInputParameters.collisionRadiusCurve),
            "Collision Radius"
        );

        var advancedToggleElement = visualElement.Q<Toggle>("AdvancedToggle");
        advancedToggleElement.BindProperty(
            serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.advancedToggle)));
        advancedToggleElement.Q<Label>().text = "Advanced";

        var collisionToggleElement = visualElement.Q<Toggle>("CollisionToggle");
        collisionToggleElement.BindProperty(
            serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.collisionToggle)));
        collisionToggleElement.Q<Label>().text = "Collision";

        var advancedSection = visualElement.Q<VisualElement>("AdvancedSection");
        var advancedSection2 = visualElement.Q<VisualElement>("AdvancedSection2");
        advancedToggleElement.RegisterValueChangedCallback(evt => {
            advancedSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            advancedSection2.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        var collisionSection = visualElement.Q<VisualElement>("CollisionSection");
        collisionToggleElement.RegisterValueChangedCallback(evt => {
            collisionSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        return visualElement;
    }

    void SetSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id, string parameter,
        string name) {
        var sliderElement = visualElement.Q<Slider>(id);
        sliderElement.BindProperty(serializedProperty.FindPropertyRelative(parameter));
        sliderElement.Q<Label>().text = name;
    }

    void SetCurvableSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id,
        string sliderParameter, string curveParameter, string name) {
        var sliderElement = visualElement.Q<VisualElement>(id);
        var sliderElementSlider = sliderElement.Q<Slider>("CurvableSlider");
        sliderElementSlider.BindProperty(serializedProperty.FindPropertyRelative(sliderParameter));
        sliderElementSlider.Q<Label>().text = name;
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.BindProperty(serializedProperty.FindPropertyRelative(curveParameter));
    }

    void SetCurvableFloat(VisualElement visualElement, SerializedProperty serializedProperty, string id,
        string floatParameter, string curveParameter, string name) {
        var sliderElement = visualElement.Q<VisualElement>(id);
        var curvableFloat = sliderElement.Q<FloatField>("CurvableFloat");
        curvableFloat.BindProperty(serializedProperty.FindPropertyRelative(floatParameter));
        curvableFloat.Q<Label>().text = name;
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.BindProperty(serializedProperty.FindPropertyRelative(curveParameter));
    }

#endif

}

}
