from pathlib import Path
import textwrap, json
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assignment_Documents'
W = 'Assets/Scripts/Weapons/'
P = 'Assets/Scripts/Player/'
E = 'Assets/Scripts/Enemy/'

descriptions = {
W+'WeaponCommands.cs': 'Defines the common Execute contract, the three reusable action objects, and the invoker that dispatches a request to the player.',
P+'Player.cs': 'Owns player input, movement context, weapon command instances and receiver methods. Weapon requests validate life status and gun availability; reload updates spare ammunition.',
W+'Gun.cs': 'Owns magazine ammunition, shooting intent, reload flags and the gun state machine. Awake composes effective weapon statistics from optional decorators.',
W+'WeaponStats.cs': 'Defines the shared statistics interface, validated base values and composable wrappers for damage and reload duration.',
W+'GunStateMachine.cs': 'Caches gun states, delegates frame updates, and changes state by calling ExitState before EnterState. It rejects requests from a stale state.',
W+'States/GunState.cs': 'Declares gun state identifiers and the shared lifecycle contract used by each concrete gun state.',
W+'States/GunIdleState.cs': 'Transitions to Shooting when firing is requested with ammunition, or to Reloading when a reload has started.',
W+'States/ShootingState.cs': 'Enforces cooldown, ammunition and reload restrictions. A single ray finds the first solid hit and applies effective damage to an enemy in an allowed layer.',
W+'States/ReloadingState.cs': 'Uses effective reload duration for the state deadline and animation. Exiting clears the reload flag and ends the animation.',
W+'GunAnimation.cs': 'Provides weapon shot and reload presentation called by the business logic. Reload timing is supplied by ReloadingState.',
P+'PlayerStateMachine.cs': 'Caches movement states and delegates updates. Death overrides other transitions and becomes terminal; stale state callers are rejected.',
E+'EnemyStateMachine.cs': 'Caches AI states, selects melee or ranged attack behavior at construction, and makes death terminal.',
E+'Enemy.cs': 'Provides AI context, target detection and memory, navigation, damage handling and death behavior used by the enemy states.',
E+'RangedEnemy.cs': 'Provides the ranged attack receiver used by EnemyRangedAttackState, including aim history, burst timing, collision checks and player damage.',
E+'HealthComp.cs': 'Stores enemy health accessed by Enemy and the enemy death checks.',
E+'EnemyPlayerHealth.cs': 'Initializes player health and processes positive damage. On lethal damage it directly calls Player.Die and invokes the OnDeath UnityEvent.',
P+'GameOverScreen.cs': 'Shows the game over overlay and reloads the active scene on Retry. Its Show method is currently called directly by Player.StopForDeath.',
P+'States/PlayerState.cs': 'Declares movement state identifiers and the shared lifecycle contract.',
P+'States/IdleState.cs': 'Stops horizontal movement and prioritizes transitions to Jump, Fall and Walk.',
P+'States/WalkState.cs': 'Applies walking velocity and transitions to Jump, Fall or Idle as input and grounding change.',
P+'States/JumpState.cs': 'Schedules one physics jump impulse and provides air control until vertical velocity turns downward.',
P+'States/FallState.cs': 'Applies air control and returns to Walk or Idle when grounded.',
P+'States/PlayerDeadState.cs': 'Calls StopForDeath once on entry and provides no ongoing movement behavior.',
E+'States/EnemyState.cs': 'Declares enemy state identifiers and the shared AI lifecycle contract.',
E+'States/EnemyIdleState.cs': 'Waits for the idle timer or detects the player and enters Chase.',
E+'States/EnemyPatrolState.cs': 'Follows assigned or randomly sampled reachable patrol destinations, returning to Idle on arrival or timeout.',
E+'States/EnemyChaseState.cs': 'Pursues the last seen position, attacks when a visible target is in range, and bounds investigation with arrival and timeout checks.',
E+'States/EnemyAttackState.cs': 'Faces the player and deals melee damage after a windup, respecting cooldown, range and visibility.',
E+'States/EnemyRangedAttackState.cs': 'Starts and cancels ranged attacks on entry and exit, manages distance, and ticks weapon attacks only after facing the target.',
E+'States/EnemyDeadState.cs': 'Calls enemy death behavior once on entry.',
}

