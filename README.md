# 🌍 WebGL 3D Asset Painter & World Builder

A runtime 3D level editing and scattering tool built with Unity. This application allows users to procedurally paint, scale, and rotate 3D assets across a terrain directly in their web browser. It features a fully dynamic UI, smart placement logic, and the ability to import custom 3D models at runtime.

## ✨ Key Features

* **Dynamic Asset Scattering:** Paint multiple objects at once with customizable density (Fill %), scale ranges, and random rotation limits.
* **Smart Spacing & Perfect Packing:** Built-in collision detection prevents objects of the same type from overlapping. Setting Fill to 100% switches from random scattering to mathematical grid-packing for dense forests or precise placement.
* **Filtered "Flow" Eraser:** The eraser acts as a mask, only deleting the specific type of asset you currently have selected without destroying the surrounding environment. Lowering the Fill % allows you to gently "thin out" a dense area instead of deleting it completely.
* **Runtime `.GLB` Import:** Users can upload their own 3D `.glb` models directly from their computer while the game is running in the browser. 
* **Auto-Generating UI & Photo Booth:** When a new asset is imported, the system spawns a temporary clone, frames a hidden camera around it, takes a 2D snapshot, and automatically generates a hotbar inventory button for it.
* **Terrain-Aware Fly Camera:** A smooth spectator camera that reads terrain height data to gracefully glide over mountains and prevents the user from clipping through the ground.

## 🎮 Controls

* **Left-Click:** Use Active Brush (Paint / Erase)
* **Right-Click (Hold):** Look Around
* **W, A, S, D:** Move Camera Forward/Back/Left/Right
* **Q / E:** Move Camera Down/Up
* **Shift:** Sprint
* **TAB:** Toggle Paint/Erase Mode
* **[ / ]:** Decrease / Increase Brush Size
* **1, 2, 3...:** Quick-select inventory slots

## 🚀 Play the Demo

[Play the WebGL version on Itch.io here!](https://bipul6129.itch.io/worldbrush-webgl-runtime-asset-painter)

## 🛠️ Developer Setup & Installation

If you want to download and modify this project:

1. Clone this repository to your local machine.
2. Ensure you have **Unity 6** (or your specific version) installed.
3. Open the project via Unity Hub.
4. **Dependencies:** This project relies on the `glTFast` package for runtime `.glb` importing. Ensure it is installed via the Unity Package Manager.

### WebGL Build Instructions

To build this project for the web without browser errors:
1. Go to `Edit -> Project Settings -> Player -> WebGL Tab`.
2. Under **Publishing Settings**, ensure **Compression Format** is set to `Disabled`.
3. Build and run!

## 📜 Technical Notes
* This project utilizes Unity's **Universal Render Pipeline (URP)**.
* Camera culling distances are implemented to optimize performance when rendering thousands of painted meshes in a browser environment.