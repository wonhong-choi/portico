# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Portico is an open source HLA (High-Level Architecture) Run-Time Infrastructure (RTI)
implementation, supporting HLA v1.3, IEEE-1516, and IEEE-1516e, for both Java and C++
federates. All the actual project source lives under `codebase/`; the repo root only
holds `README.md`.

## Build System

The build is Ant-based, driven from `codebase/build.xml`, with a bundled Ant distribution
(no need to have Ant installed separately). Always run from `codebase/`:

```
cd codebase
./ant <target>          # linux/mac
ant.bat <target>        # windows
```

Key targets (see `codebase/build.xml`, `codebase/profiles/java.xml`, `codebase/profiles/cpp.xml`):
- `./ant sandbox` (default) — builds Java + native code and assembles a runnable sandbox under `dist/`
- `./ant compile` — compile production code only
- `./ant test` — runs `test.portico`, `test.hla13`, `test.ieee1516`, `test.ieee1516e` (see below)
- `./ant clean` — removes `build/` and `dist/`
- `./ant installer` — builds an installer package from the sandbox
- `./ant release` — clean + test + sandbox + installer

Build/version settings (JDK paths, compiler versions, project version) live in
`codebase/build.properties`. Machine-local overrides that shouldn't be committed go in a
`codebase/local.properties` file (loaded before `build.properties`).

Platform-specific native (C++) build/install profiles live in `codebase/profiles/linux/`
and `codebase/profiles/windows/`, imported conditionally in `build.xml` based on
`platform.linux64` / `platform.windows`.

### Running tests

Tests use TestNG via the `java-test` macro (`codebase/profiles/system.macros.xml`). There
are separate suites per API version, each runnable independently:

```
./ant test.portico      # internal/unit tests of Portico's own classes (src/java/test/org)
./ant test.hla13         # HLA 1.3 spec-compliance suite  (src/java/test/hlaunit/hla13)
./ant test.ieee1516      # IEEE 1516 spec-compliance suite (src/java/test/hlaunit/ieee1516)
./ant test.ieee1516e     # IEEE 1516e spec-compliance suite (src/java/test/hlaunit/ieee1516e)
```

Useful properties (pass with `-D`):
- `-Dtest.binding=jvm|jgroups` — which transport binding the test federation uses (default `jvm`, in-process)
- `-Dtest.groups=<names>` — restrict to specific TestNG groups (default: all)
- `-Dtest.loglevel=<level>` / `-Dtest.fileLogLevel=<level>` — control console/file logging during tests (default `OFF`)

A test class is any class ending in `Test` found under `classdir/<suite>/**`; an optional
`<suite>/TestSetup` class is always included.

## Architecture

Portico is a **Java-based RTI**; the C++ interfaces are native wrappers that embed a JVM
and forward every call over JNI into the same Java engine used by pure-Java federates. The
layering (bottom to top):

1. **LRC — `codebase/src/java/portico/org/portico/lrc/`**
   The Local RTI Component: the version/binding-agnostic core engine. `LRC.java` is the
   main entry point/dispatcher. Subdirectories:
   - `services/` — one package per HLA service area (`federation`, `object`, `pubsub`,
     `ownership`, `time`, `sync`, `ddm`, `saverestore`, `mom`) implementing the actual RTI
     service logic.
   - `model/` — FOM/object-model representation (classes, attributes, interactions) shared
     across all API versions.
   - `compat/`, `management/`, `notifications/`, `utils/` — cross-spec compatibility shims,
     federation/connection management, callback delivery, shared helpers.

2. **Bindings — `codebase/src/java/portico/org/portico/bindings/`**
   Pluggable transport connecting federates to a federation:
   - `jvm/` — in-process binding (federates in the same JVM talk directly); used by default
     in tests (`test.binding=jvm`).
   - `jgroups/` — real network binding built on the JGroups library, for multi-process/
     multi-host federations, including WAN support (`bindings/jgroups/wan/`).

3. **API/spec layer** — two related but distinct trees:
   - `codebase/src/java/portico/hla/{rti,rti13,rti1516,rti1516e}` — the standard-mandated
     public API packages defined by each HLA spec (code federates must be written against;
     treat as fixed "spec surface").
   - `codebase/src/java/portico/org/portico/impl/{hla13,hla1516,hla1516e}` — Portico's
     implementation of those interfaces, translating spec API calls into LRC service calls
     and translating LRC callbacks back into spec-shaped federate callbacks.
   - `impl/cpp13`, `impl/cpp1516e` — the Java-side JNI counterparts used by the native C++
     bindings (matching generated headers like `org_portico_impl_cpp13_*.h`).

4. **C++ native layer — `codebase/src/cpp/{hla13,ieee1516e}`**
   Native RTI Ambassador implementations that embed a JVM via JNI
   (`src/jni/Runtime.cpp` calls `JNI_CreateJavaVM`) and bridge C++ calls into the
   `impl/cpp13`/`impl/cpp1516e` Java classes above. A C++ federate therefore always runs
   the same Java LRC engine underneath.

Request flow for any federate call: spec API (`hla/rtiXX` or C++ headers) → `impl/hlaXX`
(or `impl/cppXX` via JNI) → `lrc/services/*` → `bindings/*` (transport to other federates).

### Tests mirror this layering

`codebase/src/java/test/hlaunit/{hla13,ieee1516,ieee1516e}` contain the same functional
test suite duplicated per API version (matching subpackages like `federation`, `object`,
`ownership`, `saverestore`, `sync`, `time`, `ddm`, `mom`) to verify each spec binding's
compliance. `codebase/src/java/test/org` holds internal unit tests of Portico's own
classes, independent of any HLA spec version.

## Notes

- Java target/source level is 21 (`build.properties`); minimum JDK for building/running is JDK 21.
- Third-party dependencies (log4j, jgroups, testng, asm, cppunit) are vendored under `codebase/lib/`.
- C++ support is Windows (VS2022) and Linux (GCC 11.5, 64-bit only).
- Licensed under CDDL — modifications must be contributed back per the license terms.
