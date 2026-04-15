# Firebase Social Manager

A modular and lightweight Unity Package designed to provide social features utilizing Firebase services. This package includes logic for handling user profiles, friends list, and direct messaging (Chat) seamlessly using standard `Task` for async operations.

## Features

- **Profile Module:** Manage user profiles and meta-data.
- **Friends Module:** Send friend requests, accept/decline, and manage your friend list.
- **Chat Module:** One-on-one secure real-time messaging between users.
- **Task Integration:** Fully async/await C# API, preventing callback hells.

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
// Fetch friend list
var friendList = await friendService.GetFriendsAsync(currentUserId);
foreach(var friend in friendList) {
    Debug.Log($"Friend: {friend.DisplayName}");
}

// Send friend request
await friendService.SendFriendRequestAsync(currentUserId, targetUserId);
```

### 3. Getting Started with Chat

```csharp
// Subscribe to new messages in a chat room
chatService.SubscribeToChatRoom(roomId, (message) => {
    Debug.Log($"New message from {message.SenderId}: {message.Content}");
});

// Send a message
await chatService.SendMessageAsync(roomId, currentUserId, "Hello world!");
```

## Data Schema & Firebase Structure

The package uses a specific Firestore structure to ensure performance, security, and scalability.

### 1. User Profiles
- **Path:** `users/{userId}`
- **C# Model:** `UserProfile`

| Field | Type | Description |
| :--- | :--- | :--- |
| `displayName` | `string` | User's visible name. |
| `friendCode` | `string` | Unique 6-character code (e.g., `AB1234`) for searching. |
| `avatarId` | `string` | ID of the selected avatar icon. |
| `frameId` | `string` | ID of the selected avatar frame. |
| `level` | `number` | User's current level. |
| `achievements` | `map` | Nested object: `totalScore`, `gamesPlayed`, `wins`. |
| `lastLogin` | `timestamp` | Server timestamp of the last session. |

### 2. Social & Friends
- **Path:** `users/{userId}/friends/{friendId}`
- **C# Model:** `FriendRecord`

| Field | Type | Description |
| :--- | :--- | :--- |
| `status` | `string` | `pending_sent`, `pending_received`, or `accepted`. |
| `friendName` | `string` | Cached display name of the friend (denormalized). |
| `avatarId` | `string` | Cached avatar ID. |
| `updatedAt` | `timestamp` | Last time the relationship status changed. |

> [!NOTE]
> Relationship data is stored bi-directionally using **Write Batches** to ensure data consistency between both users.

### 3. Private Messaging (Chat)
- **Room Path:** `private_chats/{roomId}`
- **Room Model:** `PrivateChatRoom`
- **Messages Path:** `private_chats/{roomId}/messages/{messageId}`
- **Message Model:** `ChatMessage`

**Room Document:**
- `participants`: Array of exactly 2 User IDs.
- `lastMessage`: The content of the latest message sent.
- `unreadCount`: A Map `{ [userId]: number }` tracking pending messages per user.

**Message Document:**
- `senderId`: UID of the sender.
- `text`: Content of the message.
- `timestamp`: Server timestamp of delivery.

## Running the Sample (Demo)

1. Open the **Package Manager**.
2. Select **In Project** -> **Firebase Social Manager**.
3. Expand the **Samples** section on the right-hand panel.
4. Choose the sample you want to test and click **Import**:
    - `Chat & Friend Demo`: Test real-time chat and friend requests.
    - `Profile Service Demo`: Test profile creation, updates, and searching.
5. The sample scripts and scenes will be copied to `Assets/Samples/Firebase Social Manager/...` where you can freely test and modify them.
