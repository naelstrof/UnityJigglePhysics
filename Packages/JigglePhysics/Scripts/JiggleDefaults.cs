#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;

namespace JigglePhysics {
public static class JiggleDefaults {
    private static string GetActiveFolderPath() {
        // Can't believe we need to use reflection to call this method!
        MethodInfo getActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
        string folderPath = (string)getActiveFolderPath.Invoke(null, null);
        return folderPath;
    }
    [MenuItem("Assets/Create/JigglePhysics/Example breast settings", false, 15)]
    private static void CreateExampleBreastJiggle() {
        JiggleSettings breast = (JiggleSettings)ScriptableObject.CreateInstance(typeof(JiggleSettings));
        breast.SetData(new JiggleSettingsData {
            airDrag = 0.01f, // Breasts don't catch the wind much at all.
            friction = 0.05f, // Low friction means more jiggling! Breasts jiggle lots before coming to rest with a value of 0.05
            gravityMultiplier = 0.1f, // Gravity is usually already expressed in a default breast shape, we still want just a little effect if the character is sideways or upside-down though!
            blend = 1f, // Full blend by default, user can adjust this themselves.
            angleElasticity = 0.6f, // High angle elasticity, breasts shouldn't invert inside out or point straight up and down very much.
            elasticitySoften = 0.2f, // Breasts don't perfectly hold their shape, they are allowed quite a bit of free movement.
            lengthElasticity = 0.4f, // Breasts aren't exactly stretchy, though personally I really like squash and stretch movement. So the settings here have lots of stretch.
        });
        ProjectWindowUtil.CreateAsset(breast, GetActiveFolderPath() + "/BreastJiggleSettings.asset");
    }
    [MenuItem("Assets/Create/JigglePhysics/Example tail settings", false, 16)]
    private static void CreateExampleTailJiggle() {
        JiggleSettings tail = (JiggleSettings)ScriptableObject.CreateInstance(typeof(JiggleSettings));
        tail.SetData(new JiggleSettingsData {
            airDrag = 0.4f, // Tail flaps in the wind! Trails behind the character during movement.
            friction = 0.3f, // High friction, tails don't wag unless the owner means to generally. It's a limb for balance!
            gravityMultiplier = 0.7f, // Gravity should be partially expressed in the default shape of the tail, but it absolutely needs to flip and fall depending on orientation.
            blend = 1f, // Full blend by default, user can adjust this themselves.
            angleElasticity = 0.2f, // Low angle elasticity, should sweep into position slowly, intentionally.
            elasticitySoften = 0.5f, // Tails don't really care about being "perfect".
            lengthElasticity = 0.6f, // With such low angle elasticity, we need a decent amount of length elasticity to make sure the tail doesn't just stretch into oblivion. It still streches a little though!
        });
        ProjectWindowUtil.CreateAsset(tail, GetActiveFolderPath() + "/TailJiggleSettings.asset");
    }
    [MenuItem("Assets/Create/JigglePhysics/Example softbody settings", false, 17)]
    private static void CreateExampleSoftbody() {
        // I assume softbody is something like belly jiggle.
        JiggleSettings softbody = (JiggleSettings)ScriptableObject.CreateInstance(typeof(JiggleSettings));
        softbody.SetData(new JiggleSettingsData {
            airDrag = 0.01f, // Bellies don't catch the air much either.
            friction = 0.1f, // Low friction for jigglier bellies.
            gravityMultiplier = 0.1f, // Gravity is probably already expressed in the model.
            blend = 1f, // Full blend by default, user can adjust this themselves.
            angleElasticity = 0.5f, // Angles aren't used at all for jiggle skins, useless.
            elasticitySoften = 0.15f, // Similar to breasts, bellies don't perfectly hold their shape. Though they probably hold their shape better than breasts do.
            lengthElasticity = 0.35f, // Depending on how sharply the weighting of the softbody jiggle is, this value will matter a lot. I left it **very** jiggly.
        });
        ProjectWindowUtil.CreateAsset(softbody, GetActiveFolderPath() + "/SoftbodyJiggleSettings.asset");
    }
}

}

#endif
