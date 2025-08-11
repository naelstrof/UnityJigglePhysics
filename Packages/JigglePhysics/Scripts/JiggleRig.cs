using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

public class JiggleRig : MonoBehaviour {

    [SerializeField] protected Transform _rootBone;
    [SerializeField] protected bool _advanced;
    [SerializeField] protected bool _animated;
    [SerializeField] protected bool _excludeRoot;
    [SerializeField] protected JiggleBoneInputParameters _jiggleBoneInputParameters;
    [SerializeField] protected List<Transform> _excludedTransforms = new List<Transform>();
    
    private JiggleTreeSegment _jiggleTreeSegment;
    bool isValid = false;
    public bool rootExcluded => _excludeRoot;
    public Transform rootBone => _rootBone;
    public bool CheckExcluded(Transform t) => _excludedTransforms.Contains(t);

    public bool rootTransformError => !(_rootBone == null || isValid);

    private void OnEnable() {
        //JiggleJobManager.AddJiggleTree(new JiggleTree(GetJiggleBoneTransforms(), GetJiggleBoneSimulatedPoints()));
        /*JiggleRig parentMostRootBone = null;
        var childRigs = GetComponentsInChildren<JiggleRig>();
        while (childRigs.Length > 0) {
            var rig = childRigs[0];
            parentMostRootBone = rig;
            for (Transform t = rig._rootBone; t != transform; t = t.parent) {
                for (int i = 0; i < childRigs.Length; i++) {
                    if (childRigs[i] == rig) {
                        continue;
                    }
                    if (t == childRigs[i]._rootBone) {
                        parentMostRootBone = childRigs[i];
                    }
                }
            }
            List<JiggleRig> finished = new List<JiggleRig>();
            // DO DFS HERE
        }*/
        _jiggleTreeSegment ??= new JiggleTreeSegment(_rootBone, this);
        _jiggleTreeSegment.SetDirty();
        JiggleTreeUtility.AddJiggleTreeSegment(_jiggleTreeSegment);
    }

    private void OnDisable() {
        JiggleTreeUtility.RemoveJiggleTreeSegment(_jiggleTreeSegment);
    }

    public JiggleBoneParameters GetJiggleBoneParameter(float normalizedDistanceFromRoot) {
        return _jiggleBoneInputParameters.ToJiggleBoneParameters(normalizedDistanceFromRoot);
    }

    public Transform[] GetJiggleBoneTransforms() {
        var transforms = _rootBone.GetComponentsInChildren<Transform>();
        return transforms;
    }

    void OnValidate() {
        isValid = _rootBone.IsChildOf(gameObject.transform);
        if (_excludedTransforms == null) _excludedTransforms = new List<Transform>();
        ValidateCurve(ref _jiggleBoneInputParameters.stiffnessCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.angleLimitCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.stretchCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.dragCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.airDragCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.gravityCurve);
        ValidateCurve(ref _jiggleBoneInputParameters.collisionRadiusCurve);
    }

    void ValidateCurve(ref AnimationCurve animationCurve) {
        if (animationCurve == null || animationCurve.length == 0) {
            animationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        }
    }
    
#if UNITY_EDITOR
    public VisualElement GetInspectorVisualElement(SerializedProperty serializedProperty) {
        var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("c35a2123f4d44dd469ccb24af7a0ce20"));
        var visualElement = new VisualElement();
        visualTreeAsset.CloneTree(visualElement);
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "StiffnessControl",
            nameof(JiggleBoneInputParameters.stiffness),
            nameof(JiggleBoneInputParameters.stiffnessCurve),
            "Stiffness"
        );
        
        var angleLimitToggleElement = visualElement.Q<Toggle>("AngleLimitToggle");
        angleLimitToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.angleLimitToggle)));
        angleLimitToggleElement.Q<Label>().text = "Angle Limit";
        
        var angleLimitSection = visualElement.Q<VisualElement>("AngleLimitSection");
        angleLimitToggleElement.RegisterValueChangedCallback(evt => {
            angleLimitSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "AngleLimitControl",
            nameof(JiggleBoneInputParameters.angleLimit),
            nameof(JiggleBoneInputParameters.angleLimitCurve),
            "Angle Limit"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "AngleLimitSoftenSlider",
            nameof(JiggleBoneInputParameters.angleLimitSoften),
            "Angle Limit Soften"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "SoftenSlider",
            nameof(JiggleBoneInputParameters.soften),
            "Soften"
        );
        SetSlider(
            visualElement,
            serializedProperty,
            "RootStretchSlider",
            nameof(JiggleBoneInputParameters.rootStretch),
            "Root Stretch"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "StretchControl",
            nameof(JiggleBoneInputParameters.stretch),
            nameof(JiggleBoneInputParameters.stretchCurve),
            "Stretch"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "DragControl",
            nameof(JiggleBoneInputParameters.drag),
            nameof(JiggleBoneInputParameters.dragCurve),
            "Drag"
        );
        SetCurvableSlider(
            visualElement,
            serializedProperty,
            "AirDragControl",
            nameof(JiggleBoneInputParameters.airDrag),
            nameof(JiggleBoneInputParameters.airDragCurve),
            "Air Drag"
        );

        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "GravityControl",
            nameof(JiggleBoneInputParameters.gravity),
            nameof(JiggleBoneInputParameters.gravityCurve),
            "Gravity"
        );
        
        SetCurvableFloat(
            visualElement,
            serializedProperty,
            "CollisionRadiusControl",
            nameof(JiggleBoneInputParameters.collisionRadius),
            nameof(JiggleBoneInputParameters.collisionRadiusCurve),
            "Collision Radius"
        );
        
        var advancedToggleElement = visualElement.Q<Toggle>("AdvancedToggle");
        advancedToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.advancedToggle)));
        advancedToggleElement.Q<Label>().text = "Advanced";
        
        var collisionToggleElement = visualElement.Q<Toggle>("CollisionToggle");
        collisionToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.collisionToggle)));
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

    void SetSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id, string parameter, string name) {
        var sliderElement = visualElement.Q<Slider>(id);
        sliderElement.BindProperty(serializedProperty.FindPropertyRelative(parameter));
        sliderElement.Q<Label>().text = name;
    }

    void SetCurvableSlider(VisualElement visualElement, SerializedProperty serializedProperty, string id, string sliderParameter, string curveParameter, string name) {
        var sliderElement = visualElement.Q<VisualElement>(id);
        var sliderElementSlider = sliderElement.Q<Slider>("CurvableSlider");
        sliderElementSlider.BindProperty(serializedProperty.FindPropertyRelative(sliderParameter));
        sliderElementSlider.Q<Label>().text = name;
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.BindProperty(serializedProperty.FindPropertyRelative(curveParameter));
    }

    void SetCurvableFloat(VisualElement visualElement, SerializedProperty serializedProperty, string id, string floatParameter, string curveParameter, string name) {
        var sliderElement = visualElement.Q<VisualElement>(id);
        var curvableFloat = sliderElement.Q<FloatField>("CurvableFloat");
        curvableFloat.BindProperty(serializedProperty.FindPropertyRelative(floatParameter));
        curvableFloat.Q<Label>().text = name;
        var stiffnessCurveElement = sliderElement.Q<CurveField>("CurvableCurve");
        stiffnessCurveElement.BindProperty(serializedProperty.FindPropertyRelative(curveParameter));
    }

#endif
    
}
