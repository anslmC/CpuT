# CpuT: CPU Temperature Library

A lightweight, framework-independent .NET library for accessing CPU temperature readings.

CpuT provides temperature readings on demand through a small API. It does not perform polling, scheduling, logging, alerts, or background monitoring.

Windows is the primary target, Linux is supported, and macOS is currently out of scope.

**An unavailable temperature is better than a false temperature.**

## What This Library Solves

CPU temperature access varies across platforms, hardware, drivers, and firmware. CpuT provides a focused API that handles:

* Available temperature sources
* Reading validation
* Provider fallback
* Clear unavailable or failure results

> **Give me the current CPU temperature, if a reliable reading is available.**

## Why CpuT?

Broad hardware libraries such as LibreHardwareMonitor provide complete hardware telemetry. CpuT focuses specifically on CPU temperature access.

CpuT provides a small, focused API with provider fallback and validation, without requiring applications to implement platform-specific CPU temperature logic themselves.

## Current Status

The repository includes the Core API, provider discovery and caching, Windows and Linux providers, tests, and a sample.

* **Linux:** CPU-related `hwmon` sensors through known drivers and a metadata-filtered fallback.
* **Windows:** CPU-identified WMI/ACPI thermal zones when exposed by firmware.
* The kernel-driver provider remains unsupported because CpuT does not bundle or install a driver.

## Scope

CpuT provides CPU temperature readings only. It does not include:

* Polling or background monitoring
* Scheduling, logging, or alerts
* GUI or persistence
* General hardware telemetry
* GPU, memory, storage, or other system metrics
* macOS support

## Disposal and Lifecycle

`CpuT` takes ownership of providers passed to its constructor and disposes disposable providers when the `CpuT` instance is disposed.

Do not call `Dispose()` from inside a provider read, as disposal waits for active reads and can deadlock.

`Dispose()` waits for in-flight reads and has no built-in timeout. A provider that never returns can block disposal indefinitely.
