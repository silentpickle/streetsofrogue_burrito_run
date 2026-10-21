# Portable native decision example

> **What this is:** One real NPC decision captured from the game, small enough to read and replay offline.  
> **Read it if:** You want a concrete example of the decision data, or to try the replay tool.  
> **Status:** Current as of October 20, 2026.

[npc-choice-v2.json](npc-choice-v2.json) is an unchanged exported native decision
from October 20, 2026, session `1ad63b80`, build `0.6.0+20261020-200850`,
capture sequence 635. It contains detached policy input/expected output only;
no profile, save, player account, absolute path or game binary is included.

Both candidates are two world units away. With Banana preference enabled,
Fud scores 5−2=3 and Banana scores 8−2=6, so the policy requests Banana pickup.
This input/output example does not itself establish the later native transfer.
The enclosing local native run passed its independent outcome reader.

With the installation paths in [GamePaths.props](../../GamePaths.props) configured, replay it from
the repository root:

```sh
dotnet run --project tests/DeliveryRoute.Tests -c Release -- --npc-replay docs/examples/npc-choice-v2.json
```

The command evaluates the captured input and compares the expected output. It
does not launch the game or replay native movement. See [methods](../reference/methods.md)
and [setup](../getting-started.md) for contracts and dependencies.
