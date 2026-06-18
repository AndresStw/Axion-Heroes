# 🎮 Axion Heroes

An action-packed MOBA/RPG game developed in **Unity** featuring dynamic hero control systems, real-time combat, and AI-driven gameplay mechanics.

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Features](#features)
- [Core Systems](#core-systems)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Installation](#installation)
- [Controls](#controls)
- [Technologies](#technologies)
- [Game Mechanics](#game-mechanics)
- [Contributing](#contributing)
- [License](#license)

---

## 🎯 About the Project

**Axion Heroes** is an action game that combines MOBA (Multiplayer Online Battle Arena) and RPG mechanics. Players control heroes with unique abilities, engage in tactical combat, and progress through experience and leveling systems. The game features both player-controlled and AI-driven heroes, creating dynamic gameplay scenarios.

---

## ✨ Features

- **Hero Control System**: Smooth character movement and rotation with NavMesh integration
- **Combat System**: Basic attacks, special abilities, and ultimate skills with cooldown management
- **AI Bots**: Intelligent AI opponents that adapt to player actions
- **Experience & Leveling**: Dynamic progression system with experience points and level scaling
- **Mobile Support**: Joystick input for mobile platforms
- **AFK Detection**: Automatic AI activation when player is inactive
- **Respawn System**: Dynamic respawn timers based on hero level
- **Animation System**: Integrated Animator controller for smooth character animations
- **Enemy Variety**: Combat support for multiple enemy types (Shards, Sentinels, Heroes)

---

## 🔧 Core Systems

### Combat System
- **Basic Attacks**: Execute with spacebar (or customizable input)
  - Damage scaling with multipliers
  - Attack range and cooldown management
  - Enemy type detection (Heroes, Sentinels, Shards)

- **Abilities**: Special skills with cooldown management
  - Q Key: Execute special ability
  - Cooldown tracking and visual feedback

- **Ultimate Ability**: Powerful ultimate skill
  - R Key: Execute ultimate ability
  - Extended cooldown system

### Movement & Navigation
- NavMesh-based pathfinding
- Smooth character rotation and movement
- Keyboard input support (WASD)
- Mobile joystick integration
- Velocity-based animation control

### Progression System
- **Experience Gain**: Earn experience from defeated enemies
- **Leveling**: Auto-level up when experience thresholds are met
- **Max Level**: Cap at level 25 with increased experience requirements
- **Dynamic Scaling**: Experience requirements increase by 1.5x per level

### AI System (HeroBotAI)
- **Role-Based Behavior**: Different AI strategies per hero role
- **AFK Conversion**: Player converts to AI after 10 seconds of inactivity
- **Damage Feedback**: AI reacts to received damage
- **Automatic Combat**: AI engages enemies when activated

### Respawn Mechanics
- Base respawn time: 6 seconds
- Level scaling: +2 seconds per level
- Automatic health restoration
- Animation rebinding on respawn

---

## 📁 Project Structure

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Hero/
│   │   │   ├── HeroController.cs          # Main hero control logic
│   │   │   ├── HeroBotAI.cs               # AI decision making
│   │   │   └── HeroData.cs                # Hero statistics (ScriptableObject)
│   │   ├── Enemies/
│   │   │   ├── Shard_Controller.cs        # Shard enemy type
│   │   │   └── Sentinel_Controller.cs     # Sentinel enemy type
│   │   ├── Systems/
│   │   │   ├── EvolutionManager.cs        # Experience & leveling system
│   │   │   └── CombatSystem.cs            # Combat logic
│   │   └── UI/
│   │       └── MobileJoystick.cs          # Mobile input handling
│   ├── Animations/
│   │   ├── Heroes/                        # Hero animator controllers
│   │   └── Enemies/                       # Enemy animators
│   ├── Prefabs/
│   │   ├── Heroes/                        # Hero prefabs
│   │   └── Enemies/                       # Enemy prefabs
│   └── Data/
│       └── Heroes/                        # Hero stat ScriptableObjects
```

---

## 🚀 Getting Started

### Prerequisites

- **Unity** 2020.3 LTS or newer
- **C# 7.3** or later
- NavMesh Agent component
- Animator controller with the following parameters:
  - `Speed` (Float)
  - `Health` (Float)
  - `Attack` (Trigger)
  - `DoSkill` (Trigger)
  - `DoUlti` (Trigger)
  - `Die` (Trigger)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/AndresStw/Axion-Heroes.git
   cd Axion-Heroes
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Click "Add project from disk"
   - Select the Axion-Heroes folder

3. **Import Assets**
   - Ensure all packages are imported
   - Configure NavMesh for your scene

4. **Set Up Hero Data**
   - Create a `HeroData` ScriptableObject for each hero
   - Configure stats (health, attack speed, range, etc.)
   - Assign to HeroController

---

## 🎮 Controls

### Keyboard Controls (PC)

| Input | Action |
|-------|--------|
| **W/A/S/D** | Move character |
| **Space** | Basic attack |
| **Q** | Special ability |
| **R** | Ultimate ability |

### Mobile Controls

- **Joystick**: Move character
- **Screen Tap**: Ability buttons (depends on UI implementation)

---

## 🛠️ Technologies

| Technology | Usage |
|-----------|-------|
| **Unity Engine** | Game development framework |
| **C#** | Primary programming language (71.8%) |
| **NavMesh** | AI pathfinding and movement |
| **Animator** | Character animation system |
| **ShaderLab** | Custom shaders (23.4%) |
| **HLSL** | Shader compilation (4.8%) |

---

## 🎮 Game Mechanics

### Hero Attributes
- **Max Health**: Total hero health points
- **Attack Damage**: Base damage per attack
- **Attack Speed**: Attacks per second
- **Attack Range**: Distance for basic attacks
- **Movement Speed**: Hero movement velocity
- **Skill Cooldown**: Q ability cooldown
- **Ulti Cooldown**: R ability cooldown

### Experience Formula
- Base experience for kills varies by enemy type
- Experience scales with hero level
- Tower destruction awards experience

### Damage System
- Damage = Base Damage × Multiplier
- Type-specific interactions (Hero vs Shard vs Sentinel)
- Health clamping (0 to max)

### AI Behavior
- **Idle State**: Waits for player input or combat triggers
- **Combat State**: Engages enemies based on role
- **Damaged State**: Reacts to received damage
- **Dead State**: Awaits respawn

---

## 📝 Key Classes

### HeroController
Main component handling hero logic:
- Movement and rotation
- Combat input and execution
- Experience and leveling
- Respawn mechanics
- AI integration

**Key Methods:**
- `ExecuteAttackBasic()` - Performs basic attack
- `ExecuteSkill()` - Activates Q ability
- `ExecuteUltimate()` - Activates R ability
- `TakeDamage(float damage)` - Receive damage
- `LevelExperience(float cantidad)` - Gain experience
- `Die()` - Hero death logic
- `Respawn()` - Hero respawn logic

### HeroBotAI
Handles AI decision-making and behavior:
- Role-based strategies
- Enemy detection and targeting
- Combat engagement logic
- Damage response behavior

### HeroData (ScriptableObject)
Stores hero statistics:
- Health points
- Damage values
- Speed attributes
- Ability cooldowns
- Hero role definition

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📋 Roadmap

- [ ] Complete animation system
- [ ] Multiplayer integration
- [ ] Advanced hero abilities
- [ ] Item system and upgrades
- [ ] Map and environment design
- [ ] Sound and music implementation
- [ ] Particle effects and visual polish
- [ ] UI overhaul
- [ ] Balance adjustments

---

## 🐛 Known Issues

- Hero model and animations are placeholders
- Some combat mechanics are simulated
- Mobile input needs UI implementation

---

## 💬 Contact

**Author**: [AndresStw](https://github.com/AndresStw)

For questions or suggestions, feel free to open an issue or reach out!

---

## 📄 License

This project is currently unlicensed. See the LICENSE file for details (if applicable).

---

## 🙏 Acknowledgments

- Unity community for resources and support
- MOBA/RPG game design inspiration
- All contributors and testers

---

**Enjoy playing Axion Heroes!** 🎮✨
