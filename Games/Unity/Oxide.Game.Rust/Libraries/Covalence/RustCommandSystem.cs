﻿using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Rust.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class RustCommandSystem : ICommandSystem
    {
        // Default constructor
        public RustCommandSystem()
        {
            Initialize();
        }

        // A reference to Rust's internal command dictionary
        private IDictionary<string, ConsoleSystem.Command> rustCommands;

        // Chat command handler
        private ChatCommandHandler chatCommandHandler;

        // All registered chat commands
        private IDictionary<string, CommandCallback> registeredChatCommands;

        // The console player
        private RustConsolePlayer consolePlayer;

        // The covalence provider
        private readonly RustCovalenceProvider rustCovalence = RustCovalenceProvider.Instance;

        /// <summary>
        /// Initializes the command system provider
        /// </summary>
        private void Initialize()
        {
            rustCommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary<string, ConsoleSystem.Command>;
            registeredChatCommands = new Dictionary<string, CommandCallback>();
            chatCommandHandler = new ChatCommandHandler(ChatCommandCallback, registeredChatCommands.ContainsKey);
            consolePlayer = new RustConsolePlayer();
        }

        private bool ChatCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            CommandCallback callback;
            return registeredChatCommands.TryGetValue(cmd, out callback) && callback(caller, cmd, args);
        }

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandCallback callback)
        {
            // Initialize if needed
            if (rustCommands == null) Initialize();

            // Convert to lowercase
            var commandName = cmd.ToLowerInvariant();

            // Register the command as a console command
            // Check if it already exists
            if (rustCommands != null && rustCommands.ContainsKey(commandName))
                throw new CommandAlreadyExistsException(commandName);

            // Register it
            var splitName = commandName.Split('.');
            rustCommands?.Add(commandName, new ConsoleSystem.Command
            {
                name = splitName.Length >= 2 ? splitName[1] : splitName[0],
                parent = splitName.Length >= 2 ? splitName[0] : "global",
                namefull = commandName,
                isCommand = true,
                isUser = true,
                isAdmin = true,
                GetString = () => string.Empty,
                SetString = s => { },
                Call = arg =>
                {
                    if (arg == null) return;

                    if (arg.connection != null)
                    {
                        if (arg.Player())
                        {
                            var livePlayer = rustCovalence.PlayerManager.GetOnlinePlayer(arg.connection.userid.ToString()) as RustLivePlayer;
                            if (livePlayer == null) return;
                            livePlayer.LastCommand = CommandType.Console;
                            livePlayer.LastArg = arg;
                            callback(livePlayer?.BasePlayer, commandName, ExtractArgs(arg));
                            return;
                        }
                    }
                    callback(consolePlayer, commandName, ExtractArgs(arg));
                }
            });

            // Register the command as a chat command
            registeredChatCommands.Add(commandName, callback);
        }

        private static string[] ExtractArgs(ConsoleSystem.Arg arg)
        {
            if (arg == null) return new string[0];
            var argsList = new List<string>();
            var i = 0;
            while (arg.HasArgs(++i)) argsList.Add(arg.GetString(i - 1));
            return argsList.ToArray();
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        public void UnregisterCommand(string cmd)
        {
            // Initialize if needed
            if (rustCommands == null) Initialize();

            // Remove the console command
            rustCommands?.Remove(cmd);

            // Remove the chat command
            registeredChatCommands.Remove(cmd);
        }

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool HandleChatMessage(ILivePlayer player, string str) => chatCommandHandler.HandleChatMessage(player, str);
    }
}
