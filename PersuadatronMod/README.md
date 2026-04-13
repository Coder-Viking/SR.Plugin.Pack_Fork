# Persuadatron Mod - Syndicate Wars Style

A comprehensive Satellite Reign mod inspired by Syndicate Wars, adding a Persuadatron device, cybernetic implant system, new weapons, and follower AI.

## Features

### 1. Persuadatron (Portable Persuasion Device)
A gear item that converts NPCs into loyal followers. Requires a **Neural Cortex** brain implant to function.

| Persuadatron Level | Brain Implant Required | Valid Targets |
|---|---|---|
| Mk1 | Neural Cortex Mk1 | Civilians only |
| Mk2 | Neural Cortex Mk2 | + Police & light units (PowerLevel ≤ 25%) |
| Mk3 | Neural Cortex Mk3 | All units up to PowerLevel ≤ 75% (no mechs/strongest) |

### 2. Cybernetic Implant System (4 Groups × 3 Tiers)

#### Legs (AugmentationLegs)
| Tier | Sprint Speed | Cost |
|---|---|---|
| Mk1 | +15% | 500 |
| Mk2 | +30% | 1000 |
| Mk3 | +50% | 1500 |

#### Arms (AugmentationArms)
| Tier | Accuracy Bonus | Carry Capacity | Cost |
|---|---|---|---|
| Mk1 | +5% | +15% | 600 |
| Mk2 | +10% | +30% | 1200 |
| Mk3 | +18% | +50% | 1800 |

#### Body (AugmentationBody)
| Tier | HP Bonus | Damage Resist | HP Regen | Cost |
|---|---|---|---|---|
| Mk1 | +20% | +5% | - | 700 |
| Mk2 | +40% | +15% | - | 1400 |
| Mk3 | +60% | +25% | 2 HP/s | 2100 |

#### Brain (AugmentationHead)
| Tier | Persuadatron Level | Abilities | Cost |
|---|---|---|---|
| Mk1 | Level 1 | - | 800 |
| Mk2 | Level 2 | Hack Target boost | 1600 |
| Mk3 | Level 3 | + World Scan | 2400 |

### 3. Syndicate Wars Weapons

| Weapon | Type | Range | Damage | Special |
|---|---|---|---|---|
| Uzi | SMG/Pistol | 12 | 8-14 | High fire rate, 60 ammo |
| Minigun | Heavy | 18 | 12-20 | 200 ammo, 1.5s spin-up |
| Pumpgun | Shotgun | 8 | 5-10 ×8 pellets | High knockback |
| Railgun | Sniper | 40 | 80-120 | 25% crit, 2s charge |
| Flamethrower | Heavy | 10 | 6-12 | 2m area, beam |
| Gauss Gun | Rifle | 20 | 15-25 | 15 EMP damage |
| Laser Rifle | Rifle | 25 | 10-18 | Beam, high accuracy |

### 4. Follower AI

Persuaded units follow the Persuadatron carrier and behave according to priority:

1. **Has weapon** → Automatically fires at hostile enemies in range
2. **No weapon, weapon on ground** → Moves to pick up the nearest dropped weapon
3. **No weapon, none available** → Passively follows the carrier

**Configuration:**
- Max followers: 8 (configurable)
- Follow distance: 4 units
- Sprint catch-up: 15 units
- Combat range: 20 units
- Weapon pickup range: 10 units

## Hotkeys

| Key | Action |
|---|---|
| P | Persuade nearest valid target |
| F5 | Show mod status |
| F6 | Reload configuration |
| L | List all current followers |

## Configuration

The mod creates a `PersuadatronConfig.xml` file in the plugin directory on first run. Edit this file to customize:

- Persuasion range, cooldown, duration
- Maximum followers
- PowerLevel thresholds per Persuadatron tier
- Follower AI distances and update intervals
- Implant stat values per tier
- Item ID ranges (to avoid conflicts)

## Installation

1. Copy `PersuadatronMod.dll` to your Satellite Reign mods folder
2. Copy the `Data/` folder alongside the DLL
3. Launch the game - the mod will auto-initialize

## Technical Notes

- Uses the ISrPlugin interface for mod loading
- Item registration via reflection on ItemManager
- Entity detection via Physics.OverlapSphere
- Follower movement via entity movement system (with transform fallback)
- HP regen via periodic SetHealthValue calls
- All values configurable via XML
- PowerLevel estimation based on entity max HP (50 HP = 0%, 500 HP = 100%)
