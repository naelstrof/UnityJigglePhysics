using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeScaleSlider : MonoBehaviour {
    private Slider slider;
    private float startingFixedDeltaTime;
    private void Awake() {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderChanged);
        startingFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void OnSliderChanged(float arg0) {
        Time.timeScale = arg0;
        Time.fixedDeltaTime = startingFixedDeltaTime * arg0;
    }
}
