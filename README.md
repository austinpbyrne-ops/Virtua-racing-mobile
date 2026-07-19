# Virtua Racing Mobile — "The XArcade Edition"

A faithful mobile clone of Sega AM2's 1992 arcade classic **Virtua Racing**, built in Unity with URP for iOS and Android.

> **Status:** Full source code complete. Ready to open in Unity and build.

---

## Quick Start

### Prerequisites
- **Unity 2022.3 LTS** or newer (with iOS + Android build support)
- Universal Render Pipeline package
- Input System package
- TextMeshPro

### Setup (5 minutes)

1. **Open Unity Hub** → Add Project → Select this folder
2. Wait for Unity to import (3-5 minutes for URP + Input System packages)
3. **Configure URP:** `Tools > Virtua Racing > Setup All` (this sets up the flat-shaded pipeline)
4. **Open MenuScene** (create empty scene with MenuManager + Canvas)
5. **Open RaceScene** (create empty race scene — see Scene Setup below)
6. Hit Play!

### Scene Setup

#### MenuScene
```
GameObject: MenuManager (attach MenuManager.cs)
  └─ Canvas (Screen Space - Overlay)
       ├─ TitlePanel (TextMeshPro: "VIRTUA RACING" / "THE XARCADE EDITION" / "TAP TO START")
       ├─ MainMenuPanel (Buttons: RACE, GRAND PRIX, TIME TRIAL, OPTIONS, HIGH SCORES)
       ├─ TransmissionPanel (Buttons: AUTOMATIC, MANUAL)
       ├─ TrackSelectPanel (Buttons: BIG FOREST, BAY BRIDGE, ACROPOLIS)
       ├─ OptionsPanel (Sliders: Tilt Sensitivity, Music Volume, SFX Volume)
       └─ HighScoresPanel (3x TextMeshPro entries)
```

#### RaceScene
```
GameObject: GameManager (attach GameManager.cs)
GameObject: AudioManager (attach AudioManager.cs)
  └─ 4x AudioSource children

Directional Light (sun, rotation ~(50, -30, 0), color warm white)

Main Camera (attach VRCameraSystem.cs)

SkyDome: Large inverted sphere with SkyGradient material
  └─ MeshRenderer using "VirtuaRacing/SkyGradient"

---- TRACK ----
GameObject: Track (BigForestTrack.cs + TrackBuilder.cs)
  ├─ Road (MeshFilter + MeshRenderer, flat-shaded material)
  ├─ Curb_Left (MeshFilter + MeshRenderer)
  ├─ Curb_Right (MeshFilter + MeshRenderer)
  ├─ Grass_Left (MeshFilter + MeshRenderer)
  ├─ Grass_Right (MeshFilter + MeshRenderer)
  ├─ RoadCollider (MeshCollider)
  ├─ Checkpoints (child objects with CheckpointTrigger, BoxCollider isTrigger)
  └─ Scenery (child objects: FerrisWheel, Trees, Buildings, etc.)

---- PLAYER CAR ----
GameObject: PlayerCar (tag: "Player")
  ├─ CarController.cs
  ├─ F1CarMeshGenerator.cs
  ├─ CarDamageVisuals.cs
  ├─ Rigidbody (mass: 800, drag: 0.5, angular drag: 2)
  ├─ 4x WheelCollider children (FL, FR, RL, RR)
  │   └─ Wheel Mesh children
  └─ BoxCollider (for checkpoint triggers)

---- AI CARS (15x) ----
GameObject: AICar_01-15 (tag: "Car")
  ├─ AIOpponent.cs
  ├─ CarController.cs
  ├─ F1CarMeshGenerator.cs
  └─ Rigidbody + WheelColliders

---- UI ----
Canvas (Screen Space - Overlay)
  └─ HUDRacing (attach HUDRacing.cs)
       ├─ TopBar
       │    ├─ PositionText ("7TH/16")
       │    ├─ TimerText ("65")
       │    └─ LapTimeText ("LAP 1 1'00"00")
       ├─ BottomBar
       │    ├─ SpeedText ("150mph")
       │    ├─ GearText ("3")
       │    └─ DifficultyText ("BEGINNER")
       ├─ CheckpointFlash ("CHECKPOINT!")
       ├─ CountdownPanel (3 → 2 → 1 → GO!)
       ├─ PausePanel
       └─ ResultsPanel
            ├─ PositionText
            ├─ BestLapText
            ├─ TotalTimeText
            └─ ContinueText ("CONTINUE? 10")

GameObject: InputManager (attach InputManager.cs + PlayerInput)
  ├─ AccelerateZone (RectTransform, right-bottom)
  ├─ BrakeZone (RectTransform, right-top)
  └─ SteerZone (RectTransform, left side, optional)

GameObject: GhostCarSystem (attach GhostCarSystem.cs) — Time Trial only
GameObject: AttractReplay (attach AttractReplay.cs) — Title screen
```

