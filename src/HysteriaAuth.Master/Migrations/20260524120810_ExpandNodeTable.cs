using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HysteriaAuth.Master.Migrations
{
    /// <inheritdoc />
    public partial class ExpandNodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdleCount",
                table: "Sessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BandwidthDown",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BandwidthUp",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BrutalTxBandwidth",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfigUpdatedAt",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConfigVersion",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CongestionControl",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainName",
                table: "Nodes",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnablePortHopping",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSpeedTest",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreClientBandwidth",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListenAddress",
                table: "Nodes",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ListenPort",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasqueradeFile",
                table: "Nodes",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasqueradeProxyUrl",
                table: "Nodes",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MasqueradeReplyBps",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasqueradeStringContent",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasqueradeStringHeaders",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MasqueradeStringStatusCode",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasqueradeType",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObfsPassword",
                table: "Nodes",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObfsType",
                table: "Nodes",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PortHopRangeEnd",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PortHopRangeStart",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuicInitConnectionReceiveWindow",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuicInitStreamReceiveWindow",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuicMaxConnectionReceiveWindow",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuicMaxIdleTimeout",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuicMaxStreamReceiveWindow",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuicMaxUdpPayloadSize",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolverResolveConcurrency",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolverResolveInterval",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolverTcpAddr",
                table: "Nodes",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolverTlsAddr",
                table: "Nodes",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolverType",
                table: "Nodes",
                type: "TEXT",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolverUdpAddr",
                table: "Nodes",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServerCost",
                table: "Nodes",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SniffEnabled",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SniffRespectHttps",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SniffTimeout",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SpeedTestDownloadSize",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpeedTestPingInterval",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SpeedTestUploadSize",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UdpIdleTimeout",
                table: "Nodes",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdleCount",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "BandwidthDown",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "BandwidthUp",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "BrutalTxBandwidth",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ConfigUpdatedAt",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ConfigVersion",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "CongestionControl",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "DomainName",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "EnablePortHopping",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "EnableSpeedTest",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "IgnoreClientBandwidth",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ListenAddress",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ListenPort",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeFile",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeProxyUrl",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeReplyBps",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeStringContent",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeStringHeaders",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeStringStatusCode",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MasqueradeType",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ObfsPassword",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ObfsType",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "PortHopRangeEnd",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "PortHopRangeStart",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicInitConnectionReceiveWindow",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicInitStreamReceiveWindow",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicMaxConnectionReceiveWindow",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicMaxIdleTimeout",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicMaxStreamReceiveWindow",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "QuicMaxUdpPayloadSize",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverResolveConcurrency",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverResolveInterval",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverTcpAddr",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverTlsAddr",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverType",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ResolverUdpAddr",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ServerCost",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SniffEnabled",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SniffRespectHttps",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SniffTimeout",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SpeedTestDownloadSize",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SpeedTestPingInterval",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "SpeedTestUploadSize",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "UdpIdleTimeout",
                table: "Nodes");
        }
    }
}
