# Project Context

CpuT is a focused CPU temperature library for Windows and Linux. It provides on-demand CPU temperature readings through a provider-based architecture with fallback and validation.

## Architecture

The public API and result semantics live in `CpuT.Core`. Platform-specific providers are implemented in `CpuT.Windows` (for WMI/ACPI thermal zones) and `CpuT.Linux` (for hwmon sensors), with composition logic in `CpuT`.

## Provider Behavior

The library discovers and caches temperature providers. When a provider fails or returns an invalid reading, the library may attempt to use an alternate provider. Provider rediscovery is serialized and does not run in the background—readings only occur when explicitly requested by the consumer.

## Current Scope

* **Windows:** CPU-identified WMI/ACPI thermal zones when exposed by firmware
* **Linux:** CPU-related `hwmon` sensors through known drivers and a metadata-filtered fallback
* **macOS:** Unsupported and has no implementation

The library does not bundle or install drivers, perform background polling, or offer continuous monitoring. An unavailable or unsupported result does not represent a failure; it indicates that the environment cannot currently provide a usable reading.
