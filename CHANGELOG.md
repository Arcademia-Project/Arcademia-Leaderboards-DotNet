# Changelog

All notable changes to the Arcademia Leaderboards SDK for .NET are
documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.0.0] - 2026-09-22

### Added
- `ArcademiaLeaderboards` static facade: `PingAsync`, `SubmitScoreAsync`, `GetTestScoresAsync`, `RequestClaimAsync`.
- Automatic launcher detection via environment variables (`ARCADEMIA_PIPE`, `ARCADEMIA_NONCE`, `ARCADEMIA_SESSION_ID`) with a transparent fallback to sandbox (direct HTTPS + API key) mode when they are absent.
- `arcademia.json` config file support (read from next to the built executable), for setting a default API key/base URL without touching code.
- Targets `netstandard2.0`. Same API surface as the Unity package, ported from `UnityEngine.JsonUtility` to `System.Text.Json`.
