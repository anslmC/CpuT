# CpuT: CPU Temperature API

A lightweight, framework-independent .NET library for accessing reliable CPU temperature readings.

CpuT provides temperature data **on demand**. It does not perform polling, background monitoring, scheduling, logging, or alerts. The consuming application decides when and how often to request a reading.

Windows is the primary target, Linux is supported, and macOS is currently out of scope.

**An unavailable temperature is better than a false temperature.**

## What This Solves

Accessing CPU temperature can vary across hardware, operating systems, drivers, and firmware. Available sensors may also be unsupported, inaccessible, or unrelated to the CPU.

CpuT handles the platform-specific complexity by:

* Finding available CPU temperature sources.
* Validating temperature readings.
* Falling back when a provider fails.
* Returning clear status information when a reliable reading is unavailable.

The goal is simple:

> **Give me the current CPU temperature, if a reliable reading is available.**

## Why CpuT?

Broad hardware libraries such as LibreHardwareMonitor are excellent when an application needs complete hardware telemetry.

CpuT is different: it focuses only on the problem of reliably retrieving CPU temperature.

| CpuT                       | Broad hardware libraries                                 |
| -------------------------- | -------------------------------------------------------- |
| Focused on CPU temperature | Covers many hardware metrics                             |
| Small API surface          | Broad API surface                                        |
| Built-in provider fallback | Often exposes many sensors for the application to select |
| CPU-specific validation    | General sensor access                                    |
| Explicit result statuses   | Behavior varies                                          |

CpuT does not replace broader hardware libraries. It is intended for applications that only need a focused, reusable CPU temperature API.

## Current Status

The repository includes the Core API, provider discovery and caching, Windows and Linux providers, composition project, tests, and a sample.

* **Linux:** Reads CPU-related `hwmon` sensors through known drivers and a metadata-filtered fallback.
* **Windows:** Reads CPU-identified WMI/ACPI thermal zones when firmware exposes them.
* The kernel-driver provider remains unsupported because CpuT does not bundle or install a driver.

## Baseline

* Target framework: `.NET 10` (`net10.0`)
* Development environment verified: .NET SDK `10.0.303` on Windows 11

## Scope

CpuT provides CPU temperature readings only.

It does **not** include:

* Polling or background monitoring
* Scheduling, logging, or alerts
* GUI or persistence
* General hardware telemetry
* GPU, memory, storage, or other system metrics
* macOS support

Polling and scheduling are intentionally handled by the consuming application.

## Disposal and Lifecycle

`CpuT` takes ownership of providers passed to its constructor and disposes disposable providers when the `CpuT` instance is disposed. Do not reuse those provider instances elsewhere.

Do not call `Dispose()` from inside a provider's `TryRead()` or `TryReadAsync()` method, as disposal waits for active reads and can deadlock.

`Dispose()` waits for in-flight reads to finish and has no built-in timeout. If a provider read hangs indefinitely, disposal can also block indefinitely.
