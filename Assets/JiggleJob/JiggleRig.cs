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
    [SerializeField] protected bool _animated;
    [SerializeField] protected bool _excludeRoot;
    [SerializeField] protected JiggleBoneInputParameters _jiggleBoneInputParameters;
    [SerializeField] protected List<Transform> _excludedTransforms = new List<Transform>();
    
    private MonobehaviourHider.JiggleRoot _root;
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
        _root = _rootBone.gameObject.AddComponent<MonobehaviourHider.JiggleRoot>();
        _root.rig = this;
    }

    private void OnDisable() {
        if (_root != null) {
            Destroy(_root);
        }
    }

    public JiggleBoneParameters GetJiggleBoneParameter(float normalizedDistance) {
        return _jiggleBoneInputParameters.ToJiggleBoneParameters();
    }

    private Transform[] GetJiggleBoneTransforms() {
        var transforms = _rootBone.GetComponentsInChildren<Transform>();
        return transforms;
    }

    void OnValidate() {
        isValid = _rootBone.IsChildOf(gameObject.transform);
        if (_excludedTransforms == null) _excludedTransforms = new List<Transform>();
    }
    
    public JiggleBoneSimulatedPoint[] GetJiggleBoneSimulatedPoints() {
        
        // TODO: Need to create the entire structure temporarily as a list, so that we can
        // TODO: collapse the ~zero length bones out of the list before turning it into an array.
        
        var transforms = GetJiggleBoneTransforms();
        var childlessTransformIndices = new List<int>();
        var tails = 0;
        for (var index = 0; index < transforms.Length; index++) {
            var transform = transforms[index];
            if (transform.childCount != 0) continue;
            childlessTransformIndices.Add(index);
            tails++;
        }

        var points = new JiggleBoneSimulatedPoint[1+transforms.Length+tails];
        unsafe {
            for (int i = 0; i < points.Length; i++) {
                var transformIndex = i - 1;
            
                // BACK PROJECTED VIRTUAL ROOT
                if (transformIndex < 0) {
                    points[i] = new JiggleBoneSimulatedPoint {
                        parentIndex = -1,
                        parameters = _jiggleBoneInputParameters.ToJiggleBoneParameters(),
                        transformIndex = -1,
                        animated = false
                    };
                    continue;
                }
            
                // FORWARD PROJECTED TAILS
                var tailIndex = transformIndex - transforms.Length;
                if (tailIndex >= 0) {
                    points[i] = new JiggleBoneSimulatedPoint {
                        parentIndex = childlessTransformIndices[tailIndex]+1,
                        parameters = _jiggleBoneInputParameters.ToJiggleBoneParameters(),
                        transformIndex = -1,
                        animated = false
                    };
                    continue;
                }
            
                // TRANSFORMS
                var parentIndex = -1;
                if (transformIndex == 0) {
                    parentIndex = 0;
                } else {
                    for (int parentSearchIndex = 0; parentSearchIndex < transforms.Length; parentSearchIndex++) {
                        if (transforms[parentSearchIndex] != transforms[transformIndex].parent) continue;
                        parentIndex = parentSearchIndex + 1;
                        break;
                    }
                }
                points[i] = new JiggleBoneSimulatedPoint {
                    position = transforms[transformIndex].position,
                    lastPosition = transforms[transformIndex].position,
                    parentIndex = parentIndex,
                    parameters = _jiggleBoneInputParameters.ToJiggleBoneParameters(),
                    transformIndex = transformIndex,
                    animated = _animated
                };
            }
            
            // Assign child indices by searching for parents
            for (int i = 0; i < points.Length; i++) {
                int childIndex = 0;
                if (i < points.Length - tails) {
                    for (int childCheckIndex = i+1; childCheckIndex < points.Length; childCheckIndex++) {
                        if (points[childCheckIndex].parentIndex == i) {
                            points[i].childrenIndices[childIndex] = childCheckIndex;
                            childIndex++;
                        }
                    }
                }
                points[i].childenCount = childIndex;
                while (childIndex < JiggleBoneSimulatedPoint.MAX_CHILDREN) {
                    points[i].childrenIndices[childIndex] = -1;
                    childIndex++;
                }
            }

            //for (int i = 0; i < points.Length; i++) {
            //    for (int j = 0; j < JiggleBoneSimulatedPoint.MAX_CHILDREN; j++) {
            //        Debug.Log(points[i].childrenIndices[j]);
            //    }
            //    Debug.Log("----");
            //}
        }

        return points;
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
        var floatElement = visualElement.Q<FloatField>("GravityField");
        floatElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.gravity)));
        floatElement.Q<Label>().text = "Gravity";
        
        var advancedToggleElement = visualElement.Q<Toggle>("AdvancedToggle");
        advancedToggleElement.BindProperty(serializedProperty.FindPropertyRelative(nameof(JiggleBoneInputParameters.advancedToggle)));
        advancedToggleElement.Q<Label>().text = "Advanced";
        
        var detailsSection = visualElement.Q<VisualElement>("AdvancedSection");
        advancedToggleElement.RegisterValueChangedCallback(evt => {
            detailsSection.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
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
