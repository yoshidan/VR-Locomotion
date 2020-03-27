# VR-Locomotion
VR Locomotion implementation like a VRChat.

<img src="./image/movie.gif" width="320px">

<img src="./image/ikmovie.gif" width="320px">

## Requirement
* Oculus Quest
* Unity 2019.2.20f1
* [FinalIK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290)

## Local

* git clone this project
* build and run for Oculus Quest(Android)

## Description
* Character walks into the point specified by `OVRInput.Button.PrimaryThumbstickUp`.
* Relase `OVRInput.Button.PrimaryThumbstickUp` then camera warps to character's head position.

## Without FinalIK

Use `PlayerController.cs` instead of `IKPlayerController.cs` and delete `IKPlayerController.cs` if you don't have FinalIK

![UCL](./image/imageLicenseLogo.png)  
[ユニティちゃんライセンス条項](http://unity-chan.com/contents/license_jp/)

