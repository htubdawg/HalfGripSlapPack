# Proposed Open World weather API

The current SlapPack adapter reads `Sky.ready`, eased precipitation, `Storms.SnowShare`, wind and the world-height offset through reflection. The inspected Open World source has no public weather snapshot API. Keep that adapter isolated until an upstream public contract is available.

A useful public, versioned contract would expose a `TryGetLocalWeather(out WeatherSnapshot snapshot)` method for the current player. A snapshot should include:

| Value | Meaning |
| --- | --- |
| Ready/available result | False before initialization, outside an active Open World race, or after unload. |
| Rain intensity | Final eased local rain intensity, 0–1, after cutoff and rain/snow splitting. |
| Snow intensity | Final eased local snow intensity, 0–1; rain + snow no greater than 1. |
| World wind velocity | Explicit world-space vector in Unity units/second, with direction convention documented. |

The publisher should apply floating-origin height offsets itself. Intensities should describe weather at the car, independently of particle render-budget cuts; cloud coverage and road wetness are separate quantities. Polling should be inexpensive and not allocate every frame. Return false safely during scene transitions.

SlapPack should prefer this public API when available, keep Open World optional, and fall back cleanly if the version is unsupported. Test dry/rain/snow/sleet, load order, origin shifts, scene transitions and graphics-budget changes. A contract should be agreed with the Open World maintainer before changing their mod; this document is a proposal, not a published API or a sent request.
