# DellProMax16PlusMB16250 (0JT8H7): SOF SoundWire headset jack output is right-channel-only (mono/duplicated); kernel loads ARL-S cs42l43 topology on intel-mtl

## Affected package

`linux` (Ubuntu kernel / SOF SoundWire)

## Hardware / Software

- Model string: `DellInc.-DellProMax16PlusMB16250--0JT8H7` (from `/proc/asound/cards`)
- OS: Ubuntu 24.04.4 LTS (Noble)
- Kernel: `6.17.0-20-generic`
- Audio: `sof-soundwire` (card 1), cs42l43 SoundWire codec

## Problem description

Internal speakers work, but 3.5mm headset/headphone jack playback is broken: only the "Front Right" channel is audible and it is duplicated to both earpieces (effectively mono). "Front Left" is not audible. Jack detect reports inserted and enabled. Codec tone generator tests suggest the left headphone output path is not functioning while the right output drives both sides.

## Steps to reproduce

1. Plug wired headphones/headset into the 3.5mm jack.
2. Stop PipeWire/WirePlumber so ALSA can access the device:
   ```bash
   killall -9 wireplumber pipewire-pulse pipewire
   ```
3. Run stereo test on the Jack Out PCM:
   ```bash
   speaker-test -D hw:1,0 -c 2 -t sine
   ```

## Expected result

- "Front Left" plays in left ear, "Front Right" plays in right ear (stereo).

## Actual result

- Only "Front Right" is audible, and it is heard in both earpieces (mono/duplicated).
- Speakers work normally:
  ```bash
  speaker-test -D hw:1,2 -c 2 -t sine
  ```

## Additional notes (codec routing sanity check)

- Jack detect shows inserted/enabled (`Headphone Jack` = on, `Headphone Switch` = on).
- Codec tone generator routing:
  - Setting only the **Right** headphone input to "Tone Generator 1" produces sound (heard in both earpieces).
  - Setting only the **Left** headphone input to "Tone Generator 1" produces no sound.

## Relevant kernel log excerpts

From `journalctl -k`:

```
snd_hda_intel 0000:80:1f.3: SoundWire enabled on CannonLake+ platform, using SOF driver
sof-audio-pci-intel-mtl 0000:80:1f.3: Firmware paths/files for ipc type 1:
sof-audio-pci-intel-mtl 0000:80:1f.3:  Firmware file:     intel/sof-ipc4/arl-s/sof-arl-s.ri
sof-audio-pci-intel-mtl 0000:80:1f.3:  Firmware lib path: intel/sof-ipc4-lib/arl-s
sof-audio-pci-intel-mtl 0000:80:1f.3:  Topology file:     intel/sof-ace-tplg/sof-arl-cs42l43-l0-cs35l56-l23.tplg
sof-audio-pci-intel-mtl 0000:80:1f.3: Using function topologies instead intel/sof-ace-tplg/sof-arl-cs42l43-l0-cs35l56-l23.tplg
sof-audio-pci-intel-mtl 0000:80:1f.3: loading topology 0: intel/sof-ace-tplg/sof-sdca-jack-id0.tplg
sof-audio-pci-intel-mtl 0000:80:1f.3: loading topology 1: intel/sof-ace-tplg/sof-sdca-mic-id4.tplg
sof-audio-pci-intel-mtl 0000:80:1f.3: loading topology 2: intel/sof-ace-tplg/sof-sdca-2amp-id2.tplg
cs42l43-codec cs42l43-codec: ASoC: POST_PMD: cs42l43 HP event failed: -110
```

## Suspicion / request

The system is using `sof-audio-pci-intel-mtl` but loads **ARL-S** firmware/topology (`arl-s` and `sof-arl-...tplg`). This may be a topology selection / platform matching issue that breaks headset jack channel routing (right-only / mono) and triggers `cs42l43 HP event failed: -110`.

---

**Report Date:** 2026-04-13
**Reported By:** firemandave6024
**Status:** Open
