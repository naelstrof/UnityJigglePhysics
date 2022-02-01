# Unity JigglePhysics

[![openupm](https://img.shields.io/npm/v/com.naelstrof.jigglephysics?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.naelstrof.jigglephysics/)

A relativistic squash-and-stretch jigglebone physics solution for characters in Unity.

[Video demo on youtube.](https://www.youtube.com/watch?v=WHCv2lXbbTk)

## Features

* Supports per-bone squash and stretch, as well as per-vertex squash and stretch.

![SquishyDemo](https://github.com/naelstrof/UnityJigglePhysics/raw/main/squashNstretch.gif)

* Realistically relativistic! Elevators, trains, and vehicles won't leave jigglebones permanently trailing behind.

![RelativisticDemo](https://github.com/naelstrof/UnityJigglePhysics/raw/main/relativistic.gif)

* Works seamlessly on physics objects without jitter, with no configuration!

* Supports animated skeletons, JigglePhysics uses it as a target reference pose.

* Realtime rig scaling support, freely change character proportions on the fly.

![ProportionDemo](https://github.com/naelstrof/UnityJigglePhysics/raw/main/proportion.gif)

* ScriptableObject-based configuration, share settings across projects and prefabs.

![ScriptableObjectDemo](https://github.com/naelstrof/UnityJigglePhysics/raw/main/ScriptableObjectExample.png)


## Usage

A unity repository full of examples can be found here: https://github.com/naelstrof/UnityJigglePhysics. If anything below doesn't make sense, download the examples!

## How to Jiggle a Rig

1. Have a cool model attached to a bunch of transforms. SkinnedMeshRenderers look best!

![A model of a purple lady](https://github.com/naelstrof/UnityJigglePhysics/raw/main/model_example.png)

2. Create a JiggleRigBuilder Monobehaviour on the object, and select the root bones you want to jiggle by adding them to the list of JiggleRigs.

![Unity inspector animation of adding a jiggle rig and dragging a tail in.](https://github.com/naelstrof/UnityJigglePhysics/raw/main/CreateJiggleRig.gif)

3. Create a JiggleSettings ScriptableObject in the project through the `Create->JigglePhysics->Settings` menu. Then make sure that each JiggleRig has a reference to a JiggleSetting.

![Unity inspector animation of creating a jiggle setting and dragging it into the JiggleRigBuilder.](https://github.com/naelstrof/UnityJigglePhysics/raw/main/CreateJiggleSettings.gif)

4. Play! You can adjust the jiggle settings during play mode and see changes live-- And when you exit play mode, the settings should stick!

## How to Jiggle a Skin (Advanced)

1. Have a cool skinned model, doesn't need very many transforms, but does need to be a SkinnedMeshRenderer.

![The cutest little blob of a model.](https://github.com/naelstrof/UnityJigglePhysics/raw/main/SlimeGuy.png)

2. (Optional) Make sure the model has some sort of mask. By default the shaders use the Red vertex color channel to mask out the motion. I like to use Blender and Vertex Color Master for this.

![A blob with the most jiggly parts being redder than the non-jiggly parts.](https://github.com/naelstrof/UnityJigglePhysics/raw/main/JiggleMask.png)

3. Create a JiggleSkin-supported shader with either Amplify Shader Editor, or Shader Graph for your specified shader pipeline.

With ASE, there should be a custom node automatically added to your list of available nodes: `Jiggle Physics Softbody`.

![A simple example graph using ASE](https://github.com/naelstrof/UnityJigglePhysics/raw/main/ASEexample.png)

With ShaderGraph, you must use a CustomFunction node, and use the hlsl file found at `JigglePhysics/Shaders/JigglePhysicsSoftbodyShaderGraph.hsl`.

![A simple example graph using Shader Graph](https://github.com/naelstrof/UnityJigglePhysics/raw/main/ShaderGraphExample.png)

4. Add a JiggleSkin MonoBehavior to the object, and add the desired list of JiggleZones, with their specified JiggleSettings.

![A Unity Editor animation of setting up the JiggleSkin MonoBehaviour](https://github.com/naelstrof/UnityJigglePhysics/raw/main/JiggleSkinSetup.gif)

5. Ensure the target skins have a JiggleSkin-supported shader applied, then Play!

![A jiggly blob](https://github.com/naelstrof/UnityJigglePhysics/raw/main/SlimeGuyJiggle.gif)


## Installation

You can install Jiggle Physics with OpenUPM by checking out the badge above. Otherwise...

Simply add `https://github.com/naelstrof/UnityJigglePhysics.git#upm` as a package using the package manager.

Or if that doesn't work, add it to the manifest.json like so.

```
{
  "dependencies": {
    "com.naelstrof.jigglephysics": "https://github.com/naelstrof/UnityJigglePhysics.git#upm",
  }
}
```
