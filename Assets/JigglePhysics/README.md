# Unity Jiggle Physics

[![openupm](https://img.shields.io/npm/v/com.naelstrof.jigglephysics?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.naelstrof.jigglephysics/)

An acceleration-based jigglebone system with soft-body/squash-and-stretch physics for SkinnedMeshRenderers in Unity.

## Features

* GPU per-vertex deformations via shader, squash/stretch based on vertex color masks.

![kobold breakdance](https://github.com/naelstrof/UnityJigglePhysics/raw/main/breakdanceDemo.gif)

* Purely acceleration based, elevators don't cause things to float/sag.

![kobold elevator](https://github.com/naelstrof/UnityJigglePhysics/raw/main/accelerationDemo.gif)

* Works at all framerates with very little differences in the physics solve. (It's unhooked from a fixed timestep as well!)

* Can run on LateUpdate, FixedUpdate, and Update.

* Only runs when mesh is visible.

* Comes with a JiggleSurfaceApproximator which can keep a transform attached to the "skin" of a mesh. Great for censorbars and attachments that can't be added directly to the mesh.

## Usage

This unity repository actually comes with a fully working project, with a working model in an example scene. If anything below doesn't make sense, download the project!

1. Use the included AmplifyShader node to add the vertex deformations to whatever shader you want. (URP, HDRP, Standard, whatever!).

![amplify node setup](https://github.com/naelstrof/UnityJigglePhysics/raw/main/amplifySetup.png)

2. Make sure your model has RGBA vertex-color masks for how bouncy parts of the model is, keep individual neighboring bouncing parts as different colors.

![kobold vertex color setup](https://github.com/naelstrof/UnityJigglePhysics/blob/main/vertexColorSetup.png)

3. Set up a JiggleSoftbody with sensors overlapped in such a way that each "individual" bouncy part has its own sensor.

![kobold softbody setup](https://github.com/naelstrof/UnityJigglePhysics/raw/main/softbodySetup.png)

4. Add JiggleBones for the primary movement.

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
