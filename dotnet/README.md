# PorticoRti1516e.Native (C#/.NET bridge, Phase A + B + C + D + E1 + E2)

A C++/CLI bridge over Portico's native IEEE-1516e C++ API
(`codebase/src/cpp/ieee1516e/`), so C#/.NET code can drive a Portico
federate without hand-writing P/Invoke against the STL/exception-heavy
native API.

This is a **standalone Visual Studio solution, independent of the Ant
build under `codebase/`**. `PorticoRti1516e.Native` references a pre-built
Portico Windows distribution's headers/libs directly; C# projects consume
it via a normal project/assembly reference to `PorticoRti1516e.Native` -
nothing here changes or depends on the Ant pipeline.

Windows-only (C++/CLI requires `/clr`, and Portico's C++ binding only ships
prebuilt libraries for vc14_3/VS2022 on Windows). **This code has not been
built or run** - there is no Windows/MSVC/Portico-distribution toolchain
available in the environment it was written in. Treat it as a careful,
header-verified first draft; build and exercise it on a real Windows + VS2022
machine before relying on it.

## Layout

- `PorticoRti1516e.sln`
- `PorticoRti1516e.Native/` - the C++/CLI bridge (`/clr`, x64 only).
  - `Handles/` - managed wrappers for the opaque `*Handle` value types.
  - `Types/` - `wstring` <-> `String^` marshaling.
  - `Time/` - `ManagedHLAfloat64Time`/`ManagedHLAfloat64Interval`, thin
    `double`-backed wrappers around the only concrete `LogicalTime`/
    `LogicalTimeInterval` implementation `ExampleCPPFederate.cpp` uses.
  - `Exceptions/` - `PorticoRtiException` base + concrete subclasses for the
    native exceptions Phase A-D-E1-E2 actually throw, plus the
    `RTI_CATCH_AND_RETHROW` macro used at every native call site.
  - `FederateAmbassador/` - `IManagedFederateAmbassador` (the interface a C#
    federate implements) and `NativeFederateAmbassadorBridge`, the native
    `NullFederateAmbassador` subclass that forwards callbacks into it via
    `gcroot`.
  - `Ambassador/ManagedRTIambassador` - the main entry point.
- `PorticoRti1516e.Native.TestFederate/` - a C# console app mirroring the
  full Phase A+B+C+D subset of `ExampleCPPFederate::runFederate()`'s call
  sequence, for manual verification once built. Phase E1 (ownership
  management/message retraction) and Phase E2 (federation save/restore) are
  not exercised by this test federate - `ExampleCPPFederate.cpp`'s own
  `runFederate()` doesn't use those services either, so there's no
  reference call sequence to port.

## Scope: Phase A + B + C + D + E1 + E2

