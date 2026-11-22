# Game design patterns, in simple words

## What was already in the project?

The project already used the **State pattern**.
The player, gun, and enemies each have states.
For example, the gun can be idle, shooting, or reloading.
A state decides what the gun can do right now.

The project did not have the Command or Decorator patterns described below.
These two patterns have now been added to the player's weapon code.
Observer is also connected now: the game-over screen listens for the player's death.

## 1. Command pattern: an action becomes an object

Before, pressing a weapon button called the gun directly.
Now, the input code sends a command object.

**Press reload -> reload command -> gun reloads.**

The parts are:

| Part | Name in this project | What it does |
| --- | --- | --- |
| Shared rule | `IWeaponCommand` | Says that every command has an `Execute` method. |
| Commands | `StartFireCommand`, `StopFireCommand`, `ReloadCommand` | Each asks for one weapon action. |
| Invoker | `WeaponCommandInvoker` | Runs the command straight away. |
| Receiver | `Player` | Checks the player and gun, then calls the gun. |

For reload, the steps are:

1. The player presses R.
2. `OnReload` sends `ReloadCommand` to the invoker.
3. The invoker calls the command's `Execute` method.
4. The command calls `Player.RequestReload`.
5. The player checks that they are alive and have an active gun.
6. The gun loads ammo. The player keeps the remaining spare ammo.
7. The gun's existing states handle the reload wait and animation.

The command objects are made once and reused.
Two more commands, `ToggleDamageCommand` and `ToggleFastReloadCommand`, now handle upgrade keys.
F1 sends the damage toggle command. F2 sends the fast reload toggle command.
They use the same invoker and player checks as the other weapon requests.
Holding the mouse button does not make one command per bullet.
The start command tells the gun to keep firing.
The shooting state still decides when each bullet can fire.
The stop command stops firing when the button is released.
It can also stop firing after the player dies.

**Why this is Command:** each request is a separate object with the same method.
The input code only needs to choose a command.
It does not need to know how the gun carries out that request.

There is no queue, undo, or replay feature in this assignment.
Those features are not needed to use this pattern.

## 2. Decorator pattern: wrap the normal stats with upgrades

A decorator wraps another object and adds a change.
It still follows the same shared rule as the object inside it.

**Normal gun -> more damage -> faster reload.**

The parts are:

| Part | Name in this project | What it does |
| --- | --- | --- |
| Shared rule | `IWeaponStats` | Gives damage, fire rate, range, and reload time. |
| Starting object | `BaseWeaponStats` | Holds the normal values from the gun. |
| Shared wrapper | `WeaponStatsDecorator` | Keeps the object inside it and passes its values along. |
| Damage wrapper | `DamageBoostDecorator` | Doubles damage. Keeps the other values. |
| Reload wrapper | `FastReloadDecorator` | Halves reload time. Keeps the other values. |

The gun makes its stats in `Awake`, when it starts.
It first makes the base stats.
If Double Damage is checked, it wraps them with the damage decorator.
If Fast Reload is checked, it adds the reload decorator too.
The result is stored in `Gun.EffectiveStats`.

The shooting code reads the final damage, range, and fire rate.
The reload state reads the final reload time.
The reload animation uses that same time.

**Why this is Decorator:** the base object and both wrappers use `IWeaponStats`.
Each wrapper keeps another `IWeaponStats` object inside it.
It changes one value and passes the other values through.
Both upgrades can work together without making a new gun class for each mix.

The checkboxes only choose which wrappers to build.
The actual upgrade work happens in the decorator classes.

Ammo and the current gun state are not wrapped.
They still belong to the existing gun code.

## How to try the upgrades in Unity

1. Stop Play mode.
2. Open `Assets/Prefabs/Player.prefab`.
3. Find the child object with the `Gun` component, under `GunHolder`.
4. Look for **Starting upgrades (F1/F2 toggle during Play)**.
5. Check **Double Damage**, **Fast Reload**, or both.
6. Save the prefab and open `Assets/Scenes/GameScene.unity`.
7. Press Play.

The Inspector options set the starting upgrades.
During Play, press **F1** to turn double damage on or off.
Press **F2** to turn fast reload on or off.
Click inside the Game view first so it receives the keys.
A box at the top left shows the keys, active upgrades, damage, and reload time.
These keys work on the player's current active gun while the player is alive.
They do not work while the game is paused.

