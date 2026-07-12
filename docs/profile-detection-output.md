# Profile Detection Output Contract

The profile detector produces engineering evidence, not a conformance certificate. Subscriber UI and reports should present the same compact contract.

## Required summary

```text
Profile      : <display name or unknown>
Confidence   : Unknown | Possible | Likely | Confirmed | Conflict
Score        : <evaluated-match percentage>
Evidence     : <matched>/<evaluated weight>
Conflicts    : <count>
Observation  : <frame count and duration>
```

## Evidence detail

Each evaluated field contains:

- field name,
- outcome: match, conflict, or unknown,
- expected value,
- observed value,
- engineering explanation,
- evaluation weight.

Unknown evidence is not counted as a match or conflict. Missing observations must not improve confidence.

## UI behavior

- Keep capture and decoding active for all confidence states.
- Show one compact state beside the selected stream.
- Put detailed evidence behind an expandable panel or report section.
- Use conflict state only when evaluated facts contradict a definition.
- Use unknown when there is insufficient evaluated evidence.
- Do not present a confirmed engineering classification as certification.

## Configuration comparison

Display configuration-versus-wire findings separately from profile classification:

```text
Configured match : exact | warnings | errors
Mode             : strict | compatible
```

Strict mode is suitable for validation and live-transmission preflight. Compatible mode is suitable for troubleshooting unfamiliar or partially documented traffic.
