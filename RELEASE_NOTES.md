# Release Notes

## 📦 Firebase Social Manager v0.4.2-preview

A patch release to fix the "Local Echo" timestamp issue in Chat and Guild services.

### 🐛 Bug Fixes

- **Local Echo Timestamp Fix:** Resolved an issue where newly sent messages would have a `null` timestamp in the local listener.
    - Updated `ChatService` and `GuildService` to use `ServerTimestampBehavior.Estimate`.
    - This ensures messages appear with a valid (estimated) time immediately on the UI without waiting for server confirmation.

---

## 📦 Firebase Social Manager v0.4.1-preview

A patch release to align the new Presence module with the established `Suhdo.FSM` namespace convention and fix an incorrect Firebase API call.

### 🛠 Changes

- **Namespace Alignment:** Migrated all Presence module namespaces from `SocialManager.Presence` → `Suhdo.FSM.Presence` to match the unified package convention established in v0.3.1.
- **Model Namespace:** Renamed `SocialManager.Presence.Models` → `Suhdo.FSM.Presence.Model` (singular) for consistency with other modules.
- **Sample Imports:** Updated `FirebaseInit.cs` to use `Suhdo.FSM.*` namespaces across Chat, Friends, and Profile services.

### 🐛 Bug Fixes

- **Firebase API Fix:** Corrected `UpdateChildren` → `UpdateChildrenAsync` in `PresenceService` to match the Firebase Unity SDK method signature.

### ⚠️ Breaking Changes

- If you referenced the old `SocialManager.Presence` namespace, update your `using` directives to `Suhdo.FSM.Presence`.

---

## 📦 Firebase Social Manager v0.4.0-preview

This release introduces the **Presence System** for real-time online/offline status tracking and a **Generic Save Game Module** for flexible cloud saves.

### ✨ New Features

- **Presence System:** Comprehensive online/offline status tracking powered by Firebase Realtime Database.
    - Automatic offline detection via RTDB `onDisconnect` — ensures accurate status even on app crashes or network loss.
    - Batch status fetching (`GetStatusesAsync`) to efficiently query all friends' statuses in a single call.
    - Manual `SetOfflineAsync` for clean logout flows.
- **Generic Save Game Module:** A fully customizable cloud save system using Firestore.
    - Clients define their own `[FirestoreData]` models (e.g., `MyGameSave`) without modifying the package.
    - Single-snapshot overwrite strategy at `users/{userId}/save_data/current`.
- **Friend List Presence Indicators:** The Friend Demo UI now displays 🟢 (Online) / ⚪ (Offline) badges next to each friend.

### 🐛 Bug Fixes

- **Chat Permission Bug:** Resolved `Missing or insufficient permissions` error when creating new chat rooms via `WriteBatch` by implementing `getAfter` in Firestore Security Rules.
- **FriendCode Auto-Patch:** Fixed an issue where legacy accounts without a `FriendCode` were not properly patched during profile fetch.

### 🎮 Samples & Demos

- **Chat & Friend Demo:** Updated `FirebaseInit` to automatically initialize `PresenceService` and call `SetOnlineAsync` on login.
- **Save Game Demo:** New `SaveGameTestUI` with `SaveGameTestUIGenerator` for quickly testing cloud save/load operations.

### ⚠️ Setup Required

- **Realtime Database Rules:** You must add the following rules to your Firebase RTDB:
  ```json
  {
    "rules": {
      "presence": {
        "$uid": {
          ".read": "auth != null",
          ".write": "auth != null && auth.uid == $uid"
        }
      }
    }
  }
  ```
- **Firestore Rules:** Ensure `save_data` subcollection rules are configured for `users/{userId}/save_data/{docId}`.

---

## 📦 Firebase Social Manager v0.3.1-preview

### ✨ Change

- Change name space to `Suhdo.FSM`.

---

## 📦 Firebase Social Manager v0.3.0-preview

### ✨ Change

- Removed `UniTask` dependency. All asynchronous operations now use standard `System.Threading.Tasks.Task` instead.

---

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
https://github.com/Unknown-Studio/FirebaseSocialManager.git#v0.4.2-preview
```
