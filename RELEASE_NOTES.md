# Release Notes - v0.1.0-preview

## 📦 Firebase Social Manager v0.1.0-preview

This is the first pre-release version of the **Firebase Social Manager**, standardizing the module as a Unity Custom Package.

### ✨ New Features

- **Project Restructuring:** Full migration of code and assets into the official Unity Package Manager (UPM) standard structure (`Runtime`, `Editor`, `Samples~`).
- **Profile Module:** Manage user profiles and metadata on Firestore.
- **Friends Module:** Secure social network tools for sending friend requests, accepting/declining, and list management.
- **Chat Module:** High-performance, 1-on-1 real-time messaging using Firebase Cloud Firestore.
- **UniTask Integration:** All services are fully asynchronous (async/await), providing better performance and cleaner code than traditional coroutines or callbacks.

### 🎮 Samples & Demos

- **Chat & Friend Demo:** A comprehensive UI demonstration is now available via the "Samples" section in the Unity Package Manager.
- Allows for quick testing of core social features within a sample scene and modular UI scripts.

### 🛠 Improvements

- **Assembly Definition (`SocialManager.asmdef`):** Reconfigured the assembly for efficient compilation and dependency management.
- **Optimized Assets:** Cleaned metadata and folder structures to ensure a light and stable package.

### ⚠️ Pre-release Notes

- **Dependencies:** This package requires **UniTask** and **Firebase SDK (Auth, Firestore)** to be present in the project.
- As a pre-release version, APIs and folder structures are subject to change before the stable v1.0.0 release.

---

### 🚀 Installation Guide

Install via Unity Package Manager using the following Git URL:

```
https://github.com/Unknown-Studio/FirebaseSocialManager.git#v0.1.0-preview
```
