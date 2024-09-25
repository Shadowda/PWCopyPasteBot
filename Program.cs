// Create a client.
using Microsoft.Extensions.Configuration;
using PixelPilot.PixelGameClient;
using PixelPilot.PixelGameClient.Messages.Received;
using PixelPilot.PixelGameClient.Messages.Send;
using PixelPilot.PixelGameClient.Players.Basic;
using PixelPilot.PixelGameClient.World;
using PixelPilot.Structures;
using PixelPilot.Structures.Converters.PilotSimple;
using PixelPilot.Structures.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text.Json;
using static System.Reflection.Metadata.BlobBuilder;


internal class Program
{
    static Structure? clipboard;

    static PixelPilotClient client;

    static PixelWorld world;

    public class Config
    {
        public string LoginEmail { get; set; }
        public string LoginPassword { get; set; }
        public string worldId { get; set; }
    }

    private static async Task Main(string[] args)
    {
        string jsonString = File.ReadAllText("config.json");

        // Deserialize the JSON content to the Config class
        Config config = JsonSerializer.Deserialize<Config>(jsonString);

        // Access the values
        Console.WriteLine("LoginEmail: " + config.LoginEmail);
        Console.WriteLine("LoginPassword: " + config.LoginPassword);
        Console.WriteLine("worldId: " + config.worldId);


        client = PixelPilotClient.Builder()
    .SetEmail(config.LoginEmail)
    .SetPassword(config.LoginPassword)
    .SetAutomaticReconnect(true)
    .Build();

        world = new PixelWorld();
        client.OnPacketReceived += world.HandlePacket;

        // Player manager allows you to easily keep track of player stats.
        // For advanced users, it can be extended to include relevant information for you.
        var playerManager = new PlayerManager();
        client.OnPacketReceived += playerManager.HandlePacket;

        // Executed once the client receives INIT
        // Make a platform and do some silly loops.
        client.OnClientConnected += (_) =>
        {
            client.SendChat("Helper Bot Loaded");
        };

        client.OnPacketReceived += (_, packet) =>
        {
            switch (packet)
            {
                case PlayerChatPacket:
                    PlayerChatPacket msgPacket = (PlayerChatPacket)packet;
                    if (msgPacket.Message.StartsWith(".") || msgPacket.Message.StartsWith("!"))
                    {
                        var command = msgPacket.Message.Substring(1).Split(" ");

                        Console.WriteLine(string.Join(",", command));

                        switch (command[0].ToLower())
                        {
                            case "h":
                            case "help":

                                Player? player1 = playerManager.GetPlayer(msgPacket.PlayerId);
                                if (player1 == null)
                                {
                                    break;
                                }


                                string msg = $"{command[1].ToLower()}";
                                switch (command[1].ToLower())
                                {
                                    case "c":
                                    case "copy":
                                        client.SendPm(player1.Username, $"{command[1]} - args: x1 y1 x2 y2");
                                        client.SendPm(player1.Username, $"{command[1]} -  desc: copy top left point to lower right point to clipboard");
                                        break;
                                    case "p":
                                    case "paste":
                                        client.SendPm(player1.Username, $"{command[1]} - args: x y");
                                        client.SendPm(player1.Username, $"{command[1]} -  desc: paste clipboard from top left point. ignores empty blocks");
                                        break;
                                    case "ps":
                                    case "pastes":
                                        client.SendPm(player1.Username, $"{command[1]} - args: x y repeatX repeatY increaseX increaseY increaseIDs ignore0x0");
                                        client.SendPm(player1.Username, $"{command[1]} - desc: for pasting multiple items in a grid, can change switch ids, can skip the first item of the grid.");
                                        client.SendPm(player1.Username, $"{command[1]} - args2: x y repeatX repeatY increaseX increaseY increaseIDs increaseIDs2 ignore0x0");
                                        break;
                                    case "ls":
                                    case "listswitchs":
                                       

                                        break;
                                    case "lp":
                                    case "listportals":
                                        

                                        break;
                                    default:

                                        break;

                                }
                                break;
                            case "c":
                            case "copy":
                                Point p1 = new Point(int.Parse(command[1]), int.Parse(command[2]));
                                Point p2 = new Point(int.Parse(command[3]), int.Parse(command[4]));
                                clipboard = world.GetStructure(p1, p2, copyEmpty: false);
                                break;
                            case "p":
                            case "paste":
                                Point pastePoint = new Point(int.Parse(command[1]), int.Parse(command[2]));

                                if (clipboard != null)
                                {
                                    clipboard.Blocks.PasteInOrder(client, pastePoint, 5);
                                }

                                break;

                            case "ps":
                            case "pastes":

                                Task.Run(() => DoPastes(command));

                                break;
                            case "ls":
                            case "listswitchs":
                                Task.Run(() => DoListSwitchIDs(command));

                                break;
                            case "lp":
                            case "listportals":
                                Task.Run(() => DoListPortals(command));

                                break;
                        }

                        client.SendChat($"Command acknowledged {command[0]}");

                    }

                    return;
                case PlayerJoinPacket join:
                    client.Send(new PlayerChatOutPacket($"/givegod {join.Username}"));
                    break;
            }
        };




        await client.Connect(config.worldId);


        await client.WaitForDisconnect();
    }

