# Publisher Session Engine

ARSVIN separates the publisher workflow into explicit session plans so each mode has a predictable stop condition.

## Manual Continue

Manual mode is intentionally open-ended.

- It publishes continuously until the operator presses **Stop**.
- Manual table edits are applied to the next frames while the session is running.
- The legacy global duration field is not used to stop manual publishing.

## Ramp

Ramp mode is time-bound by the configured ramp states.

- Each ramp state contributes its own duration.
- The session duration is the sum of enabled ramp-state durations.
- The selected channel or channel group is interpolated from `From` to `To` inside each state.
- The session completes automatically after the last ramp state.

## Sequencer

Sequencer mode is driven by the configured state durations.

- Each sequencer state contributes its own duration.
- By default, the session completes after one full sequence cycle.
- When sequence looping is enabled, the state timing repeats continuously until the operator presses **Stop**.

## COMTRADE Replay

COMTRADE replay follows the selected publisher source configuration.

- Non-looped COMTRADE replay stops when the file reaches its last sample.
- Looped COMTRADE replay continues until the enclosing session or operator stop condition ends it.

## Multi-publisher behavior

The selected publisher follows the active manual / ramp / sequencer workspace. Other enabled publisher slots use their saved frozen channel values unless their own source is COMTRADE replay.
