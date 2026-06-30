# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Portico is an open source HLA (High Level Architecture) Run-Time Infrastructure (RTI)
implementation. The core is Java; C++ federates talk to it through a JNI wrapper that
loads a JVM in-process. Three HLA API versions are supported: HLA 1.3, IEEE 1516-2000,
and IEEE 1516-2010 (Evolved/1516e), each with its own Java and (for 1.3/1516e) C++ binding.

All real source lives under `codebase/`; the repo root is just a wrapper with the README
and Eclipse project files.

## Build system

Build is Ant-based, driven from `codebase/`. Use the bundled Ant, not a system-installed one:

```
cd codebase
./ant <target>        # Linux/Mac
ant.bat <target>      # Windows
```

Key targets (defined in `codebase/build.xml`, extended by `profiles/java.xml` and
`profiles/cpp.xml`):
- `compile` — compile all production code (Java + C++)
- `test` — compile and run the full automated test suite (Java only; C++ has its own
  `cpp.test` extension point)
- `sandbox` (default target) — assemble a runnable sandbox under `build/`
- `installer` — build an installer from the sandbox
- `clean` — remove all build artifacts
- `release` — clean → compile → test → sandbox → installer → javadocs (full CI flow)
- `release.thin` — same as `release` but skips tests

Run a single Java test suite instead of everything:
- `./ant test.portico` — internal/unit tests for the core RTI (`org/portico/...`)
- `./ant test.hla13` — HLA 1.3 conformance suite (`hlaunit/hla13`)
- `./ant test.ieee1516` — IEEE 1516-2000 conformance suite (`hlaunit/ieee1516`)
- `./ant test.ieee1516e` — IEEE 1516e conformance suite (`hlaunit/ieee1516e`)

Tests use TestNG. Useful overrides (pass as `-D`): `test.loglevel`, `test.fileLogLevel`
(default `OFF`), `test.binding` (default `jvm`, can be set to `jgroups` to exercise the
distributed transport).

Local overrides (JDK path, etc.) go in an uncommitted `codebase/local.properties`, which
overrides `codebase/build.properties` (JDK 21 required; version currently 2.2.0).

C++ builds use GCC 11 on Linux and MSVC 14.1/14.2/14.3 on Windows (see `profiles/cpp.xml`
and `system/visualstudio/` for VS project files). Windows installers are built via NSIS
(`system/nsis/`).

## Architecture

### Java core (`codebase/src/java/portico/`)

- `org.portico.lrc` — the **L**ocal **R**un-time **C**ontroller: the core RTI kernel that
  every federate ambassador talks to.
  - `LRC.java`, `LRCState`, `LRCMessageHandler`, `LRCMessageQueue` — message dispatch
  - `management/` — `Federation`/`Federate` lifecycle
  - `model/` — object model metadata (`OCMetadata`, `ACMetadata`, `ICMetadata` for object
    classes, attribute classes, interaction classes)
  - `services/` — one subpackage per HLA service area (federation management, object
    discovery, time management, synchronization points, ownership management, DDM,
    save/restore, MOM). Each service typically splits into `incoming/`/`outgoing/`
    handler subdirectories — incoming messages from the federation, outgoing requests
    from the local federate.
  - `notifications/` — `NotificationManager` + `@NotificationListener` annotation pattern
    used to deliver federate callbacks (discover, reflect, receive interaction, time
    advance granted, etc.)
- `org.portico.impl` — version-specific RTI Ambassador implementations that sit on top of
  the LRC kernel: `impl/hla13/`, `impl/hla1516/`, `impl/hla1516e/` (Java), plus
  `impl/cpp13/`, `impl/cpp1516e/` (the Java-side counterparts of the C++ JNI bridge). Each
  contains its own ambassador class, FOM parser, and type converters — when changing
  behavior for one HLA version, check whether the same fix is needed in the others.
- `org.portico.bindings` — transport layer behind the `IConnection` interface: `jgroups/`
  (real distributed networking) and `jvm/` (single-process, used by most tests).
- `org.portico.container` — lightweight DI/plugin container wiring the kernel together.
- `org.portico.utils` — shared helpers (`logging` (Log4j2), `messaging`, `fom` parsing,
  `bithelpers`, `classpath`, `annotations`).
- `hla.rti*` / `hla.rti1516e` — the standard HLA API interfaces/types themselves
  (implementations of the spec-defined classes, not Portico-specific).

### C++ (`codebase/src/cpp/`)

Two binding trees, `hla13/` and `ieee1516e/`, each laid out the same way:
`include/` (public headers), `src/{jni,platform,services,time,types,utils}` (implementation,
with `jni/` being the bridge into the Java LRC), `test/`, `example/`. The C++ layer is a thin
wrapper — it starts an embedded JVM and forwards calls into the Java `impl/cpp13` /
`impl/cpp1516e` ambassadors.

### Tests (`codebase/src/java/test/`)

- `org/portico/` — white-box tests of internal kernel/util code, run via `test.portico`.
- `hlaunit/{hla13,ieee1516,ieee1516e}/` — black-box HLA conformance suites, organized by
  feature (federation, object, time, synchronization, ownership, DDM, MOM, save/restore),
  one tree per HLA version, run via the matching `test.<version>` target.
- `examples/` — example federate code paired with `codebase/src/cpp/*/example/`.

### .NET bridge (`dotnet/`)

A standalone, Windows-only C++/CLI bridge (`PorticoRti1516e.Native`, `/clr`) that lets
C#/.NET code drive Portico's native IEEE-1516e C++ API directly — independent of the Ant
build, referencing a pre-built Portico Windows distribution's headers/libs. C# projects
consume it via a normal project/assembly reference (e.g. `PorticoRti1516e.Native.TestFederate`).
See `dotnet/README.md` for scope/layout/build instructions.

**All C# projects under `dotnet/` must target .NET Framework 4.8 (`net48`)** — do not
introduce `net6.0`/`net8.0`/.NET Standard targets here. This includes any future C# code
added to this tree, such as a planned HLA data-encoding helper library
(`IDataElement`/`HLAinteger32BE`/etc., mirroring
`codebase/src/java/portico/org/portico/impl/hla1516e/types/encoding/`).

## Verification
After making changes, run the relevant scope before declaring done:
- Java-only change in the kernel: `cd codebase && ./ant test.portico`
- Change touching a specific HLA version's ambassador/binding: run that version's suite,
  e.g. `./ant test.hla13`
- Broad/cross-cutting change: `./ant test` (runs all four Java suites)
- C++ changes additionally need a native build via the platform toolchain referenced in
  `profiles/cpp.xml` (GCC 11 / MSVC); there's no single Ant target that builds and runs C++
  tests cross-platform from this environment.
