# v0.1 validation

Automated JavaScript DOM/VM integration executed against the actual generated receiver HTML, actual existing tablet controller/core scripts, deterministic fake PeerJS transport and simulated native VTube Studio WebSocket API.

Passed:

- Public API request envelope, response ID/type matching, first token acquisition, authentication reuse and auth-denied suppression.
- Two tablet sources connected concurrently, face/body region ownership, packet decoding and head/closed-eye/mouth/body-angle mapping.
- Duplicate source swipe sequence IDs routed independently; success acknowledged only after VTS response.
- Face expiry while body continues does not inject neutral face values.
- One in-flight injection request despite repeated ticks; bounded pending requests.
- Unexpected VTS disconnect and reconnect with saved token; explicit disconnect cleanup.
- Config validation rejects unknown source/duplicate targets; pagehide cleans timers.
- JavaScript syntax of all embedded scripts in receiver/mobile/tablet checked with Node.

Not tested: real VTube Studio binary or real Live2D model, desktop browser local network policies, mobile camera hardware, live PeerJS NAT traversal, end-to-end sustained performance. Phone camera page is a reused compatible sender; the new VTS transport is tested with protocol simulation, not a physical phone. No claim of live VTS visual correctness is made.
