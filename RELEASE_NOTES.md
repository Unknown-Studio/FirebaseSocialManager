# Release Notes

## 📦 Firebase Social Manager v0.2.2-preview

### ✨ Change

- Change `UniTask` dependencies.

---

## 📦 Firebase Social Manager v0.2.1-preview

### ✨ Change

- Change `UniTask` version to `2.5.0`

---

## 📦 Firebase Social Manager v0.2.0-preview

This release introduces the highly anticipated **Guild/Team Module** and a completely refactored, flexible **Achievements Service**.

### ✨ New Features

- **Guild Module:** A complete subsystem to manage teams/guilds. Includes creation, membership limits (max 50), role management, regional suggestions, and a dedicated internal chat system (`SendMessage`, `ListenForNewMessages`).
- **Generic Achievements:** Achievements are no longer hard-coded. Developers can now pass their own Custom C# Model to `AchievementsService<T>`.
- **Performance Optimization:** Moved achievements data to a dedicated Firestore subcollection (`users/{userId}/data/achievements`) to drastically reduce payload sizes when fetching basic profiles.

### 🎮 Samples & Demos

- **Guild Demo:** Added `GuildTestUI` to fully test all guild functionalities (Create, Join, Leave, Fetch, Chat).
- **Profile Demo:** Updated to utilize the new Generic Achievements Service model with a custom `DemoAchievements`.

### ⚠️ Breaking Changes & Migration

- **Profile Model:** `UserAchievements` class has been removed. `UserProfile` no longer contains the `achievements` field.
- **Security Rules:** Update your Firestore Rules to include permissions for `match /data/{docId}` inside your users collection, and remove `guildId` from your restricted update fields.

---

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
https://github.com/Unknown-Studio/FirebaseSocialManager.git#v0.2.0-preview
```
