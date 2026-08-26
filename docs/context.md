# Project Context

CPU Temperature Monitoring API is a Windows-first, Linux-supported .NET library dedicated to CPU temperature monitoring.

The architecture keeps public result and orchestration logic in `CpuT.Core`, Windows providers in `CpuT.Windows`, Linux providers in `CpuT.Linux`, and platform composition in `CpuT`.

The provider cache invalidates after three consecutive `Failed` or `Invalid` results. `Valid` resets the counter, `Unavailable` does not increment it, and failed rediscovery starts a short bounded cooldown. Shared discovery is serialized; there is no background polling worker.

Actual kernel-driver, WMI/ACPI, and Linux hwmon access is intentionally not implemented in the initial structure. macOS is unsupported and has no implementation project.
