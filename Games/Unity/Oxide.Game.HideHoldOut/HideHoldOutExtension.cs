﻿using System;
using System.Linq;

using UnityEngine;

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Game.HideHoldOut.Libraries;

namespace Oxide.Game.HideHoldOut
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class HideHoldOutExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "HideHoldOut";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[]
        {
            "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine"
        };
        public override string[] WhitelistNamespaces => new[]
        {
            "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "UnityEngine"
        };

        public static string[] Filter =
        {
            NetworkController.NetManager_.get_GAME_VERSION,
            "## The timer between the saves in the database has just STARTED",
            "CONNECTION GRANTED",
            "EAC RegisterUser: userID",
            "Ending the session of",
            "LICENCE APPROVED",
            "Periodic_Actualizer Started",
            "Player wants to connect. Waiting for approval",
            "Resulting playerInfos",
            "SESSION WAITING FOR AUTANTICATION",
            "Searching for a player",
            "Starting Server List Checker",
            "Token added to ConnectionTokens:",
            "UserAuthenticated:",
            "WARNING: The servers had left the Master Server list",
            "b - ServerManager - DataBase_Storing",
            "k_EBeginAuthSessionResultOK",
            "k_EUserHasLicenseResultHasLicense",
            "tmp_pos modified by raycast"
        };

        /// <summary>
        /// Initializes a new instance of the HideHoldOutExtension class
        /// </summary>
        /// <param name="manager"></param>
        public HideHoldOutExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new HideHoldOutPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Command", new Command());
            Manager.RegisterLibrary("H2o", new Libraries.HideHoldOut());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="directory"></param>
        public override void LoadPluginWatchers(string directory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;

            Application.logMessageReceived += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;
        }

        internal static void ServerConsole()
        {
            if (Interface.Oxide.ServerConsole == null) return;

            var netManager = NetworkController.NetManager_;

            Interface.Oxide.ServerConsole.Title = () => $"{uLink.Network.connections.Length} | {netManager.ServManager.Server_NAME}";
            Interface.Oxide.ServerConsole.Status1Left = () => $" {netManager.ServManager.Server_NAME}";
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };
            Interface.Oxide.ServerConsole.Status2Left = () => $" {uLink.Network.connections.Length}/{netManager.ServManager.NumberOfSlot} players";
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                if (uLink.NetworkTime.serverTime <= 0) return "0b/s in, 0b/s out";
                double bytesSent = 0;
                double bytesReceived = 0;
                foreach (var connection in uLink.Network.connections)
                {
                    var stats = connection.statistics;
                    if (stats == null) continue;
                    bytesSent += stats.bytesSentPerSecond;
                    bytesReceived += stats.bytesReceivedPerSecond;
                }
                return string.Concat(Utility.FormatBytes(bytesReceived), "/s in, ", Utility.FormatBytes(bytesSent), "/s out");
            };
            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                var time = DateTime.Today.Add(TimeSpan.FromSeconds(netManager.TimeManager.TIME_display)).ToString("h:mm tt").ToLower();
                return $" {time}, Unknown map";
            };
            Interface.Oxide.ServerConsole.Status3Right = () => $"Oxide {OxideMod.Version} for {netManager.get_GAME_VERSION}";
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        private void ServerConsoleOnInput(string input)
        {
            if (!string.IsNullOrEmpty(input)) Interface.Call("OnServerCommand", input);
        }

        private static void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
