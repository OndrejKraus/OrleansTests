# StreamInterleavingTest

This project is intended for testing of Fire&Forget SMS stream delivery interleaving inside boundaries of a grain.

Normally this should work like regular grain calls and single stream deliveries should not interleave.

It seems that there is some issue, at least in Orleans 3.0.2 running on .NET Core SDK 3.1.101 under win10-x64. If you run this test in this configuration, you would probably get failed test with similar output (some output is filtered out for clarity):

```
...
Master: Proceed
Master: > OnCmd
Master: ProcessMember:1 delaying
Master: > OnAck
Master: ProcessMember:2 delaying
Master: > OnAck
Master: ProcessMember:3 delaying
Master: ProcessMember:1 seen:1
Master: < OnCmd
Master: ProcessMember:2 seen:2
Master: < OnAck
Master: ProcessMember:3 seen:3
Master: < OnAck
```

While this is expected output:

```
...
Master: Proceed
Master: > OnCmd
Master: ProcessMember:1 delaying
Master: ProcessMember:1 seen:1
Master: < OnCmd
Master: > OnAck
Master: ProcessMember:1 delaying
Master: ProcessMember:1 seen:2
Master: < OnAck
Master: > OnAck
Master: ProcessMember:1 delaying
Master: ProcessMember:1 seen:3
Master: < OnAck
```
