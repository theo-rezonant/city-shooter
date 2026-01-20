# Audio Mixer Configuration

## Required Groups Structure

```
Master (Root)
├── SFX
│   ├── Weapons
│   └── Footsteps
├── Ambience
│   ├── Wind
│   └── CityHum
└── UI
```

## Exposed Parameters

Expose these parameters for script access:

| Parameter Name | Group | Purpose |
|---------------|-------|---------|
| MasterVolume | Master | Overall game volume |
| SFXVolume | SFX | Combat and interaction sounds |
| AmbienceVolume | Ambience | Environmental loops |
| UIVolume | UI | Interface feedback |

## How to Expose Parameters

1. Select a group in the mixer
2. Right-click on the Volume slider
3. Select 'Expose [GroupName] (of Volume)'
4. In the 'Exposed Parameters' section, rename it to match the table above

## Ducking Setup

To create ambient ducking when weapons fire:

1. Create a new Snapshot (right-click in mixer > Add Snapshot)
2. Name it 'Ducking'
3. In the Ducking snapshot, lower the Ambience group volume by -10dB
4. Set transition time to 0.1 seconds

## Attenuation Units

Use logarithmic attenuation:
- Master: 0dB to -80dB range
- SFX: 0dB to -80dB range
- Ambience: -6dB to -80dB range (slightly quieter by default)
- UI: 0dB to -80dB range

## Effect Recommendations

### SFX Group
- Add a Limiter effect to prevent clipping during intense combat
- Settings: Threshold -1dB, Release 10ms

### Ambience Group
- Add a Low Pass Filter (optional) for indoor/outdoor transitions
- Default cutoff: 22000Hz (full range)

### Master Group
- Add a compressor for consistent loudness
- Settings: Threshold -12dB, Ratio 4:1, Attack 10ms, Release 100ms
