using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HysteriaAuth.Agent.Models;

namespace HysteriaAuth.Agent.Services;

/// <summary>
/// 系统监控模块 — 采集 CPU、内存、网络等系统指标。
/// Linux: 读取 /proc/stat, /proc/meminfo, /proc/net/dev
/// Windows: 使用 PerformanceCounter 或模拟数据（开发环境）
/// </summary>
public class SystemMonitor
{
    private readonly AgentConfig _config;
    private readonly ILogger<SystemMonitor> _logger;
    private readonly bool _isLinux;

    // 上一次网络采样值
    private NetworkSample? _previousNetworkSample;
    private DateTime _previousNetworkTime;

    public SystemMonitor(AgentConfig config, ILogger<SystemMonitor> logger)
    {
        _config = config;
        _logger = logger;
        _isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    /// <summary>
    /// 采集所有系统指标。CPU 采集使用两次采样差值计算，耗时约 1 秒。
    /// </summary>
    public async Task<SystemMetrics> CollectAsync()
    {
        var cpuTask = GetCpuUsageAsync();

        // 并行采集内存和网络（不依赖 CPU 的 sleep 等待）
        var memory = GetMemoryInfo();
        var (networkInBytes, networkOutBytes, networkInMbps, networkOutMbps) = GetNetworkInfo();

        var cpuUsage = await cpuTask;

        return new SystemMetrics
        {
            CpuUsagePercent = (float)Math.Round(cpuUsage, 2),
            MemoryUsagePercent = (float)Math.Round(memory.UsagePercent, 2),
            MemoryUsedMb = memory.UsedMb,
            MemoryTotalMb = memory.TotalMb,
            NetworkInBytes = networkInBytes,
            NetworkOutBytes = networkOutBytes,
            NetworkInMbps = (float)Math.Round(networkInMbps, 2),
            NetworkOutMbps = (float)Math.Round(networkOutMbps, 2),
            ActiveConnections = GetActiveConnections(),
            CollectedAt = DateTime.UtcNow
        };
    }

    // ============================
    // CPU
    // ============================

    private async Task<double> GetCpuUsageAsync()
    {
        if (_isLinux)
            return await GetLinuxCpuUsageAsync();
        return await GetWindowsCpuUsageAsync();
    }

    private async Task<double> GetLinuxCpuUsageAsync()
    {
        var stat1 = ReadLinuxProcStat();
        await Task.Delay(1000); // 两次采样间隔
        var stat2 = ReadLinuxProcStat();

        var totalDiff = stat2.Total - stat1.Total;
        var idleDiff = stat2.Idle - stat1.Idle;

        if (totalDiff <= 0) return 0;
        return (1.0 - (double)idleDiff / totalDiff) * 100;
    }

    private async Task<double> GetWindowsCpuUsageAsync()
    {
        // Windows 开发环境使用 Process.GetCurrentProcess 的 CPU 时间近似
        var startTime = DateTime.UtcNow;
        var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
        await Task.Delay(1000);
        var endTime = DateTime.UtcNow;
        var endCpu = Process.GetCurrentProcess().TotalProcessorTime;

        var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
        var elapsedMs = (endTime - startTime).TotalMilliseconds;
        var cpuPercent = cpuUsedMs / (elapsedMs * Environment.ProcessorCount) * 100;

        return Math.Min(cpuPercent, 100);
    }

    private CpuSample ReadLinuxProcStat()
    {
        try
        {
            var line = File.ReadLines("/proc/stat").First();
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // cpu  user nice system idle iowait irq softirq steal guest guest_nice
            if (parts.Length < 8) return new CpuSample();

            return new CpuSample
            {
                User = ulong.Parse(parts[1]),
                Nice = ulong.Parse(parts[2]),
                System = ulong.Parse(parts[3]),
                Idle = ulong.Parse(parts[4]),
                Iowait = ulong.Parse(parts[5]),
                Irq = ulong.Parse(parts[6]),
                Softirq = ulong.Parse(parts[7]),
                Steal = parts.Length > 8 ? ulong.Parse(parts[8]) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 /proc/stat 失败");
            return new CpuSample();
        }
    }

    private struct CpuSample
    {
        public ulong User, Nice, System, Idle, Iowait, Irq, Softirq, Steal;
        public ulong Total => User + Nice + System + Idle + Iowait + Irq + Softirq + Steal;
    }

    // ============================
    // 内存
    // ============================

    private (double UsagePercent, float UsedMb, float TotalMb) GetMemoryInfo()
    {
        if (_isLinux)
            return GetLinuxMemoryInfo();
        return GetWindowsMemoryInfo();
    }

    private (double, float, float) GetLinuxMemoryInfo()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            var memInfo = new Dictionary<string, long>();

            foreach (var line in lines)
            {
                var parts = line.Split(':', 2);
                if (parts.Length < 2) continue;
                var key = parts[0].Trim();
                var valueStr = Regex.Replace(parts[1], @"\s*kB$", "").Trim();
                if (long.TryParse(valueStr, out var val))
                    memInfo[key] = val;
            }

            var total = memInfo.GetValueOrDefault("MemTotal", 0);
            var available = memInfo.GetValueOrDefault("MemAvailable", 0);
            var used = total - available;
            var usagePercent = total > 0 ? (double)used / total * 100 : 0;

            return (usagePercent, (float)used / 1024, (float)total / 1024);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 /proc/meminfo 失败");
            return (0, 0, 0);
        }
    }