---

## Architecture Overview

### Shader: Flat-Shaded Polygons (THE defining element)
- **`Assets/Shaders/FlatShaded.shader`** — Custom geometry shader that computes per-face normals
- Every triangle gets ONE solid shade (no smooth interpolation)
- Banded lighting: 4 discrete shade levels for retro "toon-lighting" feel
- Optional dithering on shadow faces (Model 1 authentic)
- No textures, no PBR, no normal maps

### Shader: Sky Gradient
- **`Assets/Shaders/SkyGradient.shader`** — Simple top-to-bottom color gradient
- Big Forest: blue → cyan, Acropolis: dark red-orange → orange (sunset)

### Car Controller (`Assets/Scripts/Car/CarController.cs`)
- RWD arcade physics using Unity WheelColliders
- 5-speed manual or automatic transmission
- Speed-sensitive steering (less angle at high speed for stability)
- Downforce simulation (speed² scaling)
- Collision damage: reduces top speed up to 40%
- Engine RPM → audio pitch modulation

### Track Builder (`Assets/Scripts/Track/TrackBuilder.cs`)
- Spline-based track generation from segment definitions
- Auto-generates: road mesh, curbs, grass strips, ground planes
- Generates waypoints for AI (simplified centerline)
- Checkpoint trigger system
- Vertex-colored road markings (start/finish checkerboard, dashed center line)

### Track Definitions
- **`BigForestTrack.cs`** — Beginner circuit with Ferris wheel, roller coaster, trees, pit building, grandstands, flags
- **`BayBridgeTrack.cs`** — Intermediate: suspension bridge, water, tunnel, palm trees, mountains
- **`AcropolisTrack.cs`** — Expert: urban canyon, skyscrapers, striped wall, multiple tunnels, sunset

### AI Opponent (`Assets/Scripts/AI/AIOpponent.cs`)
- Waypoint-following with look-ahead for corner anticipation
- Rubber-banding: speeds up when behind, eases off when ahead (subtle)
- Obstacle avoidance (raycast-based, forward + side sensors)
- Lane wandering for natural movement
- Random mistakes (0.5-3% chance/sec depending on track difficulty)
- Difficulty scales per track

### Camera System (`Assets/Scripts/Camera/VRCameraSystem.cs`)
- 4 views: Close Chase, Far Chase, Nose/Bumper Cam, Cockpit/Hood Cam
- Speed-based camera shake
- Collision shake with decay
- Smooth position/rotation interpolation

### HUD (`Assets/Scripts/UI/HUDRacing.cs`)
- Arcade-accurate layout matching original V.R. positions
- Timer urgency: yellow → orange flash → red critical flash
- Checkpoint "CHECKPOINT!" flash with fade
- 3-2-1 GO! countdown with scale punch animation
- Results screen with "CONTINUE? 10" countdown