configs = [
dict(name='Observer', intro='This document explains the death notification event in the FPS Setup 2 Unity project and includes the related business logic file by file. The project contains an Observer style publisher, but the saved implementation does not register an observer.',
sections=[('Pattern and current implementation', [
'Observer separates a publisher from subscribers that react to a notification. Here, EnemyPlayerHealth exposes a UnityEvent named OnDeath. UnityEvent supplies listener registration and invocation, so a separate custom observer interface is not required.',
'TakeDamage ignores nonpositive damage and already dead players. It reduces health and, on lethal damage, calls Player.Die directly before invoking OnDeath. The early return for an already dead player prevents repeated death notifications from later damage.',
'The saved Player prefab has an empty OnDeath persistent listener list, and the project scripts contain no AddListener or RemoveListener calls. Player death and the game over screen therefore use direct calls rather than registered observers. HealthBarDisplay also reads health in OnGUI; it is a polling display, not an event subscriber.']),
('Death flow', ['Enemy or ranged weapon calls EnemyPlayerHealth.TakeDamage. Lethal damage calls Player.Die, which enters PlayerDeadState. That state calls Player.StopForDeath to disable controls and weapons and show GameOverScreen. OnDeath.Invoke then notifies any listeners that have been supplied externally.']),
('Listener integration needed for a complete example', ['A complete Observer demonstration would register an independent listener, for example one that updates a score or records a death, through OnDeath.AddListener in OnEnable and remove the same listener in OnDisable. Alternatively, a compatible method can be assigned to OnDeath in the Inspector. These are integration steps, not changes present in the source listings below. The direct death calls should not be described as event subscriptions.'])],
files=[E+'EnemyPlayerHealth.cs',P+'Player.cs',P+'PlayerStateMachine.cs',P+'States/PlayerState.cs',P+'States/PlayerDeadState.cs',P+'GameOverScreen.cs']),
dict(name='State', intro='This document presents the State pattern used for player movement, weapon behavior and enemy AI in FPS Setup 2. Each system stores one active state and delegates its behavior to a concrete state object. The relevant business logic is merged below by source file.',
sections=[('Pattern roles', ['Player, Gun and Enemy are the gameplay contexts. PlayerState, GunState and EnemyState define lifecycle methods. Each state machine caches concrete states in a dictionary and forwards Update and FixedUpdate to the active state.',
'A transition exits the current state, replaces it and enters the next state. TryChangeState accepts a request only from the currently active state, preventing stale states from initiating a second transition. Player and enemy death states are terminal.']),
('Player movement', ['Idle and Walk prioritize jump requests. Jump applies its impulse during FixedUpdate, then changes to Fall when upward motion finishes. Fall returns to Idle or Walk on landing. Health reaching zero overrides normal movement and enters Dead, which stops controls and weapons.']),
('Weapon behavior', ['Idle enters Shooting when TryShoot is true, ammunition remains and reloading is false. Shooting applies cooldown and ammunition checks and returns to Idle on release or an empty magazine. A reload request sets the reload flag and transfers ammunition immediately; Reloading then prevents shooting until its deadline.']),
('Enemy behavior', ['Idle alternates with Patrol until the player is detected. Chase follows the last seen position and enters Attack when a visible target is within the configured attack distance. Attack uses either the melee or ranged concrete implementation selected when the machine is constructed. Losing the target returns to pursuit or idle behavior; zero health enters Dead.']),
('Relationship to the other patterns', ['Commands express player weapon requests. States decide the behavior allowed at the current moment. Decorators provide effective weapon values used by the shooting and reloading states. The three mechanisms cooperate without replacing one another.'])],
files=[P+'Player.cs',P+'PlayerStateMachine.cs']+[P+'States/'+n+'.cs' for n in ['PlayerState','IdleState','WalkState','JumpState','FallState','PlayerDeadState']]+[W+'Gun.cs',W+'GunStateMachine.cs']+[W+'States/'+n+'.cs' for n in ['GunState','GunIdleState','ShootingState','ReloadingState']]+[E+'Enemy.cs',E+'EnemyStateMachine.cs']+[E+'States/'+n+'.cs' for n in ['EnemyState','EnemyIdleState','EnemyPatrolState','EnemyChaseState','EnemyAttackState','EnemyRangedAttackState','EnemyDeadState']]+[E+'RangedEnemy.cs',E+'HealthComp.cs',E+'EnemyPlayerHealth.cs']),
dict(name='Command', intro='This document explains how weapon input in FPS Setup 2 is represented by command objects. It records the changes made to accommodate the Command pattern and merges the command, receiver and weapon execution code by source file.',
sections=[('Pattern roles and request flow', ['IWeaponCommand declares Execute(Player player). StartFireCommand, StopFireCommand and ReloadCommand implement one request each. WeaponCommandInvoker dispatches immediately to the Player receiver. Player then validates the request and calls the current Gun.',
'Example flow: pressing R calls OnReload, which chooses ReloadCommand. The invoker calls Execute, the command calls Player.RequestReload, and the receiver updates PlayerAmmo from Gun.Reload. The gun state machine then handles the reload wait and animation.']),
('Changes made to accommodate Command', [
'Added Assets/Scripts/Weapons/WeaponCommands.cs. This file introduces the command interface, three concrete commands and the invoker. Commands hold behavior; the player receiver is supplied to Execute.',
'Updated Player.cs to cache one StartFireCommand, StopFireCommand and ReloadCommand as readonly fields. Awake constructs WeaponCommandInvoker(this). These objects are reused rather than allocated once per shot.',
'Changed OnShoot to select start or stop based on value.isPressed. Changed OnShootRelease to dispatch StopFireCommand. Changed OnReload to dispatch only when value.isPressed is true, avoiding a second reload request when the key is released.',
'Added RequestStartFire, RequestStopFire and RequestReload as receiver methods. Start and reload reject dead players and check for an active enabled gun. Reload keeps spare ammunition on the player. Stop remains available to clear firing intent, including after death.',
'Kept Gun.StartShoot, Gun.StopShoot and Gun.Reload as weapon operations. Holding fire sets TryShoot once; ShootingState controls individual shots and cooldown. Existing gun states still decide whether shooting or reload can proceed.',
'The project change notes record no required scene or input action asset changes for Command. Unity metadata accompanies the new command script. There is no command queue, undo or replay in this implementation; immediate dispatch still satisfies the pattern.']),
('Related weapon changes with a separate purpose', ['WeaponStats.cs, EffectiveStats and the upgrade checkboxes implement Decorator. The single shooting ray and removal of EnemyGunDamageBridge make hit detection and damage use the same shot. These accompanied the weapon work but are not required to accommodate Command itself.']),
('Behavior to demonstrate', ['Hold and release the fire button to send start and stop requests. After spending ammunition, press R once to request reload. A missing or disabled gun rejects new start and reload requests. A dead player cannot start fire or reload, while stop remains safe.'])],
files=[W+'WeaponCommands.cs',P+'Player.cs',W+'Gun.cs',W+'GunStateMachine.cs',W+'States/GunState.cs',W+'States/GunIdleState.cs',W+'States/ShootingState.cs',W+'States/ReloadingState.cs']),
dict(name='Decorator', intro='This document presents the composable weapon statistics in FPS Setup 2. Optional decorators wrap a base statistics object to double damage or halve reload duration. The relevant business logic and its shooting and reload consumers are merged by source file.',
sections=[('Pattern roles', ['IWeaponStats is the component contract. BaseWeaponStats supplies damage, fire rate, range and reload duration. WeaponStatsDecorator holds another IWeaponStats object and forwards values. DamageBoostDecorator overrides Damage, while FastReloadDecorator overrides ReloadDuration.']),
('Construction and use', ['Gun.Awake builds BaseWeaponStats from the serialized gun values. When DoubleDamage is enabled, it wraps the current object with DamageBoostDecorator. When FastReload is enabled, it adds FastReloadDecorator. The resulting object is exposed as Gun.EffectiveStats.',
'ShootingState reads EffectiveStats.Damage, Range and FireRate. ReloadingState uses EffectiveStats.ReloadDuration for both its deadline and BeginReload animation call. Magazine ammunition and gun state remain mutable context fields outside the decorators.']),
('Composition example', ['For base damage 25 and reload duration 1 second, no upgrades produce 25 damage and 1 second. DamageBoost alone produces 50 damage and 1 second. FastReload alone produces 25 damage and 0.5 seconds. Both produce 50 damage and 0.5 seconds. Fire rate and range pass through unchanged.',
'BaseWeaponStats clamps damage, range and reload duration to zero or greater, and fire rate to at least 0.01. The wrappers preserve the same interface, so consumers do not need an upgrade specific gun subclass.']),
('Gameplay setup', ['Set the Double Damage and Fast Reload checkboxes on the Gun component before Play. Awake composes the wrappers once; changing those checkboxes during Play does not rebuild EffectiveStats. Both upgrades are off in the saved player prefab. The gameplay screenshot appended below shows the baseline configuration and does not demonstrate upgraded damage or reload timing.'])],
files=[W+'WeaponStats.cs',W+'Gun.cs',W+'GunStateMachine.cs',W+'States/GunState.cs',W+'States/GunIdleState.cs',W+'States/ShootingState.cs',W+'States/ReloadingState.cs',W+'GunAnimation.cs'])
]

