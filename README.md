# Unity JigglePhysics

This readme is a work in progress as we incorporate features and demos, check back later!

[![openupm](https://img.shields.io/npm/v/com.gator-dragon-games.jigglephysics?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.gator-dragon-games.jigglephysics/)

A jobs-based relativistic squash-and-stretch jiggle physics solution for transforms in Unity.

## Features

- Utilizes the Unity Jobs system for performance, allowing for 600 jiggling Stanford Armadillos within 2-4ms.
  
[600 Stanford Armadillo Dance](https://github.com/user-attachments/assets/2d7047a8-c386-468e-a495-4871334df38a)

- Collisions
- Relativistic, etc

## Installation

Simply add `https://github.com/naelstrof/UnityJigglePhysics.git#upm` as a package using the package manager. Ideally you'd want to use a real version tag like `v13.0.0` in place of `upm` in a build system though!

Or if that doesn't work, add it to the manifest.json like so.

```
{
  "dependencies": {
    "com.naelstrof.jigglephysics": "https://github.com/naelstrof/UnityJigglePhysics.git#upm",
  }
}
```


## Settings

Jiggle Physics is parameterized to handle a wide variety of computed secondary motion needs. Described below are the parameters and their functions.

### Stiffness
Stiffness is the core parameter to control the behavior of the motion. This controls how much force is applied to move the bones back to their target angle.

![JiggleSettings_Stiffness](https://github.com/user-attachments/assets/8cea1900-0d21-4999-99b8-11608cd475e1)

0: Bones can move without constraint

0.5: Bones are moved toward their target angle as defined by their animation or rest pose

1: bones are moved all the way to their target animation or rest pose angle

### Stretch
Stretch allows bone chains to separate or crowd together to simulate squash and stretch. This controls how much force is applied to enforce the target bone length.

![JiggleSettings_Stretch](https://github.com/user-attachments/assets/2308bc48-e121-4e22-9097-06232c66aca3)

0: Bones are forced all the way to their target bone length as defined by the animation or rest pose

0.5: Bones are moved toward their target bone length half as much as they are moved toward their angle target

1: Bones are moved toward their target bone length only as much as the stiffness moves them toward their angle target

### Soften
In some situations, bones might need to be quite stiff without eliminating subtle oscillation near the rest pose. Soften eases the stiffness the nearer the bone is to its rest pose. This effect allows breasts to move subtlely more in a neutral pose without going all over the place.

![JiggleSettings_Soften](https://github.com/user-attachments/assets/93395b9c-a653-4ee2-a8c8-706ee3f905a8)

0: Constraints are not adjusted

0.5: Constratins are reduced near the target pose

1: Constraints are greatly reduced near the target pose

### Root Stretch
Root Stretch allows the root bone to move away from its pose position on a simple spring joint

![JiggleSettings_RootStretch](https://github.com/user-attachments/assets/dbb06f96-5fc8-4122-b1b6-4e880258964d)

0: No root movement is allowed

0.5: Root bone may move freely and is pulled back to its rest position by a spring constraint

### Drag
Drag represents mechanical friction, slowing the motion of bones in local space

![JiggleSettings_Drag](https://github.com/user-attachments/assets/d5cd22fe-cedd-4434-be63-177950dfa274)

0: Bones may oscillate freely

0.5: Bones are slowed as they oscillate toward their rest position

1: Bone motion is greatly dampened

### Angle Limit
In some circumstances, bones must never exceed a maximum angle offset. This parameter prevents that issue.

![JiggleSettings_AngleLimit](https://github.com/user-attachments/assets/6a0e6016-9b4b-4555-81aa-c11f3d863ea2)

0: Bones may not deviate from their rest angle at all

0.5: Bones may deviate by up to 45 degrees

1: bones may deviate by up to 90 degrees

### Angle Limit Soften
Angle limit corrections can be softened to reduce the hard bump at their limit

![JiggleSettings_AngleLimitSoften](https://github.com/user-attachments/assets/f866d110-322f-4a3f-880a-81a08e85678f)

0: Bones hit angle limits hard

1: Bones are corrected only partially each simulation step, softening the limit