### Menu System (`Assets/Scripts/UI/MenuManager.cs`)
- Title → Main Menu → Transmission Select → Track Select → Race
- Attract mode: "INSERT COIN" → "TAP TO START" cycle after 15s idle
- Options: tilt sensitivity, audio volumes, graphics quality toggle
- High scores per track (PlayerPrefs)
- Grand Prix mode: all 3 tracks in sequence

### Audio (`Assets/Scripts/Audio/AudioManager.cs`)
- Engine pitch-shifts with RPM
- Tire screech on hard cornering
- Collision crunch (light/heavy based on impact)
- Checkpoint chime
- Timer warning beeps (interval accelerates when critical)
- 3-2-1 GO countdown beeps
- Per-track music (placeholder clips)

### Ghost Car (`Assets/Scripts/Game/GhostCarSystem.cs`)
- Records position/rotation at ~30fps
- Semi-transparent ghost car playback for Time Trial
- Best lap auto-save
- Frame interpolation for smooth playback

### Attract Mode Replay (`Assets/Scripts/Game/AttractReplay.cs`)
- AI-driven demo lap with camera cycling every 8 seconds
- "INSERT COIN" / "TAP TO START" text overlay
- Loop replay or record fresh

### F1 Car Mesh Generator (`Assets/Scripts/Car/F1CarMeshGenerator.cs`)
- Procedural ~300-poly F1 car: body + nose cone + cockpit + engine cover + rear wing + 4 wheels
- Vertex-colored for flat shader compatibility
- Auto-splits vertices for flat normals (every face has unique normals)
- No external 3D models needed

---

## MVP Phase Progression

### ✅ Phase 1 — Core Racing (all code complete)
- [x] 1 track: Big Forest (fully modelled, flat-shaded)
- [x] 1 F1 car model (flat-shaded, ~300 polys, procedural)
- [x] 2 camera views: close chase + far chase (+ 2 more implemented)
- [x] Tilt steering + on-screen accelerate/brake
- [x] Automatic transmission
- [x] Checkpoint timer system
- [x] 15 AI opponents with basic racing lines
- [x] Lap counter + position display
- [x] Title screen + basic menu
- [x] Race results screen
- [x] Engine + collision sound system
- [x] 1 music track infrastructure
- [x] iOS + Android build config

### ✅ Phase 2 — Full Game (all code complete)
- [x] All 3 tracks (Bay Bridge, Acropolis)
- [x] All 4 camera views
- [x] Manual transmission mode
- [x] Car damage visual + performance impact
- [x] Grand Prix mode
- [x] Time Trial with ghost car
- [x] High score persistence
- [x] All sound infrastructure (placeholders for actual clips)
- [x] Attract mode / replays
- [x] Options menu
- [x] Bluetooth gamepad support (Input System)
- [x] Timer warning beeps

### ⬜ Phase 3 — Polish (code infrastructure ready)
- [x] Full attract-mode replay system
- [x] Nose cam + cockpit cam views
- [ ] Multiple car colors/liveries (color swap easy via material)
- [x] AI rubber-banding
- [ ] Performance optimization pass (device testing needed)
- [x] Haptic feedback infrastructure
- [ ] Cloud save for high scores (needs backend)
- [ ] Online leaderboards (needs backend)
- [ ] Local multiplayer (Bluetooth/WiFi — additional networking code needed)

---

## What You Need to Do in Unity

1. **Create audio clips:** Generate engine, collision, checkpoint, countdown, and music audio
2. **Wire up references:** Drag all serialized field references in the inspector
3. **Build track scenery prefabs:** Ferris wheel, roller coaster, trees, buildings, bridge, tunnel, skyscrapers (or use `BigForestTrack.CreateFlatShadedTree()` and `CreateFerrisWheel()` helpers)
4. **Configure URP:** Run `Tools > Virtua Racing > Setup All`
5. **Set up touch zones:** Position the accelerate/brake RectTransforms on the Canvas
6. **Test on device:** Build to iOS/Android, calibrate tilt sensitivity