manifest = []
for cfg in configs:
    d = Document()
    sec = d.sections[0]
    sec.page_width,sec.page_height=Inches(8.27),Inches(11.69)
    sec.top_margin=sec.bottom_margin=Inches(.65)
    sec.left_margin=sec.right_margin=Inches(.7)
    normal=d.styles['Normal']; normal.font.name='Calibri';normal.font.size=Pt(11)
    normal.paragraph_format.space_after=Pt(7)
    for name in ['Title','Subtitle','Heading 1','Heading 2','Heading 3']:
        d.styles[name].font.color.rgb=RGBColor(0,0,0)
    d.styles['Title'].font.size=Pt(25)
    for name in ['Heading 1','Heading 2']:
        d.styles[name].paragraph_format.keep_with_next=True
    code=d.styles.add_style('Source Code',1)
    code.font.name='Consolas';code.font.size=Pt(8.5)
    code.paragraph_format.space_after=Pt(0)
    code.paragraph_format.space_before=Pt(0)
    code.paragraph_format.line_spacing=Pt(10.5)
    code.paragraph_format.keep_together=True
    code.paragraph_format.widow_control=False
    d.core_properties.title=cfg['name']+' Design Pattern in FPS Setup 2'
    d.core_properties.subject='Design pattern assignment with business logic and gameplay screenshot'
    d.add_paragraph(cfg['name']+' Design Pattern', 'Title')
    d.add_paragraph('FPS Setup 2 Unity Project','Subtitle')
    d.add_paragraph(cfg['intro'])
    for title, paras in cfg['sections']:
        d.add_heading(title,1)
        for text in paras: d.add_paragraph(text)
    d.add_heading('Source file index',1)
    d.add_paragraph('The following listings contain the complete text of each named C# file. Shared files appear in more than one document so each pattern can be reviewed independently. Referenced presentation helpers and Unity engine classes remain project dependencies.')
    for i,f in enumerate(cfg['files'],1):d.add_paragraph(f'{i:02d}  {f}')
    for i,f in enumerate(cfg['files'],1):
        d.add_page_break()
        d.add_heading(f'File {i:02d}  {Path(f).name}',1)
        p=d.add_paragraph(f);p.runs[0].font.size=Pt(9)
        d.add_paragraph(descriptions[f])
        source=(ROOT/f).read_text(encoding='utf-8-sig')
        for line in source.splitlines():
            # Visual wrapping preserves the exact source characters, excluding tabs expanded for display.
            line=line.expandtabs(4)
            chunks=textwrap.wrap(line,width=91,expand_tabs=False,replace_whitespace=False,drop_whitespace=False,break_long_words=True,break_on_hyphens=False) or ['']
            for chunk in chunks:d.add_paragraph(chunk,'Source Code')
    d.add_page_break()
    d.add_heading('Gameplay screenshot',1)
    d.add_paragraph('FPS Setup 2 running in Unity Play mode in GameScene. The first person weapon, arena, enemies and health displays are visible.')
    pic=d.add_picture(str(OUT/'qa/gameplay.png'),width=Inches(6.8))
    pic._inline.docPr.set('descr','Gameplay showing a first person rifle in an arena, enemies ahead and health bars.')
    d.add_paragraph('Figure 1  Gameplay captured from the running project on 30 October 2026. The image is cropped to the Game view. It shows the shared game context; pattern structure is established by the source listings.','Caption')
    foot=sec.footer.paragraphs[0];foot.alignment=2
    foot.add_run(cfg['name']+'  |  ')
    field=OxmlElement('w:fldSimple');field.set(qn('w:instr'),'PAGE');foot._p.append(field)
    dest=OUT/(cfg['name']+'_Design_Pattern.docx')
    d.save(dest)
    manifest.append({'name':cfg['name'],'path':str(dest),'files':cfg['files'],'source_file_count':len(cfg['files'])})
(OUT/'qa/manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('\n'.join(x['path'] for x in manifest))
