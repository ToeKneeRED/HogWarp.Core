using HogWarp.Replicated;
using HogWarpSdk.Game;
using HogWarpSdk.Systems;
using System.Numerics;

namespace HogWarp.Chat
{
    public class Plugin : HogWarpSdk.IPlugin
    {
        private Logger log = new Logger("HogWarpChat");
        public event Action<Player, string>? OnChatMessage;
        public HogWarpSdk.Game.Timer.TickDelegate OnPlayerJoinDelegate;
        private float sayDist = 400;
        private float shoutDist = 1500;
        private float whisperDist = 100;
        private BP_HogWarpChat chatActor;
        public bool chatMsgOverride = false;
        private Dictionary<string, Action<Player, string>> commands = new Dictionary<string, Action<Player, string>>();

        public Plugin()
        {
        }

        public string Author => "HogWarp Team";

        public string Name => "HogWarpChat";

        public Version Version => new(1, 0);

        public void PostLoad()
        {
            HogWarpSdk.Server.PlayerSystem.PlayerJoinEvent += Chat_PlayerJoinEvent;
            HogWarpSdk.Server.PlayerSystem.PlayerLeftEvent += Chat_PlayerLeftEvent;
            OnChatMessage += Chat_OnChatMessage;

            chatActor = HogWarpSdk.Server.World.Spawn<BP_HogWarpChat>()!;
            chatActor.Plugin = this;
            
            commands.Add("/me", SlashMe);
            commands.Add("/house", SlashHouse);
            commands.Add("/say", SlashDistMsg);
            commands.Add("/shout", SlashDistMsg);
            commands.Add("/whisper", SlashDistMsg);
        }

        public void Shutdown()
        {
        }

        enum House
        {
            Gryffindor,
            Hufflepuff,
            Ravenclaw,
            Slytherin,
            Unaffiliated
        }

        public void AddCommand(string command, Action<Player, string> action)
        {
            if (!commands.TryAdd(command, action))
            {
                log.Warn($"{command} command already exists!");
            }
        }

        public void ReceiveMessage(Player player, string msg)
        {
            OnChatMessage?.Invoke(player, msg);
        }

        public void SendMessage(Player player, ulong senderId, string msg)
        {
            if (chatActor != null)
            {
                chatActor.RecieveMsg(player, (int)senderId, msg);
            }
        }

        private void SlashMe(Player player, string msg)
        {
            foreach (var p in HogWarpSdk.Server.PlayerSystem.Players)
            {
                SendMessage(p, player.Id, "<Server>" + player.Username + msg.Substring(3) + "</>");
            }
        }

        private void SlashHouse(Player player, string msg)
        {
            foreach (var p in HogWarpSdk.Server.PlayerSystem.Players.Where(p => p.House == player.House))
            {
                SendMessage(p, player.Id, "<img id=\"" + (House)player.House + "\"/><" + (House)player.House + ">" + player.Username + ": " + msg.Substring(7) + "</>");
            }
        }

        private void SlashDistMsg(Player player, string msg)
        {
            float msgDist = 0;
            int msgSub = 0;
            string msgType = "says";

            if (msg.StartsWith("/say")) { msgDist = sayDist; msgSub = 5; msgType = "says"; }
            else if (msg.StartsWith("/shout")) { msgDist = shoutDist; msgSub = 7; msgType = "shouts"; }
            else { msgDist = whisperDist; msgSub = 9; msgType = "whispers"; }

            foreach (var p in HogWarpSdk.Server.PlayerSystem.Players)
            {
                Vector3 plyPos = new Vector3(player.Position.X, player.Position.Y, player.Position.Z);
                Vector3 pPos = new Vector3(p.Position.X, p.Position.Y, p.Position.Z);
                var dist = plyPos - pPos;

                if (dist.Length() <= msgDist)
                {
                    SendMessage(p, player.Id, player.Username + " " + msgType + ": " + msg.Substring(msgSub));
                }
            }
        }

        private void BuildMessage(Player player, string msg)
        {
            log.Info(player.Username + ": " + msg);

            if (commands.ContainsKey(msg.Split(" ")[0]))
            {
                commands[msg.Split(" ")[0]].DynamicInvoke(player, msg);
            }
            else
            {
                foreach (var p in HogWarpSdk.Server.PlayerSystem.Players)
                {
                    SendMessage(p, player.Id, "<img id=\"" + (House)player.House + "\"/><" + (House)player.House + ">" + player.Username + ": </>" + msg);
                }
            }
        }
        private void Chat_OnChatMessage(Player sender, string msg)
        {
            if (!chatMsgOverride)
            {
                BuildMessage(sender, msg);

                foreach (var otherPlayer in HogWarpSdk.Server.PlayerSystem.Players.Where(otherPlayer => otherPlayer != sender))
                {
                    // Notify other clients of the overhead message update
                    chatActor.SetOverheadText(otherPlayer, (int)sender.Id, msg);
                }
            }
        }

        private void Chat_PlayerJoinEvent(Player joiningPlayer)
        {
            log.Info($"{joiningPlayer.Username} joined the server");

            foreach (var otherPlayer in HogWarpSdk.Server.PlayerSystem.Players)
            {
                if (otherPlayer == joiningPlayer)
                {
                    // Create overhead widgets for clients already online for joining player
                    foreach (var player in HogWarpSdk.Server.PlayerSystem.Players.Where(player => player != joiningPlayer))
                    {
                        chatActor.CreateOverheadWidget(joiningPlayer, (int)player.Id);
                    }
                }
                else
                {
                    chatActor.OnPlayerJoinEvent(otherPlayer, (int)joiningPlayer.Id);

                    // Notify other clients to create overhead widget for joining player
                    chatActor.CreateOverheadWidget(otherPlayer, (int)joiningPlayer.Id);
                }
            }
        }

        private void Chat_PlayerLeftEvent(Player player)
        {
            log.Info($"{player.Username} left the server");

            foreach (var p in HogWarpSdk.Server.PlayerSystem.Players)
            {
                chatActor.OnPlayerLeftEvent(p, (int)player.Id);
            }
        }
    }
}

namespace HogWarp.Replicated
{
    public partial class BP_HogWarpChat
    {
        internal Chat.Plugin? Plugin { get; set; }
        private Logger log = new Logger("BP_HogWarpChat");
        public partial void SendMsg(Player player, string Message)
        {
            Plugin!.ReceiveMessage(player, Message);
        }

        public partial void ServerErrorMessage(Player player, string Message)
        {
            log.Error($"<{player.Username}>: {Message}");
        }

        public partial void ServerInfoMessage(Player player, string Message)
        {
            log.Info($"<{player.Username}>: {Message}");
        }

        public partial void ServerWarnMessage(Player player, string Message)
        {
            log.Warn($"<{player.Username}>: {Message}");
        }
    }
}
