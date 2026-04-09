# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.1-preview] - 2026-04-09

### Changed

- Change name space to `Suhdo.FSM`.

## [0.3.0-preview] - 2026-04-09

### Changed

- Removed `UniTask` dependency. All asynchronous operations now use standard `System.Threading.Tasks.Task` instead.

## [0.2.2-preview] - 2026-04-08

### Changed

- Change **Unitask** dependencies.

## [0.2.1-preview] - 2026-04-08

### Changed

- Change **Unitask** version to 2.5.0

## [0.2.0-preview] - 2026-04-08

### Added

- **Guild Module:** Complete implementation of Guilds/Teams including creation, searching, joining, and internal messaging.
- **Generic Achievements:** Extracted achievements logic into a standalone plug-and-play `AchievementsService<T>` utilizing Firestore subcollections to optimize reads.

### Changed

- Profile Service no longer handles achievements, reducing document size and improving load times.
- Updated Profile Demo to demonstrate the new generic Achievements Service.
- Updated security rules requirements for the `users/{userId}/data` subcollection.

## [0.1.0-preview] - 2026-04-03

### Added

- **Standard Unity Package Structure:** Reorganized codebase into `Runtime`, `Editor`, and `Tests` folders.
- **Profile Module:** Initial implementation of user profile management with Firestore.
- **Friends Module:** Handling of friend requests and social relationships.
- **Chat Module:** Secure 1-on-1 real-time messaging between users.
- **UniTask Support:** Full implementation of async/await patterns for improved performance and readability.
- **Samples:** Included a "Chat & Friend Demo" UI sample accessible via Unity Package Manager.
- **Documentation:** Added README.md and installation guides.
- **Project Setup:** Included .gitignore and .meta files for package-only distribution.
