using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUD : MonoBehaviour {
    void Start() {
        var slider = GetComponent<UIDocument>().rootVisualElement.Q<Slider>("FPS");
        slider.RegisterValueChangedCallback(FpsSliderChanged);
        slider.value = 100f;
        
        var timescaleSlider = GetComponent<UIDocument>().rootVisualElement.Q<Slider>("Timescale");
        timescaleSlider.RegisterValueChangedCallback(TimesScaleSliderChanged);
        timescaleSlider.value = 1f;
    }

    private void TimesScaleSliderChanged(ChangeEvent<float> evt) {
        Time.timeScale = evt.newValue;
    }

    private void FpsSliderChanged(ChangeEvent<float> evt) {
        Application.targetFrameRate = (int)evt.newValue;
    }
    
}