Every key toggle rebuilds the wrappers from the base stats.
This means pressing F1 repeatedly cannot keep doubling the damage.
Turning an upgrade off restores its normal value and keeps the other upgrade.
Damage changes apply to the next shot.
Fast reload changes apply to the next reload. A reload already running keeps its original timer and animation.
The choices reset to the saved starting options when the scene restarts.
Both options start off unchecked.
The game scene uses this player prefab, so it receives the prefab changes.

| Options | Damage per hit | Reload time |
| --- | --- | --- |
| Neither | 25 | 1 second |
| Double Damage | 50 | 1 second |
| Fast Reload | 25 | 0.5 seconds |
| Both | 50 | 0.5 seconds |

Fire rate stays at 10 shots per second.
Range stays at 100 units. The magazine still holds 100 rounds.
The existing game still moves ammo into the magazine when reload starts.
The reload wait still prevents shooting until reload ends.

## One more change: damage uses the same shot

Before, the shooting state fired a ray to see what was hit.
`EnemyGunDamageBridge` then noticed that ammo had gone down.
It fired a second ray later to deal damage.

Now, the shooting state's ray also deals the damage.
Each shot can damage the first enemy it hits once.
It can find the enemy even when the collider is on a child object.
A wall in front of the enemy blocks the shot.
The ray includes normal solid layers, even if only the enemy layer can take damage.
Trigger colliders are ignored. Walls should use normal solid colliders.

The bridge's damage value of 25 and enemy layer setting were moved to `Gun`.
The old bridge component and script were removed.
The existing shooting and reload sounds were kept.

## How the three patterns work together

- **Command:** What did the player ask for? Example: start firing.
- **State:** What can the gun do now? Example: wait for reload to finish.
- **Decorator:** What stats does the gun use? Example: deal 50 damage.

The new patterns do not replace the state machines.
Movement, looking, and enemy AI still use their existing code.
There are no pickups or inventory screens in this change.

## Files changed

Paths below start from the project folder.

| File | Change |
| --- | --- |
| `Assets/Scripts/Weapons/WeaponCommands.cs` | Added the command rule, fire and reload commands, two upgrade toggle commands, and invoker. |
| `Assets/Scripts/Weapons/WeaponStats.cs` | Added the stats rule, base stats, shared wrapper, and two upgrade wrappers. |
| `Assets/Scripts/Player/Player.cs` | Input now sends commands. Added receiver methods that check and carry out requests. Reload only responds to the key press. F1 and F2 send upgrade commands. Added the upgrade status box. |
| `Assets/Scripts/Weapons/Gun.cs` | Added damage, damage layers, upgrade checkboxes, and the wrapped stats. Added toggle methods that rebuild the wrappers during Play. |
| `Assets/Scripts/Weapons/States/ShootingState.cs` | Uses final stats and deals damage from one ray. Keeps ammo, sound, and shot animation. |
| `Assets/Scripts/Weapons/States/ReloadingState.cs` | Uses the final reload time for both the wait and animation. |
| `Assets/Prefabs/Player.prefab` | Moved bridge settings to the gun. Removed the bridge component. Kept both upgrades off. |
| `Assets/Scripts/Enemy/EnemyGunDamageBridge.cs` | Removed. Shooting now handles damage itself. |
| `Assets/Editor/WeaponPatternChecks.cs` | Added checks that can run from Unity's Tools menu. They use a temporary scene and do not save it. |
| `DESIGN_PATTERNS.md` | Added this explanation. |

Unity `.meta` files were added for the new scripts and the Editor folder.
The old bridge's `.meta` file was removed with its script.
The game scene and input action files did not need changes.
Temporary build files and check logs are kept under `Temp` and `Logs`.
They are not gameplay files.

## Checks and a short demonstration

### Checks performed

- All project C# files, including the Editor check script, compiled against the installed Unity files. There were no warnings or errors.
- The real stats code passed a separate test for all four upgrade mixes, the wrappers in the opposite order, unchanged fire rate and range, and safe values for bad base settings.
- The player prefab's normal values and its link from the game scene were checked in the saved files.
- The removed bridge has no remaining references in scene or prefab files.

### Checks that still need Unity

Headless Unity exited before it could run the gameplay checks.
Its log is `Logs/WeaponPatternChecks.log`.
The cause of that early exit was not confirmed.

