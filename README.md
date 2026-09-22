# Arcademia Leaderboards SDK (.NET)

Submit high scores from your game and let players claim them to their
Arcademia account by scanning a QR code. Your code never has to care
whether it's running on an arcade cabinet or on your own PC, it just
works either way.

This is the plain .NET build of the SDK, for anything that isn't Unity:
Godot (C# scripting), MonoGame, Stride, a custom engine, or any .NET tool.
If you're building in Unity, use the
[Unity package](https://github.com/Arcademia-Project/ac.arcademia.leaderboards-unity)
instead, it has the identical API.

Full platform docs (including the raw HTTP API for non-.NET engines):
**https://manager.arcademia.ac/docs**

## Install

```
dotnet add package Arcademia.Leaderboards
```

Targets `netstandard2.0`, so it works on .NET Framework 4.6.1+, .NET
6/8/9+, and Mono.

## Two modes, one API

| | Launcher (Live) | Sandbox |
|---|---|---|
| When | Game was started by the Arcademia launcher on an arcade machine | Anywhere else: your dev machine, a build you're testing, CI |
| Auth | Nothing you set. The launcher and the machine's credentials handle it | Your game's API key (`apiKey` in config) |
| Scores land on | The live, public leaderboard | The **Test area** only. Visible to you in the dashboard, never public |
| Claiming | Works, shows a QR popup on the cabinet | Not available (`RequestClaimAsync` returns `rejected` immediately) |

You don't choose the mode yourself. `ArcademiaLeaderboards` figures it out
automatically by checking for environment variables the launcher sets on
the game process before starting it. Write your gameplay code once and it
behaves correctly in both places.

## Quick start

```csharp
using Arcademia.Leaderboards;

async Task OnGameOver(long finalScore, string playerName)
{
    var result = await ArcademiaLeaderboards.SubmitScoreAsync("highscore", finalScore, playerName);

    if (result.Success)
        Console.WriteLine($"Saved (#{result.Rank}), mode = {result.Mode}");
    else
        Console.WriteLine($"Score not saved: {result.Message}");
}
```

That covers a minimal integration. Everything below is optional.

## Configuration

The SDK needs an **API base URL** and an **API key**. Grab both from your
game's *Leaderboards* tab in the Arcademia dashboard (request access,
then generate a key, there's more detail in the dashboard itself). The
key only matters in sandbox mode. On the machine it's ignored in favour
of the launcher/machine credentials, so **it's safe to ship inside your
build** (see *Shipping the key* below, and the full docs for the full
security explanation).

**Option A: `arcademia.json`** (recommended, since you can change it
without a rebuild)

Place an `arcademia.json` file next to your built executable (the SDK
looks in `AppContext.BaseDirectory`):

```json
{
  "apiBase": "https://manager.arcademia.ac",
  "apiKey": "arc_xxxxxxxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
}
```

This gets read automatically the first time you call anything on
`ArcademiaLeaderboards`. You can leave `apiBase` out entirely to use the
default, which is the live API. You'd only override it if you're
pointing at a private or staging deployment. In Godot, set your export
template to copy this file alongside the exported binary.

**Option B: `Configure()` in code**

```csharp
ArcademiaLeaderboards.Configure(new ArcademiaSettings
{
    apiBase = "https://manager.arcademia.ac",
    apiKey  = "arc_xxxxxxxx_...",
});
```

Call this before anything else if you'd rather set the key at runtime,
say from a settings menu, instead of shipping `arcademia.json`. Calling
`Configure` on the machine is harmless too; the key just gets ignored
there.

## API reference

### `ArcademiaLeaderboards.Mode` (returns `ArcademiaMode`)
`Launcher` or `Sandbox`. Read-only, set automatically on first use.

### `ArcademiaLeaderboards.SessionId` (returns `string`)
The current play session id when running via the launcher, otherwise
`null`. This is informational only, you never need to pass it yourself.

### `PingAsync()` (returns `Task<PingResult>`)
A sanity check: confirms connectivity and, in sandbox mode, that the API
key is valid. Worth calling once on startup.

```csharp
var ping = await ArcademiaLeaderboards.PingAsync();
if (!ping.Success) Console.WriteLine(ping.Message);
```

### `SubmitScoreAsync(boardSlug, value, playerName = null, metadataJson = null, scoreId = null)` (returns `Task<ScoreResult>`)
Submits a score to the named board.

- `boardSlug`: from the dashboard, e.g. `"highscore"` or `"time-trial"`.
- `value`: a whole number (`long`). For time-based boards, submit
  milliseconds, the dashboard formats it back for display.
- `playerName`: free text, any characters, up to 32. Server-side
  profanity filtering applies. Defaults to `"Player"` if you leave it out.
- `metadataJson`: an optional raw JSON object string (max 2 KB), e.g.
  `"{\"level\":\"3-2\",\"character\":\"fox\"}"`. It gets stored alongside
  the score and isn't shown to players, useful for support or anti-cheat
  review later.
- `scoreId`: normally you can leave this `null` and a `Guid` gets
  generated for you. Passing your own lets you safely retry a submission
  (say, after a network blip) without creating a duplicate, since the
  server deduplicates by this id.

```csharp
var result = await ArcademiaLeaderboards.SubmitScoreAsync(
    "highscore", 15230, "REX", "{\"level\":\"3-2\"}");
```

`result.Status` is one of `"submitted"`, `"queued"`, `"rejected"`, or
`"error"`. If the player's offline on the machine, the launcher queues
the score and flushes it once connectivity returns, so `result.Status`
will be `"queued"` rather than `"submitted"`. Both count as success from
your game's point of view, there's nothing extra to handle.

Hang on to `result.ScoreId` if you plan to offer a claim next.

### `RequestClaimAsync(scoreId)` (returns `Task<ClaimResult>`)
Offers a just-submitted live score for the player to save to their
Arcademia account. This shows a QR code popup on the cabinet and won't
return until the player scans it, cancels, or about five minutes pass, so
call it from a "Save my score?" prompt handler rather than your main
update loop.

```csharp
var claim = await ArcademiaLeaderboards.RequestClaimAsync(result.ScoreId);
switch (claim.Status)
{
    case "saved":     Console.WriteLine("Saved to your account!"); break;
    case "cancelled": Console.WriteLine("Cancelled.");              break;
    case "expired":   Console.WriteLine("Timed out.");              break;
    default:          Console.WriteLine("Couldn't save right now."); break;
}
```

This only really means anything in launcher mode. In sandbox mode it just
returns immediately with `Status = "rejected"`, since there's no cabinet
to show a QR code on and test scores can't be claimed anyway. Feel free
to call it unconditionally; it's a safe no-op outside the machine.

### `GetTestScoresAsync(boardSlug, limit = 25, offset = 0)` (returns `Task<TestScoresResult>`)
Reads back scores from the sandbox test area, i.e. whatever you or
another dev submitted while not on a cabinet. Only works in sandbox mode
(it returns `Success = false` on the machine, since the concept doesn't
apply there). Handy for a debug overlay while you're developing.

## Shipping the key

Your compiled build, including an `arcademia.json` next to it if you're
using one, is something a player (or a curious developer) can open up.
**That's expected, and it's safe.** The API key only grants writes to the
sandbox test area, it can never write to a live leaderboard. A live write
has to come from an arcade machine with an open play session for your
game, verified server-side, which is something a key alone can never
fake, stolen or not. The full explanation, including exactly what a
malicious actor can and can't do with a leaked key, is on the online
docs' Security model page.

## Godot notes

Godot's C# support runs on Mono/.NET, so this package works there like
any other NuGet dependency: add it via your `.csproj` (Godot generates
one for a C#-enabled project), and call it from any script the same way
as the example above. `SubmitScoreAsync`/`RequestClaimAsync` are `async
Task` methods, so `await` them from an `async` Godot callback (e.g. a
signal handler) rather than blocking the main thread.
