Loads an assembly at runtime into a context of its own.

That context can be dropped again, which is the whole point: a program letting its user
pick one assembly after another would otherwise hold on to every one of them until it
closes.

What the host already carries - the framework itself - is deferred to the host rather
than loaded a second time. A type loaded twice is two different types, and everything
comparing types then quietly stops working: annotations go unrecognised, casts fail for
no visible reason.
