# CADSNET scripts: how the game works

## Folders

| Folder | What's inside | Study it? |
|---|---|---|
| `Core/` | The game's memory and score: `GameProgressManager` (flags), `ThreatLog` (score), `ThreatCatalog` (lesson texts), `ObjectiveManager`, `NarrationManager`, `SceneLoader`, `ScreenFader` | **Yes, first** |
| `Threats/` | The Chapter 1 decisions: `LaptopWifi` (fake café Wi-Fi), `FakeUpdateBanner`, `Phone2FA`, `CafeCounter` (receipt password) | **Yes** |
| `Story/` | Chapter 1 flow (`WakeUpSequence`, `CafeEnding`, `CafeStranger`) | **Yes** |
| `Story/Chapter2/` | Chapter 2: `Chapter2` (the order) + one file per beat + `StoryTools` (shared helpers) | **Yes** |
| `UI/` | `DebriefScreen` (results), `ConsequenceToast` (the butterfly), `UIBuild` (helpers that build UI from code), menus | Know what each does |
| `Phone/` | Opening/closing the phone, its screens, home Wi-Fi, `PhoneStory` (Chapter 2 phone screens) | Know what each does |
| `Laptop/` | Using the laptop, the thesis window, `LaptopNight` (Chapter 2 desktop, browser, ShopHub, report) | Know what each does |
| `World/` | Doors, zones, flash drive, the crosshair that lets you interact | Short, read once |
| `Decoration/` | City traffic, walkers, NPC poses, the phone map | Ignore: no cybersecurity logic |

## The one pattern behind every choice

```
player chooses  →  ThreatLog.Instance.Record("threat_id", safe)  →  ConsequenceToast.Show()
                   (and sometimes a flag in GameProgressManager, e.g. credentialsStolen = true)
```

- `Record(id, true)` = Safe, `Record(id, false)` = Compromised.
- `MarkExposed(id)` first, then `Record(id, true)` = Recovered (made a mistake, then fixed it).
- `MarkPending(id)` = the consequence happens later (shown as "🦋 pending" on the results screen until `Resolve(id)`).
- The text on the results screen for each id lives in `ThreatCatalog.cs`.

## The whole game in order

```
Chapter 1 (WakeUpSequence → house → city → café)
  flash drive + backup, 2FA (optional), café receipt, laptop Wi-Fi (real or fake), fake update, submit
  CafeEnding (walk out) → LaptopDesktop.EndChapter() → DebriefScreen → Continue
Chapter 2 (Interior scene again, chapter = 2 → WakeUpSequence adds Chapter2)
  1. StolenLoginBeat   only if credentialsStolen     phone: "Was this you?" or "password changed"
  2. KaiMessageBeat                                  phone: send / reply / call Kai
  3. LockedFilesBeat   only if installedFakeUpdate   laptop: pay / restore from backup / wipe
  4. ShopHubBeat                                     laptop: password choice, pick a seller
     ShopHubBeat.PasswordLeak  only if reusedPassword  phone, after the laptop is closed
  5. FinaleBeat                                      laptop: report (only "Report received", no result yet)
  6. BedtimeBeat                                     go to bed (BedSleep), bed camera, "Two weeks later..."
                                                     epilogue with the outcome, final results
```

## Where the Chapter 2 screens are (Hierarchy)

All Chapter 2 screens are real objects, hidden until the story shows them. Edit their text, colors and layout
in the Inspector. Every story button calls `Choose("...")` in its OnClick; the story waits for that choice.

- **Phone** (Interior and MainScene(city)): `PhoneCanvas > PhoneFrame > Screen` → `SignInAlertScreen`,
  `PasswordChangedScreen`, `ResetPasswordScreen`, `CallScreen`, `MessagesScreen` (with a hidden `BubbleTemplate`
  that is copied for each message, and Kai's `SendMoneyButton` / `ReplyButton` / `CallKaiButton`).
  References: the `PhoneStory` component on `Screen`, and the Messages fields on `PhoneHomeScreen`.
- **Laptop** (Interior): the normal desktop (wallpaper, taskbar) is reused at night. `LaptopCanvas > Desktop` has a
  hidden `BrowserIcon` next to `ThesisIcon` (shown at night; the thesis icon hides if the files were wiped with no
  backup). `LaptopCanvas > NightApps` → `Browser` (a window copied from the thesis window: title bar, address bar,
  pages `StartPage`, `SignUpPage`, `NewPasswordPage`, `ListingsPage`, `ScamChatPage`, `OrderPlacedPage`,
  `ReportPage`, `ReportSentPage`), `LockedFiles` (full screen).
- **Bed** (Interior): `BedSleep`, an invisible box over the bed with a "Sleep" prompt, hidden until bedtime. Going to
  bed reuses the wake-up scene's `CinematicCamera` (its lying-down pose).
  References: the `LaptopNight` component on `LaptopCanvas`.
- The scenes have their own copies of the phone and laptop (they are not linked to `Assets/Prefabs`), so edit the
  objects in the scene, not the prefab.

## Flags (GameProgressManager): what the game remembers

`chapter`, `hasFlashDrive`, `thesisBackedUp`, `is2FAEnabled`, `hasReceipt`, `connectedToCafeWiFi`,
`credentialsStolen`, `installedFakeUpdate`, `thesisSubmitted`, `accountHijacked`, `sentMoneyToScammer`,
`reusedPassword`, `paidScamSeller`, `sawGhostlineSeller`.

## Debugging tips

- **Jump to any situation:** in Play mode, open the `DontDestroyOnLoad` section of the Hierarchy, select
  `GameProgressManager`, and tick flags in the Inspector.
- **Two kinds of UI:** most screens are placed in the Hierarchy with OnClick set in the Inspector (phone, laptop,
  2FA app, all Chapter 2 screens). A few Chapter 1 screens are still built by code and only exist in Play mode:
  the results screen, the café receipt, the laptop Wi-Fi list and fake sign-in page, the fake update and the
  butterfly notice. Find those in their scripts.
- **The phone pauses time** (`Time.timeScale = 0`). Story waits use `StoryTools.Wait`, which uses real time.
- **The phone won't open while the laptop is in use** (the player can't move). Chapter 2 waits for the laptop
  to close before using the phone.
- **Objectives are matched by position:** `CompleteSubTask(5, 0)` means the 6th objective in the list in
  `ObjectiveManager`. Adding an objective in the middle shifts the numbers after it.
- Console lines starting with `[ThreatLog]` and `[GameProgress]` show every recorded choice and flag change.