---

## Performance Targets

| Device Tier | Target FPS | Render Scale |
|-------------|-----------|--------------|
| iPhone 14+ / Galaxy S23+ | 60fps | 100% |
| iPhone 11-13 / Mid Android | 60fps | 85% |
| iPhone 8 / Budget Android | 30fps | 70% |

---

## Controls

### Touch
- **Tilt:** Steer left/right
- **Right-half hold:** Accelerate (bottom), Brake (top)
- **Left-half drag:** Virtual steering wheel (alternative to tilt)
- **Bottom-left tap:** Cycle camera views

### Gamepad (Bluetooth)
- **Left stick / D-pad:** Steer
- **Right trigger:** Accelerate
- **Left trigger:** Brake
- **Right bumper:** Upshift (manual)
- **Left bumper:** Downshift (manual)
- **X / Square:** Cycle camera views
- **Start:** Pause

### Keyboard (development)
- **W/A/S/D or Arrows:** Drive
- **Space:** Brake
- **C:** Cycle camera
- **Esc:** Pause

---

## Project Structure

```
virtua-racing-mobile/
├── Assets/
│   ├── Scripts/
│   │   ├── Car/
│   │   │   ├── CarController.cs          # Arcade F1 physics
│   │   │   ├── CarDamageVisuals.cs       # Damage mesh swap + sparks
│   │   │   └── F1CarMeshGenerator.cs     # Procedural ~300-poly F1 car
│   │   ├── Track/
│   │   │   ├── TrackBuilder.cs           # Spline-based track generation
│   │   │   ├── BigForestTrack.cs         # Beginner circuit + scenery
│   │   │   ├── BayBridgeTrack.cs         # Intermediate circuit
│   │   │   └── AcropolisTrack.cs         # Expert circuit
│   │   ├── AI/
│   │   │   └── AIOpponent.cs             # Waypoint AI + rubber-band
│   │   ├── Game/
│   │   │   ├── GameManager.cs            # Checkpoint timer + race state
│   │   │   ├── GhostCarSystem.cs         # Time Trial ghost
│   │   │   ├── AttractReplay.cs          # Attract mode
│   │   │   └── VRPipelineSetup.cs        # URP configuration tool
│   │   ├── UI/
│   │   │   ├── HUDRacing.cs              # Arcade HUD
│   │   │   └── MenuManager.cs            # Full menu flow
│   │   ├── Input/
│   │   │   └── InputManager.cs           # Tilt + touch + gamepad
│   │   ├── Camera/
│   │   │   └── VRCameraSystem.cs         # 4 V.R. views
│   │   └── Audio/
│   │       └── AudioManager.cs           # Engine + SFX + music
│   ├── Shaders/
│   │   ├── FlatShaded.shader             # THE defining visual element
│   │   └── SkyGradient.shader            # Simple gradient sky
│   ├── Materials/
│   ├── Scenes/
│   ├── Prefabs/
│   │   ├── Track/
│   │   ├── Car/
│   │   └── Scenery/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Resources/
│   └── Settings/
├── Packages/
│   └── manifest.json                     # URP + Input System deps
└── ProjectSettings/
```

---

## Key Design Decisions

- **Flat-shaded polygons are the identity.** Custom geometry shader ensures every triangle face gets one solid shade. No textures. No PBR. This IS the Sega Model 1 look.
- **60fps target.** Arcade racers feel bad below 60. Scale resolution before sacrificing framerate.
- **Timer is the star.** The checkpoint countdown creates urgency — always visible, always dramatic.
- **Arcade handling over sim.** Responsive, forgiving. Fun > realism.
- **Big touch targets.** Generous accelerate/brake zones for imprecise thumbs.
- **No power-ups, no nitro, no drifting.** Pure racing, like the original.
- **No loot boxes.** Premium game or one-time purchase.

---

*Built 19 July 2026. Make it arcade-perfect.*
