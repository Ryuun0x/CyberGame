# CADSNET scripts: how the game works

## Folders

| Folder | What's inside | Study it? |
|---|---|---|
| `Core/` | The game's memory and score: `GameProgressManager` (flags), `ThreatLog` (score), `ThreatCatalog` (lesson texts), `ObjectiveManager`, `NarrationManager`, `SceneLoader`, `ScreenFader` | **Yes, first** |
| `Threats/` | The Chapter 1 decisions: `LaptopWifi` (fake café Wi-Fi), `FakeUpdateBanner`, `Phone2FA`, `CafeCounter` (receipt password) | **Yes** |
| `Story/` | Chapter 1 flow (`WakeUpSequence`, `CafeEnding`, `CafeStranger`) | **Yes** |
| `Story/Chapter2/` | Chapter 2: `Chapter2` (the order) + one file per beat + `StoryTools` (shared helpers) | **Yes** |
| `UI/` | `DebriefScreen` (results), `ConsequenceToast` (the butterfly), `UIBuild` (small UI helpers, used by the city map), menus | Know what each does |
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
Prologue (Story/Prologue.cs, on the WakeUpSequence object; first time only)
  11:47 PM cinematic: Kai's texts (subtitles in the letterbox bar), the living-room TV news naming Ghostline,
  she lies down. Esc/P pause it, other keys skip. Lines are at the top of Prologue.cs.
  Morning (Prologue.PlayWakeUp): 9:04 AM card, she wakes up and sits up in bed (third person), then first person.
Chapter 1 (WakeUpSequence → house → city → café)
  flash drive + backup, 2FA (optional), café receipt, laptop Wi-Fi (real or fake), fake update, submit
  CafeEnding (walk out) → LaptopDesktop.EndChapter() → DebriefScreen → Continue
Chapter 2 (Interior scene again, chapter = 2 → WakeUpSequence starts its Chapter2 component)
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
- The scenes have their own copies of the phone (not linked to `Assets/Prefabs`), so edit the phone in the scene
  and make the same change in **both** scenes (e.g. phone size). `LaptopCanvas` is a linked prefab copy, but each
  scene adds its own parts on top (bold in the Inspector): the night apps in Interior, the café Wi-Fi in the city.

## Submitting the thesis (both scenes)

`Laptop/ThesisSubmit.cs` on `LaptopCanvas` (linked prefab). The thesis window's **Send to adviser** button opens
`LaptopCanvas > BrowserWindow` (Goggle Mail): Loading → **offline page** ("No internet", ERR_INTERNET_DISCONNECTED)
when `LaptopDesktop.HasInternet` is false (always at home; at the café until the laptop joins a network), or the
**mail compose page** (Thesis_Final.pdf attached) when online. Send → Sending… → Message sent → `LaptopDesktop.SubmitThesis()`.
At the café the mail page loading is what pushes the fake browser update (`FakeUpdatePopup` sits in the browser).

## Where the Chapter 1 café objects are (MainScene(city))

Also real objects, hidden until used:
- `UI > CafeReceipt` → `Receipt` (the paper). `CafeCounter.receipt` points to it.
- `UI > LaptopCanvas` → `WifiList` (network rows, each OnClick calls `Join("name")`; `PasswordRow`) and
  `CaptivePortal` (the fake sign-in: `Form`, `Connected`). References: the `LaptopWifi` component on `LaptopCanvas`.
- `UI > LaptopCanvas > ThesisWIndow > FakeUpdatePopup` → `Question` (X / Download) and `Installing`.
  References: the `FakeUpdateBanner` component on `LaptopCanvas`.
