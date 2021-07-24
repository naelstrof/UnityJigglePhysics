// This script I use to mirror my R and G channels of the vertex colors.
// It's real useful because I can just leave my model with a mirror modifier in blender.
// It should only proc on my Kobold Model (or any with a "VagExpand" blendshape).


// Copyright 2019 Vilar24

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is furnished 
// to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS 
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace JigglePhysicsExample {
    public class ColorSplitter : AssetPostprocessor {
        private void OnPostprocessModel(GameObject g) {
            Apply(g.transform);
        }

        private void Apply(Transform t) {
            CreateAsymmetricalBlendShapes(t.gameObject, 0.04f);
            foreach (Transform child in t)
                Apply(child);
        }

        private void CreateAsymmetricalBlendShapes(GameObject gameObject, float blendDistance) {
            SkinnedMeshRenderer mesh = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (mesh == null) return;
            bool foundKobold = false;
            for (int i = 0; i < mesh.sharedMesh.blendShapeCount; i++) {
                if (mesh.sharedMesh.GetBlendShapeName(i) == "VagExpand") {
                    foundKobold = true;
                }
            }
            if (!foundKobold ) {
                return;
            }
            List<Color> colors = new List<Color>();
            List<Vector3> verts = new List<Vector3>();
            mesh.sharedMesh.GetColors(colors);
            mesh.sharedMesh.GetVertices(verts);
            float maxx = 0f;
            foreach( Vector3 v in verts ) {
                maxx = Mathf.Max(Mathf.Abs(v.x), maxx);
            }
            List<Color> newColors = new List<Color>();
            for (int i = 0; i < mesh.sharedMesh.vertexCount; i++) {
                Vector3 v = verts[i];
                Color c = colors[i];
                Color ci = new Color(c.g,c.r,c.b,c.a);
                float d = Mathf.Clamp01((((v.x / maxx)/blendDistance)+1f)/2f);
                Color newColor = new Color();
                newColor = Color.Lerp(c, ci, d);
                newColors.Add(newColor);
            }
            mesh.sharedMesh.SetColors(newColors);
        }
    }
}
#endif