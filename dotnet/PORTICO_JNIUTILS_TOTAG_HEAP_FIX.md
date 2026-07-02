# Fix: cross-heap free of `userSuppliedTag` in ieee1516e `JniUtils::toTag`

> **Handoff document.** This is a self-contained bug report + fix, intended for a
> session that has push access to the upstream repository to raise a Pull Request.
> Everything needed to locate, apply, rebuild, and verify the fix is contained here —
> no other context is required.

---

## 1. PR target metadata

| Field | Value |
|-------|-------|
| Upstream repo | `openlvc/portico` (https://github.com/openlvc/portico) |
| Base branch | `maintenance-2.2.x` |
| New branch | create from `maintenance-2.2.x`, e.g. `fix/jniutils-totag-heap-corruption` |
| Suggested PR title | `Fix cross-heap free of userSuppliedTag in ieee1516e JniUtils::toTag` |
| File changed | `codebase/src/cpp/ieee1516e/src/jni/JniUtils.cpp` (one function) |

> Do **not** rebase onto `master`/`main` — the fix must go on `maintenance-2.2.x`
> as requested.

---

## 2. Symptom

An IEEE-1516e C++ federate (native, or via a C++/CLI wrapper) crashes with a Windows
heap-corruption exception during the **receive path**:

- Exception code: **`0xC0000374`** (`STATUS_HEAP_CORRUPTION`), faulting module
  `ntdll.dll`.
- Triggered inside `reflectAttributeValues` / `receiveInteraction` callbacks while the
  evoke/callback pump is running.
- The crash is **native** — it bypasses any managed (`/clr`) exception handlers, so a
  C#/WPF host just dies silently with no MessageBox.
- Under a **debug CRT** (`/MDd`, `librti1516e64d.dll`) it halts immediately on heap
  validation; under a **release CRT** (`/MD`, `librti1516e64.dll`) it is intermittent —
  the process sometimes runs past the corrupted metadata and delivers the callback,
  which is why "just continue" in the debugger sometimes appears to work.

Reproduces with **two separate native processes** on one machine (one publisher, one
subscriber). It is therefore **not** a C#/CLI-bridge bug and **not** a shared-data race
between federates — it is a per-process, deterministic memory-management defect in
Portico's native JNI layer, hit on every callback that carries a non-null
`userSuppliedTag`.

---

## 3. Affected code

**File:** `codebase/src/cpp/ieee1516e/src/jni/JniUtils.cpp`
**Function:** `VariableLengthData JniUtils::toTag( JNIEnv *jnienv, jbyteArray jtag )`
**Location on `maintenance-2.2.x`:** lines **1023–1035**.

`toTag()` converts the `userSuppliedTag` JNI `byte[]` into a `VariableLengthData`. It is
invoked on **every** `reflectAttributeValues`, `receiveInteraction`, and
`removeObjectInstance` callback — exactly the receive path in the symptom above.

### Buggy code (verbatim)

```cpp
VariableLengthData JniUtils::toTag( JNIEnv *jnienv, jbyteArray jtag )
{
	// if we don't have a tag, just return an empty byte[]
	if( jtag == NULL )
		return VariableLengthData();

	// convert the tag
	//   we assume that there is no null terminator
	jsize size = jnienv->GetArrayLength( jtag );
	jbyte *buffer = new jbyte[size];
	jnienv->GetByteArrayElements( jtag, NULL );
	VariableLengthData data( (void*)buffer, size );
	jnienv->ReleaseByteArrayElements( jtag, buffer, JNI_ABORT );
	return data;
}
```

### Three defects in these four lines

1. **Wrong-heap allocation.** `jbyte *buffer = new jbyte[size];` allocates a block on
   the **RTI DLL's** C++ heap (the module that owns `JniUtils.cpp`).

2. **Discarded JNI pointer + uninitialized data.**
   `jnienv->GetByteArrayElements( jtag, NULL );` is called for its side effect but its
   **return value is thrown away**. Consequences:
   - The actual tag bytes are **never read** — `buffer` (from `new jbyte[size]`) holds
     uninitialized garbage, which `VariableLengthData` then copies. So even when it does
     not crash, the received tag is wrong (a latent correctness bug).
   - The JVM's critical/array pointer obtained by `GetByteArrayElements` is **leaked**
     (never released with the pointer the JVM actually handed back).

3. **Cross-heap free → corruption.**
   `jnienv->ReleaseByteArrayElements( jtag, buffer, JNI_ABORT )` passes `buffer` — the
   **RTI-DLL `new[]` pointer** — as the `elems` argument. `ReleaseByteArrayElements`
   requires the *exact pointer that `GetByteArrayElements` returned*. Instead the JVM is
   handed a foreign pointer and attempts to reconcile/free it against **its own**
   allocator/heap. Freeing an RTI-DLL `new[]` block through the JVM's heap manager
   corrupts heap metadata → `0xC0000374`.

---

## 4. The fix (verbatim)

Mirror the correct value-map pattern: capture the pointer `GetByteArrayElements`
returns, build the `VariableLengthData` from **that** pointer, release **that same**
pointer, and drop the stray `new jbyte[size]`.

```cpp
VariableLengthData JniUtils::toTag( JNIEnv *jnienv, jbyteArray jtag )
{
	// if we don't have a tag, just return an empty byte[]
	if( jtag == NULL )
		return VariableLengthData();

	// convert the tag
	//   we assume that there is no null terminator
	jsize size = jnienv->GetArrayLength( jtag );
	jbyte *buffer = jnienv->GetByteArrayElements( jtag, NULL );
	VariableLengthData data( (void*)buffer, size );
	jnienv->ReleaseByteArrayElements( jtag, buffer, JNI_ABORT );
	return data;
}
```

The only changed lines are:

```diff
-	jbyte *buffer = new jbyte[size];
-	jnienv->GetByteArrayElements( jtag, NULL );
+	jbyte *buffer = jnienv->GetByteArrayElements( jtag, NULL );
```

This removes the wrong-heap allocation, reads the real tag bytes, and — because
`GetByteArrayElements` and `ReleaseByteArrayElements` now receive the same pointer —
eliminates the cross-heap free. It also fixes the latent correctness bug: the received
`userSuppliedTag` now holds the correct bytes instead of uninitialized memory.

---

## 5. Why the fix is correct

- **`VariableLengthData` copies and self-owns its input.** Its `(void*, size)`
  constructor takes an internal copy of the bytes; it does not retain the caller's
  pointer. So releasing the JNI array (`ReleaseByteArrayElements` with `JNI_ABORT`)
  immediately after constructing `data` is safe — `data` already owns its own copy.
  `JNI_ABORT` is the right release mode here: we only read the array, so no changes
  need to be copied back.

- **This matches the sibling converter that is already correct.**
  In the same file, `JniUtils::toAttributeValueMap(...)` (correct byte-array handling at
  lines **285–292** on `maintenance-2.2.x`) uses exactly this pattern:

  ```cpp
  jbyteArray jarray = (jbyteArray)jnienv->GetObjectArrayElement( values, i );
  jbyte *byteArrayContents = jnienv->GetByteArrayElements( jarray, NULL );
  jsize jarraySize = jnienv->GetArrayLength( jarray );
  VariableLengthData data( (void*)byteArrayContents, jarraySize );
  jnienv->ReleaseByteArrayElements( jarray, byteArrayContents, JNI_ABORT );
  ```

  `toParameterValueMap(...)` does the same. The fix simply makes `toTag()` consistent
  with these — it was the odd one out.

- **Historical note.** The Portico v2.0.1 changelog includes *"Corrected indirection for
  processing VariableLengthData objects."* That fix corrected the **value-map** path
  (attributes/parameters) but did **not** touch this `toTag()` helper, so the same class
  of bug survived here for the `userSuppliedTag`.

---

## 6. Reproduction & verification

### Reproduce (before the fix)
1. Build/obtain a Portico ieee1516e distribution from `maintenance-2.2.x`, preferably a
   **debug** build (`librti1516e64d.dll`) so heap validation is strict.
2. Run two federates joining the same federation:
   - a **publisher** that registers an object / sends an interaction **with a non-null
     `userSuppliedTag`**, and
   - a **subscriber** that subscribes and pumps callbacks (`evokeMultipleCallbacks`).
3. On the first `reflectAttributeValues` / `receiveInteraction` delivered to the
   subscriber, the debug CRT halts with `0xC0000374` (release CRT: intermittent crash).

### Verify (after the fix)
1. Apply the one-function change and rebuild the native ieee1516e library on
   Windows/MSVC:
   ```
   cd codebase
   ant.bat compile        # (or the ieee1516e C++ build target)
   ```
   This regenerates `librti1516e64.dll` / `librti1516e64d.dll` (+ `.lib`). Deploy the
   rebuilt DLL/lib into the distribution the federates consume (`bin\vc14_3`,
   `lib\vc14_3`).
2. Re-run the two federates. Expected:
   - No `0xC0000374` — the debug build no longer halts on heap validation during
     reflect/receive.
   - The received `userSuppliedTag` now contains the **correct** bytes the publisher
     sent (previously uninitialized garbage).
3. Sanity-check that nothing else regresses by running the Java ieee1516e conformance
   suite (does not require the native rebuild):
   ```
   cd codebase
   ./ant test.ieee1516e
   ```

### Rebuild note
The change is C++ in the native JNI layer, so it needs a Windows/MSVC (vc14_3 / VS2022)
or Linux/GCC native rebuild of the ieee1516e binding — there is no cross-platform Ant
target that builds and runs the C++ tests. A pure Java `./ant test.ieee1516e` run only
exercises the Java core and will not cover the native `toTag` path directly, but is a
useful non-regression check.
