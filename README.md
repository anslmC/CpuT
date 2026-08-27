# CpuT: CPU Temperature Monitoring API

A lightweight, framework-independent .NET library focused on honest CPU temperature monitoring.

Windows is the primary target and Linux is supported from the initial architecture. macOS is explicitly out of scope. An unavailable temperature is preferable to a false temperature; no provider returns placeholder readings.

## Current status

The repository contains the Core API, provider discovery/cache structure, Windows and Linux provider boundaries, composition project, tests, and sample. Linux can read CPU-related hwmon sensors through known drivers and a metadata-filtered generic fallback. Windows can read CPU-identified WMI/ACPI thermal zones when firmware exposes them; the kernel-driver provider remains unsupported because CpuT does not bundle or install a driver.

## Baseline

- Target framework: `.NET 10` (`net10.0`), the current LTS baseline used for this initial structure.
- Development environment verified: .NET SDK `10.0.303` on Windows 11.
- Minimum supported Windows and Linux versions will be documented when the native provider implementations are introduced.

## Scope

The API is limited to CPU temperature monitoring. It does not include a GUI, persistence, general hardware monitoring, macOS support, or a provider/plugin registration system.
