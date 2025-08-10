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
    }

    private void OnDisable() {
        if (_jiggleTreeSegment != null) {
            JiggleTreeUtility.RemoveJiggleTreeSegment(_jiggleTreeSegment);
            _jiggleTreeSegment = null;
        }
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
        SetSlider(visualElement, serializedProperty,
            "StiffnessSlider",
            nameof(JiggleBoneInputParameters.stiffness),
            "Stiffness"
        );
        
        var stiffnessCurveElement = visualElement.Q<CurveField>("StiffnessCurve");
        stiffnessCurveElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.stiffnessCurve)));
        stiffnessCurveElement.Q<Label>().text = "Stiffness Curve";
        
        var angleLimitToggleElement = visualElement.Q<Toggle>("AngleLimitToggle");
        angleLimitToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.angleLimitToggle)));
        angleLimitToggleElement.Q<Label>().text = "Angle Limit";
        
        var angleLimitSection = visualElement.Q<VisualElement>("AngleLimitSection");
        angleLimitToggleElement.RegisterValueChangedCallback(evt => {
            angleLimitSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        });

        SetSlider(visualElement, serializedProperty,
            "AngleLimitSlider",
            nameof(JiggleBoneInputParameters.angleLimit),
            "Angle Limit"
        );
        SetSlider(visualElement, serializedProperty,
            "AngleLimitSoftenSlider",
            nameof(JiggleBoneInputParameters.angleLimitSoften),
            "Angle Limit Soften"
        );
        SetSlider(visualElement, serializedProperty,
            "SoftenSlider",
            nameof(JiggleBoneInputParameters.soften),
            "Soften"
        );
        SetSlider(visualElement, serializedProperty,
            "RootStretchSlider",
            nameof(JiggleBoneInputParameters.rootStretch),
            "Root Stretch"
        );
        SetSlider(visualElement, serializedProperty,
            "StretchSlider",
            nameof(JiggleBoneInputParameters.stretch),
            "Stretch"
        );
        SetSlider(visualElement, serializedProperty,
            "DragSlider",
            nameof(JiggleBoneInputParameters.drag),
            "Drag"
        );
        SetSlider(visualElement, serializedProperty,
            "AirDragSlider",
            nameof(JiggleBoneInputParameters.airDrag),
            "Air Drag"
        );
        var gravityElement = visualElement.Q<FloatField>("GravityField");
        gravityElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.gravity)));
        gravityElement.Q<Label>().text = "Gravity";
        
        var collisionRadiusElement = visualElement.Q<FloatField>("CollisionRadiusField");
        collisionRadiusElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.collisionRadius)));
        collisionRadiusElement.Q<Label>().text = "Collision Radius";
        
        var collisionCurveElement = visualElement.Q<CurveField>("CollisionRadiusCurve");
        collisionCurveElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.collisionRadiusCurve)));
        collisionCurveElement.Q<Label>().text = "Collision Radius Curve";
        
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
            stiffnessCurveElement.style.display = _jiggleBoneInputParameters.advancedToggle ? DisplayStyle.Flex : DisplayStyle.None;
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
#endif
    
}