To run the added checks, open the project in Unity, stop Play mode, and choose
**Tools -> Check Weapon Patterns**.
Keep the saved player prefab's upgrades off for this baseline check.
The Console should show **WEAPON PATTERN CHECKS PASSED**.

The check covers all four stat mixes, command requests, reload ammo,
missing and disabled guns, dead-player requests, cooldowns, empty ammo,
one damage application per shot, child hitboxes, walls, upgraded shot damage,
and matching reload and animation times.

These checks call the weapon code directly in the Editor.
They do not replace trying the real controls in Play mode.

### Show the assignment

1. Leave both upgrades off and press Play.
2. Hold the left mouse button. The gun should keep firing.
3. Release it. Firing should stop.
4. After using some ammo, press R. Reload should use the available spare ammo.
5. Explain the command flow: button -> command -> player -> gun.
6. Press F1 and compare enemy health loss per hit. Press it again to turn the upgrade off.
7. Press F2 and compare the next reload wait. Press F1 too to try both upgrades together.
8. Explain the decorator flow: base stats -> damage wrapper -> reload wrapper.
9. Check that a wall protects an enemy behind it.
10. Check that death stops firing and prevents new fire and reload requests.

Real mouse and keyboard input, the visible animation, sound playback,
and death during live gameplay still need this Play mode check.

## 3. Observer pattern: listen for player death

**Player health announces death -> the game-over screen hears it -> the screen appears.**

Think of a doorbell. Pressing the bell sends a signal.
Anything connected to that signal can respond.
The button does not need to know how each connected device works.

Here, the health component sends the signal.
The game-over screen listens for that signal.

| Part | Code | Simple meaning |
| --- | --- | --- |
| Publisher / subject | `EnemyPlayerHealth` | Knows when health reaches zero. |
| Event | `OnDeath` | The death announcement. |
| Observer / listener | `GameOverScreen` | Waits for the announcement. |
| Response | `Show()` | Displays the game-over screen and unlocks the mouse. |

### How the code works

1. When the screen is enabled, `OnEnable()` finds the health component.
2. `health.OnDeath.AddListener(Show)` signs the screen up for death announcements.
3. An enemy damages the player through `TakeDamage()`.
4. When health reaches zero, the player enters the existing dead state. That stops movement and shooting.
5. Health then calls `OnDeath.Invoke()` to announce the death.
6. Unity calls every method signed up to that event. The screen's `Show()` method is one of them.
7. `Show()` sets `IsShowing` to true. `OnGUI()` draws the screen and Retry button.
8. When the screen is disabled or destroyed, `OnDisable()` calls `RemoveListener(Show)`. It stops listening.

The health code already ignores damage when the player is dead.
So later hits do not keep sending the same death announcement.
The screen removes its listener when disabled, so enabling it again does not build up extra listeners.

### What changed for Observer

- `Assets/Scripts/Player/GameOverScreen.cs`: added listener registration in `OnEnable` and removal in `OnDisable`. The screen now requires a health component on the same object.
- `Assets/Scripts/Player/Player.cs`: removed the direct call to `GameOverScreen.Show()` from `StopForDeath()`.
- `Assets/Scripts/Enemy/EnemyPlayerHealth.cs`: kept the existing death event and made the death code easier to read with a short comment.
- `DESIGN_PATTERNS.md`: added this explanation.

No Inspector event setup is needed. The listener is connected by code.
The Inspector's On Death list can stay empty; it does not list listeners added by `AddListener` during play.
Retry still reloads the current game scene, which creates a fresh health component and screen.

### Why this is Observer

Health announces what happened. The screen decides how to react.
Health does not call the game-over screen directly.
Another feature could listen to the same event later without adding screen-specific code to health.
The existing dead state still handles stopping the player.

### Easy assignment explanation

"I used Observer for the game-over screen. The screen subscribes to the player's death event.
When health reaches zero, health sends the event and the screen shows itself.
The screen unsubscribes when disabled. This keeps the health code separate from the screen code."

### Try it

1. Start the game and let an enemy damage the player.
2. The screen should stay hidden while health is above zero.
3. When health reaches zero, movement and shooting should stop and Game Over should appear.
4. Click Retry. The player should start alive with the screen hidden.
5. For a demonstration, disable just the GameOverScreen component before death. The player should still die, but the screen should not appear because it is no longer listening.

Compilation is checked separately. The live death, screen, and Retry flow still needs a Play mode check.
