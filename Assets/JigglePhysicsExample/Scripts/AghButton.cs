using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;

public static class AghButton {
    [MenuItem("KoboldKare/HomogenizeButtons")]
    public static void HomogenizeButtons() {
        ColorBlock block = new ColorBlock();
        block.normalColor = Color.white;
        block.colorMultiplier = 1f;
        block.highlightedColor = new Color(0.49f,1f,0.9435571f, 1f);
        block.pressedColor = new Color(0.1090246f, 0.4716981f, 0.7129337f, 1f);
        block.selectedColor = new Color(0.2559185f, 0.764151f, 0.7129337f, 1f);
        block.disabledColor = new Color(0.7843137f, 0.7843137f, 0.7843137f, 0.5019608f);
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Changed button colors");
        var undoIndex = Undo.GetCurrentGroup();
        foreach(GameObject g in Selection.gameObjects) {
            foreach(Button b in g.GetComponentsInChildren<Button>(true)) {
                Undo.RecordObject(b, "Changed button color");
                b.colors = block;
                EditorUtility.SetDirty(b);
            }
        }
        Undo.CollapseUndoOperations(undoIndex);
    }
    [MenuItem("KoboldKare/Disable all GPU Instanced Materials")]
    public static void FindGPUInstancedMaterial() {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Changed project gpu instancing");
        var undoIndex = Undo.GetCurrentGroup();
        string[] pathsToAssets = AssetDatabase.FindAssets("t:Material");
        foreach (var path in pathsToAssets) {
            var path1 = AssetDatabase.GUIDToAssetPath(path);
            var go = AssetDatabase.LoadAssetAtPath<Material>(path1);
            if (go.enableInstancing) {
                Undo.RecordObject(go, "Changed project gpu instancing");
                go.enableInstancing = false;
                EditorUtility.SetDirty(go);
            }
        }
        Undo.CollapseUndoOperations(undoIndex);
    }
    [MenuItem("KoboldKare/Find Specular Workflow Materials")]
    public static void FindSpecularWorkflowMaterials() {
        string[] pathsToAssets = AssetDatabase.FindAssets("t:Material");
        foreach (var path in pathsToAssets) {
            var path1 = AssetDatabase.GUIDToAssetPath(path);
            var go = AssetDatabase.LoadAssetAtPath<Material>(path1);
            if (go.IsKeywordEnabled("_SPECULAR_SETUP")) {
                Selection.activeObject = go;
                return;
            }
        }
    }
    [MenuItem("KoboldKare/Enable all environment reflections, specular highlights (to reduce shader variants.)")]
    public static void FindSpecularOffMaterials() {
        string[] pathsToAssets = AssetDatabase.FindAssets("t:Material");
        foreach (var path in pathsToAssets) {
            var path1 = AssetDatabase.GUIDToAssetPath(path);
            var go = AssetDatabase.LoadAssetAtPath<Material>(path1);
            if (go.IsKeywordEnabled("_SPECULARHIGHTLIGHTS_OFF")) {
                go.DisableKeyword("_SPECULARHIGHTLIGHTS_OFF");
            }
            if (go.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF")) {
                go.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            if (go.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")) {
                go.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            EditorUtility.SetDirty(go);
        }
    }
    [MenuItem("KoboldKare/Find Missing Script")]
    public static void FindMissingScript() {
        foreach(GameObject g in Selection.gameObjects) {
            foreach(var c in g.GetComponents<Component>()) {
                if (c == null) {
                    if (g.hideFlags == HideFlags.HideAndDontSave || g.hideFlags == HideFlags.HideInHierarchy || g.hideFlags == HideFlags.HideInInspector) {
                        Debug.Log("Found hidden gameobject with a missing script, deleted " + g);
                        GameObject.DestroyImmediate(g);
                        continue;
                    }
                    Selection.activeGameObject = g;
                    return;
                }
            }
        }
        foreach(var g in Object.FindObjectsOfType<GameObject>()) {
            foreach(var c in g.GetComponents<Component>()) {
                if (c == null) {
                    if (g.hideFlags == HideFlags.HideAndDontSave || g.hideFlags == HideFlags.HideInHierarchy || g.hideFlags == HideFlags.HideInInspector) {
                        Debug.Log("Found hidden gameobject with a missing script, deleted " + g);
                        GameObject.DestroyImmediate(g);
                        continue;
                    }
                    Selection.activeGameObject = g;
                    return;
                }
            }
        }
        string[] pathsToAssets = AssetDatabase.FindAssets("t:GameObject");
        foreach (var path in pathsToAssets) {
            var path1 = AssetDatabase.GUIDToAssetPath(path);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path1);
            foreach(var c in go.GetComponentsInChildren<Component>(true)) {
                if (c == null) {
                    Selection.activeGameObject = go;
                    return;
                }
            }
        }
        Debug.Log("No missing scripts found anywhere! Good job.");
    }
}

#endif