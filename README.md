# 🎮 Axion Heroes

![Unity](https://img.shields.io/badge/Unity-2020.3_LTS+-000000?style=flat&logo=unity&logoColor=white)
![C%23](https://img.shields.io/badge/C%23-7.3+-239120?style=flat&logo=csharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-PAUSED-orange)
![License](https://img.shields.io/badge/License-Unlicensed-lightgrey)

An action-packed MOBA/RPG game developed in **Unity** featuring dynamic hero control systems, real-time combat, and AI-driven gameplay mechanics.

---

## ⏸️ Project Status: PAUSED

**This project is currently paused.** Development was halted to focus on another project (**[SUBJECT_04](https://github.com/AndresStw/SUBJECT_04)**). This README is preserved as documentation of the state the project was left in, so it can be resumed without losing context.

### 📌 Pause Summary

| Aspect | Detail |
|--------|--------|
| **Status** | Paused (not abandoned) |
| **Reason** | Focus shifted to SUBJECT_04 |
| **Last Progress** | Core hero control, combat system, AI bots, and progression loop functional |
| **What's Missing** | Multiplayer, final animations, UI polish, map design |
| **To Resume** | See "How to Resume This Project" section below |

> 💡 **Personal note:** Axion Heroes is still a project I want to finish. The pause is temporary and strategic, not a discard. The core gameplay loop already works — what's left is content and polish.

---

## 📋 Table of Contents

- [About the Project](#about-the-project)
- [Features](#features)
- [Core Systems](#core-systems)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Controls](#controls)
- [Technologies](#technologies)
- [Game Mechanics](#game-mechanics)
- [Key Classes](#key-classes)
- [Roadmap](#roadmap)
- [Known Issues](#known-issues)
- [How to Resume This Project](#how-to-resume-this-project)
- [Contact](#contact)
- [License](#license)

---

## 🎯 About the Project

**Axion Heroes** is an action game that combines MOBA (Multiplayer Online Battle Arena) and RPG mechanics. Players control heroes with unique abilities, engage in tactical combat, and progress through experience and leveling systems.

The project was built as a learning ground for:
- Hero controller architecture
- NavMesh-based AI
- Combat systems with cooldowns and damage types
- Mobile input integration
- ScriptableObject-driven data design

---

## ✨ Features

### ✅ Implemented (functional at time of pause)

- **Hero Control System**: Smooth character movement and rotation with NavMesh integration
- **Combat System**: Basic attacks, special abilities, and ultimate skills with cooldown management
- **AI Bots**: Intelligent AI opponents that adapt to player actions
- **Experience & Leveling**: Dynamic progression system with experience points and level scaling
- **Mobile Support**: Joystick input for mobile platforms
- **AFK Detection**: Automatic AI activation when player is inactive
- **Respawn System**: Dynamic respawn timers based on hero level
- **Animation System**: Integrated Animator controller for smooth character animations
- **Enemy Variety**: Combat support for multiple enemy types (Shards, Sentinels, Heroes)

### 🚧 Pending (for when resumed)

- [ ] Multiplayer integration
- [ ] Final hero models and animations (currently placeholders)
- [ ] Map and environment design
- [ ] Item system and upgrades
- [ ] UI overhaul
- [ ] Sound and music implementation
- [ ] Particle effects and visual polish
- [ ] Balance adjustments

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
