# Enemy Progression Mod

## Das Problem

In Satellite Reign skalieren Gegner automatisch mit der Stärke des Spielers. Bessere Ausrüstung und Upgrades führen zu stärkeren Gegnern, sodass sich Fortschritt sinnlos anfühlt — man wird für Investitionen in bessere Waffen/Augmentierungen mit stärkeren Feinden "belohnt".

## Die Lösung

Dieses Mod ersetzt das vanilla Skalierungssystem durch **distrikt-basierte Progression**:

- **Downtown/RedLight (Start)**: Schwache Gegner (0-20%)
- **Industrial**: Leicht stärkere Gegner (15-40%)
- **Grid**: Mittlere Gegner (35-60%)
- **CBD**: Starke Gegner (55-80%)
- **Endgame/Boss**: Stärkste Gegner (75-100%)

Gegner werden **nur** basierend auf dem **Spielwelt-Fortschritt** (welcher Distrikt) stärker — **nicht** basierend auf der Ausrüstung des Spielers. So lohnt es sich tatsächlich, in Waffen, Augmentierungen und Fähigkeiten zu investieren!

## Installation

1. Die kompilierte `EnemyProgressionMod.dll` in den `Mods`-Ordner des Spiels kopieren
2. Spiel starten — beim ersten Start wird automatisch eine `EnemyProgressionConfig.xml` im Plugin-Ordner erstellt

## Hotkeys

| Taste | Funktion |
|-------|----------|
| **F7** | Zeigt aktuellen Progressions-Status (Distrikt, Gegner-Level-Range, Statistiken) |
| **F8** | Lädt die Konfigurationsdatei neu (für Änderungen ohne Neustart) |

## Konfiguration

Die `EnemyProgressionConfig.xml` wird beim ersten Start automatisch erstellt und kann angepasst werden:

```xml
<?xml version="1.0" encoding="utf-8"?>
<EnemyProgressionConfig>
  <Enabled>true</Enabled>
  <UpdateIntervalSeconds>5</UpdateIntervalSeconds>
  <ShowDistrictChangeMessage>true</ShowDistrictChangeMessage>
  <DistrictProgressionOverrides>
    <District Index="0" Name="Downtown"   Min="0"    Max="0.2" />
    <District Index="1" Name="Industrial" Min="0.15" Max="0.4" />
    <District Index="2" Name="Grid"       Min="0.35" Max="0.6" />
    <District Index="3" Name="CBD"        Min="0.55" Max="0.8" />
    <District Index="4" Name="Endgame"    Min="0.75" Max="1" />
  </DistrictProgressionOverrides>
</EnemyProgressionConfig>
```

### Konfigurationsoptionen

| Feld | Beschreibung |
|------|-------------|
| `Enabled` | `true`/`false` — Mod aktivieren/deaktivieren |
| `UpdateIntervalSeconds` | Wie oft (in Sekunden) die Progression aktualisiert wird |
| `ShowDistrictChangeMessage` | Popup beim Distrikt-Wechsel anzeigen |
| `DistrictProgressionOverrides` | Progression pro Distrikt (0.0 = schwächste, 1.0 = stärkste Gegner) |

### Feineinstellung

- **Overlap-Bereiche** (z.B. Industrial 15-40% und Grid 35-60%) sorgen für weichere Übergänge
- **Min/Max enger setzen** = weniger Gegnervariation pro Distrikt
- **Min/Max weiter setzen** = mehr Gegnervariation (näher am Vanilla-Feeling)
- Werte von 0.0-1.0 entsprechen dem internen SpawnCard-Progressionssystem

## Technische Details

Das Mod nutzt das `ISrPlugin`-Interface und greift per Reflection auf das private `m_SpawnDecks`-Feld des `SpawnManager` zu. Es überschreibt die `m_MinProgression`/`m_MaxProgression`-Werte der SpawnCards basierend auf dem aktuellen Distrikt des Spielers (via `ProgressionManager.Get().CurrentDistrict`).

### Wie es funktioniert

1. **Spielstart**: Config wird geladen (oder Default erstellt)
2. **Alle X Sekunden**: Aktueller Distrikt wird abgefragt
3. **Bei Distriktwechsel**: SpawnCards werden angepasst:
   - Jede SpawnCard hat `m_MinProgression` und `m_MaxProgression`
   - Diese werden auf den Bereich des aktuellen Distrikts geclampt
   - Das Spiel wählt dann nur Gegner aus, die in diesen Bereich fallen
4. **Ergebnis**: Gegner richten sich nach dem Spielweltfortschritt, nicht nach Spielerausrüstung

## Kompilierung

Benötigt die Satellite Reign Assembly-DLLs:
- `Assembly-CSharp.dll`
- `Assembly-CSharp-firstpass.dll`
- `UnityEngine.dll`

Diese befinden sich im `SatelliteReignWindows_Data/Managed/`-Ordner des Spiels.