    private (double, float, float) GetWindowsMemoryInfo()
    {
        try
        {
            // 使用 GC 内存信息作为近似
            var totalBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var totalMb = (float)totalBytes / (1024 * 1024);
            var usedBytes = Process.GetCurrentProcess().WorkingSet64;
            var usedMb = (float)usedBytes / (1024 * 1024);

            // Windows 下无法通过简单方式获取全系统内存，返回进程内存近似值
            return (totalMb > 0 ? usedMb / totalMb * 100 : 0, usedMb, totalMb);
        }
        catch
        {
            return (0, 0, 0);
        }
    }

    // ============================
    // 网络
    // ============================

    private (long InBytes, long OutBytes, float InMbps, float OutMbps) GetNetworkInfo()
    {
        if (_isLinux)
            return GetLinuxNetworkInfo();
        return GetWindowsNetworkInfo();
    }

    private (long, long, float, float) GetLinuxNetworkInfo()
    {
        try
        {
            var lines = File.ReadAllLines("/proc/net/dev");
            var configuredInterfaces = _config.Monitor.NetworkInterfaces
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            long totalRx = 0, totalTx = 0;

            foreach (var line in lines.Skip(2)) // 跳过标题行
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 10) continue;

                var iface = parts[0].TrimEnd(':');
                if (!configuredInterfaces.Contains(iface) && iface != "eth0" && iface != "ens3" && iface != "enp0s3")
                    continue; // 仅统计配置的接口

                if (long.TryParse(parts[1], out var rx))
                    totalRx += rx;
                if (long.TryParse(parts[9], out var tx))
                    totalTx += tx;
            }

            // 计算速率
            var now = DateTime.UtcNow;
            float inMbps = 0, outMbps = 0;

            if (_previousNetworkSample != null)
            {
                var secondsDiff = (now - _previousNetworkTime).TotalSeconds;
                if (secondsDiff > 0)
                {
                    var bytesInDiff = totalRx - _previousNetworkSample.Value.InBytes;
                    var bytesOutDiff = totalTx - _previousNetworkSample.Value.OutBytes;
                    inMbps = (float)(bytesInDiff * 8 / secondsDiff / 1_000_000);
                    outMbps = (float)(bytesOutDiff * 8 / secondsDiff / 1_000_000);
                }
            }

            _previousNetworkSample = new NetworkSample { InBytes = totalRx, OutBytes = totalTx };
            _previousNetworkTime = now;

            return (totalRx, totalTx, Math.Max(0, inMbps), Math.Max(0, outMbps));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 /proc/net/dev 失败");
            return (0, 0, 0, 0);
        }
    }

    private (long, long, float, float) GetWindowsNetworkInfo()
    {
        // Windows 开发环境返回模拟数据
        return (0, 0, 0, 0);
    }

    private struct NetworkSample
    {
        public long InBytes;
        public long OutBytes;
    }

    // ============================
    // 活跃连接数
    // ============================

    private int GetActiveConnections()
    {
        // Phase 2 简化：返回 0。Phase 3 将从 Hysteria /online API 获取。
        return 0;
    }
}
