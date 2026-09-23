# MDD4All.DME.AssemblyLoading

Loads a data model assembly for MDD4All.DME, the object graph editor application.

The assembly is loaded into a context of its own, so it can be dropped again when another data model is picked. Everything the shared framework already provides is deferred to the host instead - two copies of the same type are two different types, and annotations stop being recognised.
