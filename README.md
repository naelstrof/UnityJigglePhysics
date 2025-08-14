# Unity JigglePhysics

This readme is a work in progress as we incorporate features and demos, check back later!

[![openupm](https://img.shields.io/npm/v/com.naelstrof.jigglephysics?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.naelstrof.jigglephysics/)

A jobs-based relativistic squash-and-stretch jiggle physics solution for transforms in Unity.

## Features

- Utilizes the Unity Jobs system for performance, allowing for 600 jiggling Stanford Armadillos within 2-4ms.
  
[600 Stanford Armadillo Dance](https://github.com/user-attachments/assets/2d7047a8-c386-468e-a495-4871334df38a)

- Collisions
- Relativistic, etc

## Installation

Simply add `https://github.com/naelstrof/UnityJigglePhysics.git#upm` as a package using the package manager.

Or if that doesn't work, add it to the manifest.json like so.

```
{
  "dependencies": {
    "com.naelstrof.jigglephysics": "https://github.com/naelstrof/UnityJigglePhysics.git#upm",
  }
}
```
