# Firebase Social Manager

A modular and lightweight Unity Package designed to provide social features utilizing Firebase services. This package includes logic for handling user profiles, friends list, and direct messaging (Chat) seamlessly using `UniTask` for async operations.

## Features

- **Profile Module:** Manage user profiles and meta-data.
- **Friends Module:** Send friend requests, accept/decline, and manage your friend list.
- **Chat Module:** One-on-one secure real-time messaging between users.
- **UniTask Integration:** Fully async/await API, preventing callback hells.

## Prerequisites

Before you install this package, please ensure your project has the following:

1. **Firebase SDK for Unity:** You must import the Firebase SDK (e.g. Firebase Auth, Firestore, and Realtime Database) into your `Assets` or via EDM4U.
2. C# 9.0+ and Unity 2022.3 or greater.

## Installation (Git URL)

The easiest way to install or update this package is via the Unity Package Manager using a Git URL.

1. Open your Unity Project.
2. Go to `Window` -> `Package Manager`.
3. In the top-left corner, click the `+` (plus) button.
4. Select **Add package from git URL...**
5. Enter the following URL and click **Add**:
   ```
   https://github.com/Unknown-Studio/FirebaseSocialManager.git
   ```
   _(Note: Use the URL above to install this package via Unity Package Manager)._

> **Version Control:** To lock the package to a specific version (e.g. `0.1.0`), append `#0.1.0` to the end of your git URL: `https://github.com/Unknown-Studio/FirebaseSocialManager.git#v0.1.0-preview`

## Documentation & Usage

This package relies on interface-driven Dependency Injection for its services.

### 1. Initialization

First, initialize and bind the services (for example, using Reflex, VContainer, Zenject, or manually):

```csharp
// Example Manual Architecture setup:
IChatService chatService = new ChatService();
IFriendService friendService = new FriendService();
// ProfileService profileService = new ProfileService(); ...
```

### 2. Getting Started with Friends

```csharp
// Lấy danh sách bạn bè
var friendList = await friendService.GetFriendsAsync(currentUserId);
foreach(var friend in friendList) {
    Debug.Log($"Friend: {friend.DisplayName}");
}

// Gửi lời mời kết bạn
await friendService.SendFriendRequestAsync(currentUserId, targetUserId);
```

### 3. Getting Started with Chat

```csharp
// Lắng nghe tin nhắn mới trong phòng chat
chatService.SubscribeToChatRoom(roomId, (message) => {
    Debug.Log($"New message from {message.SenderId}: {message.Content}");
});

// Gửi tin nhắn
await chatService.SendMessageAsync(roomId, currentUserId, "Hello world!");
```

## Running the Sample (Demo)

1. Open the **Package Manager**.
2. Select **In Project** -> **Firebase Social Manager**.
3. Expand the **Samples** section on the right-hand panel.
4. Click **Import** next to the `Chat & Friend Demo`.
5. The sample scripts and scenes will be copied to `Assets/Samples/Firebase Social Manager/...` where you can freely test and modify them.
