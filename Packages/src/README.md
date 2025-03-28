# <img alt="logo" height="26" src="https://github.com/mob-sakai/mob-sakai/assets/12690315/474143fa-ed3e-49bc-a0bb-d01048c5c493"/> Composite Canvas Renderer

[![](https://img.shields.io/npm/v/com.coffee.composite-canvas-renderer?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.coffee.composite-canvas-renderer/)
[![](https://img.shields.io/github/v/release/mob-sakai/CompositeCanvasRenderer?include_prereleases)](https://github.com/mob-sakai/CompositeCanvasRenderer/releases)
[![](https://img.shields.io/github/release-date/mob-sakai/CompositeCanvasRenderer.svg)](https://github.com/mob-sakai/CompositeCanvasRenderer/releases)  
![](https://img.shields.io/badge/Unity-2019.4+-57b9d3.svg?style=flat&logo=unity)
[![](https://img.shields.io/github/license/mob-sakai/CompositeCanvasRenderer.svg)](https://github.com/mob-sakai/CompositeCanvasRenderer/blob/main/LICENSE.txt)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)
[![](https://img.shields.io/github/watchers/mob-sakai/CompositeCanvasRenderer.svg?style=social&label=Watch)](https://github.com/mob-sakai/CompositeCanvasRenderer/subscription)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)

<< [📘 Documentation](#-documentation) | [🎮 Demo](#-demo) | [⚙ Installation](#-installation) | [🚀 Usage](#-usage) | [🤝 Contributing](#-contributing) >>

<br><br>

## 📝 Description

CompositeCanvasRenderer bakes multiple source graphics into a bake-buffer (RenderTexture) and renders it.

It also supports additional material modification, mesh modification, and baking effects, allowing you to enjoy effects that were challenging to implement with standard UI shaders, such as blur, soft outline, and soft shadow.

**Key Features:**

- Bakes multiple source graphics into a `RenderTexture` as **bake-buffer**.
- Utilizes the materials set on the source graphics during the baking process.
- The bake-buffer is automatically baked as needed, or you can trigger manual baking with `SetDirty()`.
- Apply custom effects to the bake-buffer using a `CommandBuffer` after baking (as post-bake effect).
- You can use custom materials for rendering the bake-buffer.

Let's enjoy a wide range of effects that were previously challenging to achieve with standard UI shaders!

<img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/28a3cab1-d939-4161-8ef5-d24c760c5d49" width="600/">

#### Features

* **Efficiency**: By combining multiple graphics into a bake-buffer, it reduces the number of draw calls and can significantly improve rendering performance.

* **Automated Baking**: The bake-buffer is automatically generated as needed, simplifying the rendering process and reducing manual intervention.

* **Control**: Users have the flexibility to manually trigger the baking process using SetDirty(), giving them control over when and how the graphics are baked.

* **Material Usage**: It ensures that the materials set on the graphics are used during the baking process, maintaining visual consistency.

* **Post-Bake Effects**: After baking, you can apply various effects to the bake buffer using a command buffer, allowing for additional visual enhancements or post-processing.

* **Built-in Effects**: Several effects are available out of the box!  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/43cde946-7feb-42bc-9e58-1da69477b103" width=650/>

* **Custom Material Support**: You can use custom materials for rendering the bake-buffer.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/cdd0f457-e808-4b1a-bc5b-76464cb5ee4a" width=500/>

* **Foreground/Background Rendering**: Supports both foreground and background rendering.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/99da8004-c35a-4b5a-9f11-297810a7d6d2" width=300/>

* **Color and Blend Mode**: Allows you to change color modes and blend modes.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/25f6bdaf-672f-450c-a74c-c46a3ca01faf" width=450/>

* **Quality and Performance Control**: You can fine-tune quality and performance using the `Down Sampling Rate` parameter.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/963286de-a075-4397-9b8f-669f5ad4cbd0" width=450/>

* **Perspective/Orthographic Rendering**: Supports both perspective and orthographic rendering. In orthographic rendering (where relative `position.z` and relative `rotation.xy` are 0), baking is less frequent.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/a0148c6f-7869-4071-a86d-c4bc4c8806b1" width=400/>

* **TextMeshPro Compatibility**: Works seamlessly with TextMeshPro. `<font>` and `<sprite>` tags are supported, and it may also be compatible with other custom UI classes.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/ed6a78bb-e339-404e-a2ce-e686bbceceaf" width=500/>

* **Editor Support**: Enjoy a convenient editing experience with the ability to preview the bake buffer in the inspector and visualize the baking region in the scene view. You can also customize the behavior from the project settings.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/8369b4e2-4463-4469-af1c-e7ac81959d03" width=300/>
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/c614e39d-f3f4-4178-a870-6198a71fbf01" width=250/>

<br><br>

## 📄 Documentation

Check out the detailed documentation to learn more about the project and its features.

[Documentation](http://mob-sakai.github.io/CompositeCanvasRenderer/)

<br><br>

## 🎮 Demo

[WebGL Demo](http://mob-sakai.github.io/CompositeCanvasRenderer/Demo/)

![]()

<br><br>

## ⚙ Installation

_This package requires **Unity 2019.4 or later**._

#### Install via OpenUPM

This package is available on [OpenUPM](https://openupm.com) package registry.
This is the preferred method of installation, as you can easily receive updates as they're released.

If you have [openupm-cli](https://github.com/openupm/openupm-cli) installed, then run the following command in your project's directory:

```
openupm add com.coffee.composite-canvas-renderer
```

#### Install via UPM (using Git URL)

Navigate to your project's Packages folder and open the `manifest.json` file. Then add this package somewhere in the `dependencies` block:

```json
{
  "dependencies": {
    "com.coffee.composite-canvas-renderer": "https://github.com/mob-sakai/CompositeCanvasRenderer.git?path=Packages/src",
    ...
  },
}
```

To update the package, change suffix `#{version}` to the target version.

* e.g. `"com.coffee.composite-canvas-renderer": "https://github.com/mob-sakai/CompositeCanvasRenderer.git?path=Packages/src#1.0.0",`

<br><br>

## 🚀 Usage

1. Create a GameObject with the `CompositeCanvasRenderer` component.

2. Add UI elements such as Image, RawImage, Text, TextMeshProUGUI, etc., to the GameObject.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/571dd4ed-2ab7-49fd-be88-d176728fdcb8" width=300/>

3. The baking area is determined by the `RectTransform.size` and the `Expands` option. In the scene view, the baking area is displayed as a magenta rectangle.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/c614e39d-f3f4-4178-a870-6198a71fbf01" width=300/>

4. Adjust the `CompositeCanvasRenderer` settings in the inspector.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/8369b4e2-4463-4469-af1c-e7ac81959d03" width=400/>

5. Select an effect in the inspector and fine-tune its settings.  
  <img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/569d59ac-8bf5-421f-baaa-8487967f15a1" width=400/>

6. Enjoy!

<br><br>

## 🤝 Contributing

### Issues

Issues are incredibly valuable to this project:

- Ideas provide a valuable source of contributions that others can make.
- Problems help identify areas where this project needs improvement.
- Questions indicate where contributors can enhance the user experience.

### Pull Requests

Pull requests offer a fantastic way to contribute your ideas to this repository.  
Please refer to [CONTRIBUTING.md](https://github.com/mob-sakai/CompositeCanvasRenderer/tree/main/CONTRIBUTING.md)
and [develop branch](https://github.com/mob-sakai/CompositeCanvasRenderer/tree/develop) for guidelines.

### Support

This is an open-source project developed during my spare time.  
If you appreciate it, consider supporting me.  
Your support allows me to dedicate more time to development. 😊

[![](https://user-images.githubusercontent.com/12690315/50731629-3b18b480-11ad-11e9-8fad-4b13f27969c1.png)](https://www.patreon.com/join/2343451?)  
[![](https://user-images.githubusercontent.com/12690315/66942881-03686280-f085-11e9-9586-fc0b6011029f.png)](https://github.com/users/mob-sakai/sponsorship)

<br><br>

## License

* MIT

## Author

* ![](https://user-images.githubusercontent.com/12690315/96986908-434a0b80-155d-11eb-8275-85138ab90afa.png) [mob-sakai](https://github.com/mob-sakai) [![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai) ![GitHub followers](https://img.shields.io/github/followers/mob-sakai?style=social)

## See Also

* GitHub page : https://github.com/mob-sakai/CompositeCanvasRenderer
* Releases : https://github.com/mob-sakai/CompositeCanvasRenderer/releases
* Issue tracker : https://github.com/mob-sakai/CompositeCanvasRenderer/issues
* Change log : https://github.com/mob-sakai/CompositeCanvasRenderer/blob/main/CHANGELOG.md