Connect/disconnect, create/destroy/join/resign federation execution,
synchronization points, handle lookups, and `evoke(Multiple)Callbacks`
(Phase A), plus no-timestamp object pub/sub/register/update/delete -
`PublishObjectClassAttributes`/`SubscribeObjectClassAttributes`,
`RegisterObjectInstance`, `UpdateAttributeValues`, `DeleteObjectInstance`,
and the matching `DiscoverObjectInstance`/`ReflectAttributeValues`/
`RemoveObjectInstance` callbacks (Phase B), plus no-timestamp interaction
pub/sub/send - `PublishInteractionClass`/`SubscribeInteractionClass`/
`SendInteraction`, and the matching `ReceiveInteraction` callback (Phase C),
plus time management - `EnableTimeRegulation`/`DisableTimeRegulation`,
`EnableTimeConstrained`/`DisableTimeConstrained`, `TimeAdvanceRequest`, the
timestamped overloads of `UpdateAttributeValues`/`SendInteraction`/
`DeleteObjectInstance` (each returning a `ManagedMessageRetractionHandle`),
and the matching `TimeRegulationEnabled`/`TimeConstrainedEnabled`/
`TimeAdvanceGrant` callbacks plus the timestamped (no retraction handle)
overloads of `ReflectAttributeValues`/`ReceiveInteraction`/
`RemoveObjectInstance` (Phase D), plus ownership management -
`UnconditionalAttributeOwnershipDivestiture`/
`NegotiatedAttributeOwnershipDivestiture`/`ConfirmDivestiture`/
`AttributeOwnershipAcquisition`/`AttributeOwnershipAcquisitionIfAvailable`/
`AttributeOwnershipReleaseDenied`/`AttributeOwnershipDivestitureIfWanted`/
`CancelNegotiatedAttributeOwnershipDivestiture`/
`CancelAttributeOwnershipAcquisition`/`QueryAttributeOwnership`/
`IsAttributeOwnedByFederate`, the matching 9 ownership callbacks
(`RequestAttributeOwnershipAssumption`, `RequestDivestitureConfirmation`,
`AttributeOwnershipAcquisitionNotification`, `AttributeOwnershipUnavailable`,
`RequestAttributeOwnershipRelease`,
`ConfirmAttributeOwnershipAcquisitionCancellation`,
`InformAttributeOwnership`, `AttributeIsNotOwned`, `AttributeIsOwnedByRTI`),
plus message retraction - `Retract` and the `RequestRetraction` callback,
plus the retraction-handle overloads of `ReflectAttributeValues`/
`ReceiveInteraction`/`RemoveObjectInstance` (Phase E1), plus federation
save/restore - `RequestFederationSave` (no-timestamp and timestamped
overloads), `FederateSaveBegun`/`FederateSaveComplete`/
`FederateSaveNotComplete`, `AbortFederationSave`/`QueryFederationSaveStatus`,
`RequestFederationRestore`,
`FederateRestoreComplete`/`FederateRestoreNotComplete`,
`AbortFederationRestore`/`QueryFederationRestoreStatus`, and the matching
11 callbacks (`InitiateFederateSave` (2 overloads), `FederationSaved`,
`FederationNotSaved`, `FederationSaveStatusResponse`,
`RequestFederationRestoreSucceeded`, `RequestFederationRestoreFailed`,
`FederationRestoreBegun`, `InitiateFederateRestore`, `FederationRestored`,
`FederationNotRestored`, `FederationRestoreStatusResponse`) (Phase E2).
Phase E1/E2's API surface is unverified beyond signature-matching against
`RTI/RTIambassador.h`/`RTI/FederateAmbassador.h`, since
`ExampleCPPFederate.cpp` doesn't exercise ownership management, message
retraction, or save/restore.
**Not yet implemented**: DDM/regions and MOM (Phase E3-E4). Calling
anything outside Phase A/B/C/D/E1/E2 means using
`NativeFederateAmbassadorBridge`'s inherited `NullFederateAmbassador`
no-ops for any callback not listed above, and there is currently no
managed surface on `ManagedRTIambassador` for those service areas at all.

Only `CallbackModel::HLA_EVOKED` is supported - callbacks are delivered
solely when `EvokeCallback`/`EvokeMultipleCallbacks` is called, so they
never arrive on an arbitrary JVM thread.

## Building

1. Build or obtain a Portico Windows distribution (headers + `lib/vc14_3` +
   `bin/vc14_3` + `jre/`), e.g. via `cd codebase && ant sandbox` or an
   installer.
2. Point `PorticoRti1516e.Native` at it: set a `PorticoHome` MSBuild
   property (e.g. `/p:PorticoHome=C:\path\to\distribution`), a
   `PORTICO_HOME` environment variable, or create an untracked
   `PorticoRti1516e.Native/PorticoRti1516e.Native.props` file that sets it.
3. Open `PorticoRti1516e.sln` in Visual Studio 2022 and build (x64).
4. At runtime, the test federate's process needs `bin\vc14_3` (for
   `rti1516e64.dll`/`fedtime1516e64.dll`) and `jre\bin\server` (for
   `jvm.dll`) on `PATH`, same as
   `codebase/src/cpp/ieee1516e/example/win64-vc14_3.bat`. It also needs
   `codebase/src/cpp/ieee1516e/example/testfom.fed` (the same FOM
   `ExampleCPPFederate.cpp` loads) copied next to the test federate's
   executable, since `CreateFederationExecution` passes that file name to
   the RTI.