    private static void DoPastes(string[] command)
    {
        //!pastes x y repeatX repeatY increaseX increaseY increaseIDs ignore0x0
        //!pastes x y repeatX repeatY increaseX increaseY increaseIDs increaseIDs2 ignore0x0
        Point oPastesPoint = new Point(int.Parse(command[1]), int.Parse(command[2]));

        int px = int.Parse(command[1]);
        int py = int.Parse(command[2]);
        int repeatX = int.Parse(command[3]);
        int repeatY = int.Parse(command[4]);
        int increaseX = int.Parse(command[5]);
        int increaseY = int.Parse(command[6]);
        int increaseIDs = int.Parse(command[7]);
        bool ignore0x0 = false;
        int increaseIDs2 = 0;
        if (command.Length >= 9)
        {
            bool success = int.TryParse(command[8], out increaseIDs2);

            if (! success);
            {
                ignore0x0 = true;
            }

            if (command.Length >= 10)
            {
                ignore0x0 = true;
            }
        }

        if (clipboard != null)
        {
            var json = PilotSaveSerializer.Serialize(clipboard);
            Structure clipboardCopy = PilotSaveSerializer.Deserialize(json);

            for (int y = 0; y < repeatY; y++)
            {
                for (int x = 0; x < repeatX; x++)
                {
                    if (x == 0 && y == 0)
                    {
                        if (ignore0x0)
                        {
                            continue;
                        }
                    }

                    foreach (var block in clipboardCopy.Blocks)
                    {
                        int blockId = block.Block.BlockId;

                        if (blockId == 104 || blockId == 105 || blockId == 101)
                        {
                            PixelPilot.PixelGameClient.World.Blocks.MorphableBlock mb = (PixelPilot.PixelGameClient.World.Blocks.MorphableBlock)block.Block;
                            mb.Morph += increaseIDs;
                        }
                        if (blockId == 102)
                        {
                            PixelPilot.PixelGameClient.World.Blocks.ActivatorBlock mb = (PixelPilot.PixelGameClient.World.Blocks.ActivatorBlock)block.Block;
                            mb.SwitchId += increaseIDs;
                        }
                        if (blockId == 72)
                        {
                            PixelPilot.PixelGameClient.World.Blocks.PortalBlock mb = (PixelPilot.PixelGameClient.World.Blocks.PortalBlock)block.Block;
                            mb.PortalId += increaseIDs;
                            mb.TargetId += increaseIDs2;
                        }

                    }
                    Point newPastesPoint = new Point(oPastesPoint.X + (increaseX * x), oPastesPoint.Y + (increaseY * y));

                    clipboardCopy.Blocks.PasteInOrder(client, newPastesPoint, 5).Wait();

                    Console.WriteLine($"x: {x}, y: {y}");
                }
            }


        }
        //starting at x,y repear x time then y times increaing by 
    }

    private static void DoListSwitchIDs(string[] command)
    {
        var blocks = world.GetBlocks(false);
        HashSet<int> allNumbers = new HashSet<int>(Enumerable.Range(0, 1000));
        foreach (var block in blocks)
        {
            int blockId = block.Block.BlockId;

            // Remove numbers that are present in the list from the HashSet

            if (blockId == 104 || blockId == 105 || blockId == 101)
            {
                PixelPilot.PixelGameClient.World.Blocks.MorphableBlock mb = (PixelPilot.PixelGameClient.World.Blocks.MorphableBlock)block.Block;

                Console.WriteLine($"x: {block.X}, y: {block.Y}, BlockId: {mb.BlockId}, Id: {mb.Morph}");
                allNumbers.Remove(mb.Morph);
            }
        }

        Console.WriteLine("Free Ids");
        foreach (int number in allNumbers)
        {
            Console.WriteLine($"{number}");
        }
    }

    private static void DoListPortals(string[] command)
    {
        var blocks = world.GetBlocks(false);
        HashSet<int> allNumbers = new HashSet<int>(Enumerable.Range(0, 1000));
        foreach (var block in blocks)
        {
            int blockId = block.Block.BlockId;

            // Remove numbers that are present in the list from the HashSet


            if (blockId == 72)
            {
                PixelPilot.PixelGameClient.World.Blocks.PortalBlock mb = (PixelPilot.PixelGameClient.World.Blocks.PortalBlock)block.Block;
                Console.WriteLine($"x: {block.X}, y: {block.Y}, BlockId: {mb.BlockId}, PortalId: {mb.PortalId} TargetId: {mb.TargetId}");
                allNumbers.Remove(mb.PortalId);
                allNumbers.Remove(mb.TargetId);
            }
        }

        Console.WriteLine("Free Ids");
        foreach (int number in allNumbers)
        {
            Console.WriteLine($"{number}");
        }
    }
}