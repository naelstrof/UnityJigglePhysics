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
public struct JiggleTransformCachedData {
    public Transform bone;
    public float normalizedDistanceFromRoot;
    public float lossyScale;
}

public class JiggleRig : MonoBehaviour {
    [SerializeField] private Transform _rootBone;
    [FormerlySerializedAs("_advanced")] [SerializeField] protected bool advanced;
    [FormerlySerializedAs("_excludeRoot")] [SerializeField] protected bool excludeRoot;
    [SerializeField] protected JiggleTreeInputParameters jiggleTreeInputParameters = JiggleTreeInputParameters.Default();
    [FormerlySerializedAs("_excludedTransforms")] [SerializeField] protected List<Transform> excludedTransforms = new List<Transform>();
    [SerializeField] protected List<JiggleCollider> personalColliders = new List<JiggleCollider>();
    [SerializeField, HideInInspector] List<JiggleTransformCachedData> transformCachedData;
    [SerializeField] protected List<JiggleColliderSerializable> jiggleColliders = new List<JiggleColliderSerializable>();

    private JiggleTreeSegment _jiggleTreeSegment;
    bool isValid = false;
    public bool rootExcluded => excludeRoot;
    public Transform rootBone => _rootBone;
    public bool CheckExcluded(Transform t) => excludedTransforms.Contains(t);

#if UNITY_EDITOR
    [ContextMenu("Jiggle Physics/Reset to defaults")]
    public void ResetToDefaults() {
        jiggleTreeInputParameters = JiggleTreeInputParameters.Default();
        EditorUtility.SetDirty(this);
    }
#endif
    
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
        var count = transformCachedData.Count;
        for (int i = 0; i < count; i++) {
            var cachedData = transformCachedData[i];
            if (cachedData.bone == t) {
                return cachedData.normalizedDistanceFromRoot;
            }
        }
        return 0f;
    }
    
    public float GetCachedLossyScale(Transform t) {
        var count = transformCachedData.Count;
        for (int i = 0; i < count; i++) {
            var cachedData = transformCachedData[i];
            if (cachedData.bone == t) {
                return cachedData.lossyScale;
            }
        }
        return 1f;
    }

    public bool GetNormalizedDistanceFromRootListIsValid() => transformCachedData != null &&
                                                         transformCachedData.Count > 0;

    public bool GetHasRootTransformError() => !(!_rootBone || isValid);
    
    public bool GetIsValid() => isValid;

    private void OnEnable() {
        _jiggleTreeSegment ??= new JiggleTreeSegment(_rootBone, this);
        _jiggleTreeSegment.SetDirty();
        JigglePhysics.AddJiggleTreeSegment(_jiggleTreeSegment);
    }

    private void OnDisable() {
        JigglePhysics.RemoveJiggleTreeSegment(_jiggleTreeSegment);
    }

    public JigglePointParameters GetJiggleBoneParameter(float normalizedDistanceFromRoot, float lossyCachedSacle, float lossyRealScale) {
        return jiggleTreeInputParameters.ToJigglePointParameters(normalizedDistanceFromRoot, lossyCachedSacle, lossyRealScale);
    }

    public Transform[] GetJiggleBoneTransforms() {
        var transforms = _rootBone.GetComponentsInChildren<Transform>();
        return transforms;
    }

    void OnValidate() {
        isValid = (_rootBone && _rootBone.IsChildOf(gameObject.transform));
        excludedTransforms ??= new List<Transform>();
        ValidateCurve(ref jiggleTreeInputParameters.stiffness.curve);
        ValidateCurve(ref jiggleTreeInputParameters.angleLimit.curve);
        ValidateCurve(ref jiggleTreeInputParameters.stretch.curve);
        ValidateCurve(ref jiggleTreeInputParameters.drag.curve);
        ValidateCurve(ref jiggleTreeInputParameters.airDrag.curve);
        ValidateCurve(ref jiggleTreeInputParameters.gravity.curve);
        ValidateCurve(ref jiggleTreeInputParameters.collisionRadius.curve);
        BuildNormalizedDistanceFromRootList();
    }

    public void BuildNormalizedDistanceFromRootList() {
        if (!isValid) {
            return;
        }
        JigglePhysics.VisitForLength(_rootBone, this, _rootBone.position, 0f, out var totalLength);
        transformCachedData = new List<JiggleTransformCachedData>();
        VisitAndSetCacheData(_rootBone, _rootBone.position, 0f, totalLength);
    }

    public void VisitAndSetCacheData(Transform t, Vector3 lastPosition, float currentLength, float totalLength) {
        if (CheckExcluded(t)) {
            return;
        }
        currentLength += Vector3.Distance(lastPosition, t.position);
        var scale = t.lossyScale;
        transformCachedData.Add(new JiggleTransformCachedData() {
            bone = t,
            normalizedDistanceFromRoot = currentLength / totalLength,
            lossyScale = (scale.x + scale.y + scale.x)/3f,
        });
        var validChildrenCount = JigglePhysics.GetValidChildrenCount(t, this);
        for (int i = 0; i < validChildrenCount; i++) {
            var child = JigglePhysics.GetValidChild(t, this, i);
            VisitAndSetCacheData(child, t.position, currentLength, totalLength);
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
            "Stiffness",
            0.3f,
            1f,
            "Stiffness controls how strongly the bone returns to its rest pose. A value of 1 makes it immovable, while a value of 0 makes it fall freely."
        );

        var angleLimitToggleElement = visualElement.Q<Toggle>("AngleLimitToggle");
        angleLimitToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.angleLimitToggle)));
        angleLimitToggleElement.tooltip = "Enable or disable angle limit.";
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
            "Angle Limit",
            0f,
            1f,
            "Angle Limit controls the maximum angle deviation from the bone's rest position. 0 means no limit, while 1 represents a 90 degree limit."
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "AngleLimitSoftenSlider",
            nameof(JiggleTreeInputParameters.angleLimitSoften),
            "Angle Limit Soften",
            "Softens the angle limit to prevent hard stops, 0 is hard, 1 is soft."
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "SoftenSlider",
            nameof(JiggleTreeInputParameters.soften),
            "Soften",
            "Weakens the stiffness of the bone when it's closer to the target pose. Prevents large deformations, while still looking very soft."
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "RootStretchSlider",
            nameof(JiggleTreeInputParameters.rootStretch),
            "Root Stretch",
            "Allows the root bone to translate. 0 means the root bone is fixed in place, while 1 means it can stretch freely."
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "StretchControl",
            nameof(JiggleTreeInputParameters.stretch),
            "Stretch",
            0f,
            1f,
            "Stretch controls the elasticity of the bone length, where 0 is no stretch and 1 is full stretch."
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "DragControl",
            nameof(JiggleTreeInputParameters.drag),
            "Drag",
            0f,
            1f,
            "Drag controls the tendency for the bone to stop oscillating, where 0 is maximum oscillations and 1 is zero oscillations."
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "AirDragControl",
            nameof(JiggleTreeInputParameters.airDrag),
            "Air Drag",
            0f,
            1f,
            "Air Drag controls how much resistance the bone experiences in air, 0 is no resistance and 1 is maximum resistance."
        );

        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "GravityControl",
            nameof(JiggleTreeInputParameters.gravity),
            "Gravity"
        );

        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "CollisionRadiusControl",
            nameof(JiggleTreeInputParameters.collisionRadius),
            "Collision Radius"
        );

        var advancedToggleElement = visualElement.Q<Toggle>("AdvancedToggle");
        advancedToggleElement.BindProperty(
            serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.advancedToggle)));
        advancedToggleElement.Q<Label>().text = "Advanced";

        var collisionToggleElement = visualElement.Q<Toggle>("CollisionToggle");
        collisionToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleTreeInputParameters.collisionToggle)));
        collisionToggleElement.tooltip = "Enable or disable collision with Jiggle Colliders";
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

    void SetSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id, string parameter, string propertyName, string tooltip) {
        var sliderElement = visualElement.Q<Slider>(id);
        var property = serializedProperty.FindPropertyRelative(parameter);
        sliderElement.BindProperty(property);
        sliderElement.tooltip = tooltip;
        sliderElement.Q<Label>().text = propertyName;
    }

    void SetCurvableSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id, string curvableFloatParameter, string propertyName, float min, float max, string tooltip) {
        var curvableFloatProperty = serializedProperty.FindPropertyRelative(curvableFloatParameter);
        var floatProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.value));
        var toggleProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.curveEnabled));
        var curveProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.curve));
        
        var sliderElement = visualElement.Q<VisualElement>(id);
        var sliderElementSlider = sliderElement.Q<Slider>("CurvableSlider");
        sliderElementSlider.BindProperty(floatProperty);
        sliderElementSlider.lowValue = min;
        sliderElementSlider.highValue = max;
        sliderElementSlider.tooltip = tooltip;
        sliderElementSlider.Q<Label>().text = propertyName;
        
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.tooltip = tooltip;
        stiffnessCurveElement.BindProperty(curveProperty);
        
        var toggle = sliderElement.Q<Toggle>("CurvableToggle");
        toggle.BindProperty(toggleProperty);
        toggle.tooltip = "Enable or disable curve sampling for this value based on the normalized distance from the root.";
        stiffnessCurveElement.style.display = toggleProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        toggle.RegisterValueChangedCallback(evt => {
            stiffnessCurveElement.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });
    }

    void SetCurvableFloat(VisualElement visualElement, SerializedProperty serializedProperty, string id, string curvableFloatParameter, string propertyName) {
        var curvableFloatProperty = serializedProperty.FindPropertyRelative(curvableFloatParameter);
        var floatProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.value));
        var toggleProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.curveEnabled));
        var curveProperty = curvableFloatProperty.FindPropertyRelative(nameof(JiggleTreeCurvedFloat.curve));
        
        var sliderElement = visualElement.Q<VisualElement>(id);
        var curvableFloat = sliderElement.Q<FloatField>("CurvableFloat");
        curvableFloat.BindProperty(floatProperty);
        curvableFloat.Q<Label>().text = propertyName;
        
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.BindProperty(curveProperty);
        
        var toggle = sliderElement.Q<Toggle>("CurvableToggle");
        toggle.BindProperty(toggleProperty);
        stiffnessCurveElement.style.display = toggleProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        toggle.RegisterValueChangedCallback(evt => {
            stiffnessCurveElement.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });
    }

#endif

}

}