- `Story & Interactables > CafeExitWall`: the invisible doorway wall, switched by `CafeEnding` (on `CafeZone`).
- `Ambient Life > Café Customers > StrangerLaptop` → `ScreenHinge > GhostlineScreen` (the stranger's logo).
- Both game scenes: `UI > DebriefScreen` (results; cards are copies of its hidden `CardTemplate`,
  `PendingTemplate`, `HeadingTemplate`) and `UI > ConsequenceToast` (the butterfly). Both are linked prefabs.

## Hierarchy categories

Every scene is grouped the same way: `Environment` (lights, rooms, city), `Player & Cameras`,
`Story & Interactables` (things you press E on and story objects), `Ambient Life` (walkers, cars, café
customers), `UI` (every canvas and the EventSystem). StartMenu uses `Environment`, `Cameras`, `Menu`, `UI`.
`GameManager` (GameProgressManager + ThreatLog) stays at the top level: it survives scene loads
(`DontDestroyOnLoad`), which only works on top-level objects.

## Pause menu

`UI/PauseMenu.cs` on the `PauseMenu` prefab (`Assets/Prefabs/PauseMenu.prefab`), placed in both Interior and
MainScene(city). Unlike the phone and laptop, these are **linked prefab copies**: edit the prefab and both scenes
change. Esc (or P) pauses only while walking around; with the phone or laptop open, Esc closes those instead.
Chapter 2's story waits (`StoryTools.Wait`) stand still while paused.
- Layout: `PauseScreen` (50% black overlay) → `MenuContainer`, a copy of the Start Menu's left-side menu
  (Resume / Main Menu / Quit Game, OnClick set in the Inspector).
- Blur: the `PauseBlur` child is a Volume using `Assets/Settings/PauseBlur.asset` (Depth of Field, Gaussian, plus
  Color Adjustments to darken). A Volume only works when the camera's post-processing is on, so `PauseMenu`
  turns it on while paused and back off on resume. Change the blur strength in that asset.
- While paused the player and camera can't move, and the crosshair and "Press E" prompt are hidden.

## Settings

`Assets/Prefabs/Settings.prefab`, placed in the Start Menu and inside the PauseMenu prefab (edit the prefab, both
change). It replaces the menu column while open; Back (or Esc in the pause menu) returns.
- `UI/SettingsMenu.cs`: the screen. Tabs Graphics / Audio / Controls are placed pages; every slider, checkbox and
  `<` `>` button calls one method from its OnClick / On Value Changed.
- `Core/GameSettings.cs`: the values, saved with PlayerPrefs (keys start with `Settings.`) when the screen closes,
  and loaded when the game starts. Quality Low/High = the project's two quality levels (Mobile / PC renderer).
  Music and Sound Effects volumes are stored for the sound effects to come.
- Resolution steps through 720p / 900p / 1080p / 1440p / 4K, only up to what the monitor shows; the first time,
  it picks the biggest one that fits. Mouse sensitivity is read in `FirstPersonController.CameraRotation`.
- Background: the pause menu already darkens and blurs the game. In the Start Menu, Settings shows its own
  `SettingsOverlay` (50% black) and `Cameras > SettingsBlur` (the same `PauseBlur` blur) while open.

## Flags (GameProgressManager): what the game remembers

`chapter`, `hasFlashDrive`, `laptopAt` ("home" / "hands" / "cafe", LaptopCarry), `thesisBackedUp`, `is2FAEnabled`, `hasReceipt`, `connectedToCafeWiFi`,
`credentialsStolen`, `installedFakeUpdate`, `thesisSubmitted`, `accountHijacked`, `sentMoneyToScammer`,
`reusedPassword`, `paidScamSeller`, `sawGhostlineSeller`.

## Debugging tips

- **Jump to any situation:** in Play mode, open the `DontDestroyOnLoad` section of the Hierarchy, select
  `GameProgressManager`, and tick flags in the Inspector.
- **Everything is in the Hierarchy before Play:** screens are placed objects with OnClick set in the Inspector;
  scripts only show/hide them and fill in text. Only these are made while playing: copies of templates (result
  cards, chat bubbles), the walkers and cars from `City Life`, and the minimap/map drawn by `CityMapController`.
- **The phone pauses time** (`Time.timeScale = 0`). Story waits use `StoryTools.Wait`, which uses real time.
- **The phone won't open while the laptop is in use** (the player can't move). Chapter 2 waits for the laptop
  to close before using the phone.
- **Objectives are matched by position:** `CompleteSubTask(5, 0)` means the 6th objective in the list in
  `ObjectiveManager`. Adding an objective in the middle shifts the numbers after it.
- Console lines starting with `[ThreatLog]` and `[GameProgress]` show every recorded choice and flag change.
