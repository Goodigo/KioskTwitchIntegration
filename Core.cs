using MelonLoader;
using HarmonyLib;
using Unity.VisualScripting;
using static Customer;
using System;
using System.Net.Sockets;
using System.IO;
using static UnityEngine.UI.GridLayoutGroup;
using TMPro;
using UnityEngine;
using JetBrains.Annotations;
using System.Reflection;
using UnityEngine.Rendering;
using System.Xml.Linq;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.UISystemProfilerApi;
using System.Collections.Generic;
using System.Linq;
using static GameManager;

[assembly: MelonInfo(typeof(Kiosk_Twitch_Integration.Core), "Kiosk Twitch Integration", "1.0.1", "Goodigo", null)]
[assembly: MelonGame("ViviGames", "Kiosk")]
[assembly: MelonAuthorColor(0, 241, 207, 71)]
namespace Kiosk_Twitch_Integration;

public struct TwitchChatter
{
    public TwitchChatter(string n, string c)
    {
        name = n;
        color = c;
    }
    public string name;
    public string color;
}

public struct TwitchOrder
{
    public TwitchOrder(List<OrdedItemNew> i, TwitchChatter c, float t)
    {
        items = i;
        chatter = c;
        timestamp = t;
    }
    public List<OrdedItemNew> items;
    public TwitchChatter chatter;
    public float timestamp;
}

public struct TwitchMessage
{
    public TwitchMessage(string m, TwitchChatter c)
    {
        content = m;
        chatter = c;
    }
    public string content;
    public TwitchChatter chatter;
}

public class Core : MelonMod
{

    public static Dictionary<string, int> OrderLookup = new();
    public static Dictionary<string, int> AddonsLookup = new();

    public static Dictionary<int, int> OrderIDToOrderIndex = new();

    public static Dictionary<string, int> SkinsLookup = new();

    public static Queue<TwitchOrder> queue = new();
    public static TwitchOrder currentOrder;

    private static MelonPreferences_Category twitchSettings;
    private static MelonPreferences_Entry<string> twitchChannel;
    private static MelonPreferences_Entry<string> botName;
    private static MelonPreferences_Entry<string> botAuth;
    private static MelonPreferences_Entry<int> chatterCooldown;
    private static MelonPreferences_Entry<int> chatterMax;
    private static MelonPreferences_Entry<int> addonsMax;
    private static MelonPreferences_Entry<string> rewardID;
    private static MelonPreferences_Entry<bool> nameInWorld;
    private static MelonPreferences_Entry<bool> showChat;
    private static MelonPreferences_Entry<int> bellRingsMax;
    private static MelonPreferences_Entry<float> skipPenalty;

    private static MelonPreferences_Category chatterSkins;


    public static GameObject messageTextObject;

    public static int bellsLeft = 0;
    public static float bellLastRungAt = 0;
    public static int bellsRung = 0;


    TcpClient Twitch;
    StreamReader Reader;
    StreamWriter Writer;
    const string URL = "irc.chat.twitch.tv";
    const int PORT = 6667;
    private float PingCounter = 0;
    public override void OnInitializeMelon()
    {
        //translate main dish strings to IDs
        OrderLookup.Add("burger", 0);
        OrderLookup.Add("hotdog", 1);
        OrderLookup.Add("coffee", 7);
        OrderLookup.Add("soda", 16);
        OrderLookup.Add("beer", 17);
        OrderLookup.Add("pancakes", 37);
        OrderLookup.Add("eggs & sausages", 38);
        OrderLookup.Add("fries", 32);
        OrderLookup.Add("onion rings", 33);
        OrderLookup.Add("nuggets", 34);
        OrderLookup.Add("salad", 20);

        //translate addon strings to IDs
        AddonsLookup.Add("mustard", 9);
        AddonsLookup.Add("ketchup", 10);
        AddonsLookup.Add("cheese", 13);
        AddonsLookup.Add("lettuce", 14);
        AddonsLookup.Add("onion", 23);
        AddonsLookup.Add("tomato", 25);
        //game adds these to eggs & sausages, two of each (2,2,26,26), seems to not be necessary
        AddonsLookup.Add("egg", 2);
        AddonsLookup.Add("sausage", 26);

        //aliases and shorthands
        OrderLookup.Add("bg", 0);
        OrderLookup.Add("h", 1);
        OrderLookup.Add("c", 7);
        OrderLookup.Add("sd", 16);
        OrderLookup.Add("br", 17);
        OrderLookup.Add("p", 37);
        OrderLookup.Add("pancake", 37);
        OrderLookup.Add("eggs & sausage", 38);
        OrderLookup.Add("eggs", 38);
        OrderLookup.Add("e&s", 38);
        OrderLookup.Add("e & s", 38);
        OrderLookup.Add("es", 38);
        OrderLookup.Add("f", 32);
        OrderLookup.Add("onion ring", 33);
        OrderLookup.Add("or", 33);
        OrderLookup.Add("nuggies", 34);
        OrderLookup.Add("n", 34);
        OrderLookup.Add("sl", 20);
        AddonsLookup.Add("m", 9);
        AddonsLookup.Add("k", 10);
        AddonsLookup.Add("c", 13);
        AddonsLookup.Add("l", 14);
        AddonsLookup.Add("o", 23);
        AddonsLookup.Add("t", 25);

        //additional lookup table for checking available addons
        OrderIDToOrderIndex.Add(0, 0);
        OrderIDToOrderIndex.Add(1, 1);
        OrderIDToOrderIndex.Add(7, 2);
        OrderIDToOrderIndex.Add(16, 3);
        OrderIDToOrderIndex.Add(17, 4);
        OrderIDToOrderIndex.Add(37, 5);
        OrderIDToOrderIndex.Add(38, 6);
        OrderIDToOrderIndex.Add(32, 7);
        OrderIDToOrderIndex.Add(33, 8);
        OrderIDToOrderIndex.Add(34, 9);
        OrderIDToOrderIndex.Add(20, 10);


        //translate skin names to IDs
        SkinsLookup.Add("guy sweater", 0); //guy with grey sweater
        SkinsLookup.Add("suit red", 1); //suit red tie
        SkinsLookup.Add("suit green", 2); //suit green tie
        SkinsLookup.Add("lady red", 3); //lady red top
        SkinsLookup.Add("lady green", 4); //lady green sweater
        SkinsLookup.Add("guy jacket", 5); //guy jacket
        SkinsLookup.Add("guy jumper", 6); //guy grey jumper
        SkinsLookup.Add("guy sunglasses", 7); //guy sunglasses
        SkinsLookup.Add("cop", 8); //cop
        SkinsLookup.Add("lady hat", 9); //lady hat
        SkinsLookup.Add("clown", 10); //clown

        //create twitch.cfg
        twitchSettings = MelonPreferences.CreateCategory("twitch");
        twitchSettings.SetFilePath("twitch.cfg");
        twitchChannel = twitchSettings.CreateEntry<string>("Channel", "");
        //botName = twitchSettings.CreateEntry<string>("Bot Username", "");
        botAuth = twitchSettings.CreateEntry<string>("Bot OAuth Token", "");
        chatterMax = twitchSettings.CreateEntry<int>("Max Orders per Chatter", 0);
        chatterCooldown = twitchSettings.CreateEntry<int>("Order Cooldown", 0);
        addonsMax = twitchSettings.CreateEntry<int>("Maximum Addons per Dish", 6);
        rewardID = twitchSettings.CreateEntry<string>("Channel Point Reward ID", "");
        nameInWorld = twitchSettings.CreateEntry<bool>("Show Chatter Name on Customer instead of on UI", true);
        showChat = twitchSettings.CreateEntry<bool>("Show Chat Messages from Current Customer Chatter", true);

        bellRingsMax = twitchSettings.CreateEntry<int>("Maximum Bell Rings Per Order", 0);
        skipPenalty = twitchSettings.CreateEntry<float>("Time Penalty for skipping Orders", 30);

        twitchSettings.SaveToFile();

        //create chatterSkins.cfg
        chatterSkins = MelonPreferences.CreateCategory("skins");
        chatterSkins.SetFilePath("chatterSkins.cfg");
        chatterSkins.SaveToFile();


        chatterMax.OnEntryValueChanged.Subscribe(PrintConfig, 100);
        chatterCooldown.OnEntryValueChanged.Subscribe(PrintConfig, 100);
        addonsMax.OnEntryValueChanged.Subscribe(PrintConfig, 100);

        LoggerInstance.Msg("Cooldown: " + chatterCooldown.Value + " Max Orders: " + chatterMax.Value);
        LoggerInstance.Msg("Channel " + twitchChannel.Value + " Bot OAuth Token " + botAuth.Value);

        ConnectToTwitch();  //initial connection

        LoggerInstance.Msg("Initialized.");
    }

    public override void OnApplicationQuit()
    {
        Twitch.Close();
        base.OnApplicationQuit();
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        base.OnSceneWasLoaded(buildIndex, sceneName);
        if (buildIndex == 0 && sceneName == "SampleScene")
        {
            twitchSettings.SetFilePath("twitch.cfg");   //reload values from cfg when main menu is loaded or on restart
            bellsRung = 0;
        }

    }

    public override void OnLateUpdate()
    {
        if (!Twitch.Connected) ConnectToTwitch();

        //ping twitch every minute so connection doesn't close
        PingCounter += Time.deltaTime;
        if (PingCounter > 60)
        {
            Writer.WriteLine("PING " + URL);
            Writer.Flush();
            PingCounter = 0;
        }

        TryToRingBell();
        //LoggerInstance.Msg(bellsLeft + "    " + bellLastRungAt);


        if (Twitch.Available > 0)   //message from twitch received
        {
            string msg = Reader.ReadLine();
            if (msg.Length > 0) LoggerInstance.Msg(msg);

            if (msg == ":tmi.twitch.tv NOTICE * :Login authentication failed")
            {
                LoggerInstance.Msg("Could not authenticate with Twitch, make sure the bot's token is of type User Access");
            }
            //once twitch confirms connection, send initial messages
            if (msg.StartsWith(":tmi.twitch.tv 001"))
            {
                SendMessage("Kiosk orders are open! Use !kioskhelp for details on how to order, and !kioskoptions for the available dishes and addons.");
                PrintConfig(0, 1);  //print the config values regardless of if it was changed
                SendMessage("If you're the customer at the window, all your messages will appear ingame, and you can ring the bell multiple times using for example !bell 5");
            }

            if (msg.Contains("PRIVMSG"))
            {    //is actual chat message
                TwitchMessage message = ParseMessage(msg);

                //custom-reward-id=40a52456-6f7d-4a19-8eee-a1d20ca5b1a6
                string reward = rewardID.Value;
                bool isMessageReward = msg.Contains("custom-reward-id=" + reward);
                bool hasCommand = message.content.StartsWith("!order ");

                if (message.content.StartsWith("!kioskhelp"))
                {
                    SendMessage("Use !order followed by your order. List up to 4 items (names as displayed on the whiteboard) seperated by commas, add addons to each item by seperating them with spaces. " +
                        "Check available order options via !kioskoptions. " +
                        "For example: !order beer, burger ketchup onion, eggs & sausages, salad lettuce");
                }
                else if (message.content.StartsWith("!kioskoptions"))
                {
                    SendMessage("Burger (with Ketchup/Mustard/Lettuce/Tomato/Onion/Cheese), Hotdog (with Ketchup/Mustard/Lettuce), Salad (must have Lettuce, with Tomato/Onion), Pancakes, Eggs & Sausages, Fries, Onion Rings, Nuggets, Coffee, Soda, Beer");
                }

                else if (message.content.StartsWith("!skin"))
                {
                    if (message.content.Length <= 6) //message has nothing after !skin
                        SendMessage("Available Skins: Guy Sweater, Guy Jacket, Guy Jumper, Guy Sunglasses, Suit Red, Suit Green, Lady Red, Lady Green, Lady Hat, Cop, Clown");
                    else
                    {
                        string skin = message.content.Substring(6);
                        if (SkinsLookup.ContainsKey(skin))
                        {
                            int skinID = SkinsLookup[message.content.Substring(6)];
                            //if chatter alreadyd has a config entry, delete and recreate it
                            if (chatterSkins.HasEntry(message.chatter.name))
                                chatterSkins.DeleteEntry(message.chatter.name);
                            chatterSkins.CreateEntry<int>(message.chatter.name, skinID);

                            chatterSkins.SaveToFile();
                        }
                    }
                }

                else if (reward == "" && hasCommand || reward != "" && isMessageReward)
                {

                    string ordermsg = hasCommand ? message.content.Substring(6) : message.content; //cut off "!order" and take rest of message
                    TwitchOrder order = new(ConvertOrder(ordermsg), message.chatter, Time.realtimeSinceStartup);
                    if (order.items.Count() > 0) //is order valid
                    {
                        if (IsChatterOnCooldown(message.chatter.name))
                        {
                            SendMessage("@" + message.chatter.name + " Order discarded, you are still on cooldown");
                        }
                        else if (IsChatterOverMax(message.chatter.name))
                        {
                            SendMessage("@" + message.chatter.name + " Order discarded, you have too many orders in the queue");
                        }
                        else //order is valid, and chatter is not on cooldown or at max
                        {
                            queue.Enqueue(order);
                            LoggerInstance.Msg("Order added from " + order.chatter.name + " at " + order.timestamp);
                        }
                    }
                    else SendMessage("@" + message.chatter.name + " Invalid order, check !kioskhelp and !kioskoptions for how to place an order");
                }
                else //normal message
                {
                    if (currentOrder.chatter.name == message.chatter.name)  //current customer chatter typed
                    {
                        if (message.content.StartsWith("!bell"))
                        {
                            int n = 1;
                            if (message.content.Length >= 6)    //message has !bell_ and more, parse out number of bells
                                int.TryParse(message.content.Substring(6).Split(" ")[0], out n);

                            int remaining = bellRingsMax.Value - bellsRung;
                            bellsLeft = n > 0 ? (bellRingsMax.Value > 0 ? Math.Min(n, remaining) : n) : 1;
                        }
                        else if (showChat.Value) //show their chat message next to the customer
                            AddMessageToCustomer(message.content);
                    }
                }
            }
        }
    }

    public void ConnectToTwitch()
    {
        Twitch = new TcpClient(URL, PORT);
        Reader = new StreamReader(Twitch.GetStream());
        Writer = new StreamWriter(Twitch.GetStream());

        string token = botAuth.Value;
        if (!token.StartsWith("oauth:")) token = "oauth:" + token;  //if provided token doesn't have "oauth:" at the start, add it

        Writer.WriteLine("PASS " + token);
        //Writer.WriteLine("NICK " + botName.Value.ToLower());
        Writer.WriteLine("NICK placeholder");
        Writer.WriteLine("JOIN #" + twitchChannel.Value.ToLower());
        Writer.WriteLine("CAP REQ :twitch.tv/tags");
        Writer.Flush();
    }

    //tell the bot to send a message with the given text
    public void SendMessage(string text)
    {
        Writer.WriteLine("PRIVMSG #" + twitchChannel.Value.ToLower() + " :" + text);
        Writer.Flush();
    }

    public void PrintConfig(int oldValue, int newValue)
    {
        if (oldValue != newValue)
            SendMessage("Order cooldown is " + chatterCooldown.Value +
                " seconds, maximum amount of orders per chatter is " + chatterMax.Value +
                ", maximum addons per dish is " + addonsMax.Value);
    }

    //take a string reponse returned by Twitch, and return a TwitchMessage (text content and chatter)
    //@badge-info=;badges=bits-charity/1;client-nonce=6c1399c6f3b54f311f45f7c7b32789dc;color=#00FF7F;display-name=Bartolini;emotes=;first-msg=0;flags=;id=db172017-2c4b-4961-987c-c866c42edd97;mod=0;returning-chatter=0;room-id=38534648;subscriber=0;tmi-sent-ts=1740008344259;turbo=0;user-id=130442997;user-type=
    //:bartolini!bartolini@bartolini.tmi.twitch.tv PRIVMSG #goodigo :!order pancake
    public TwitchMessage ParseMessage(string response)
    {
        TwitchChatter chatter = ParseChatterFromMessage(response);

        int splitPoint = response.IndexOf("PRIVMSG") + 9 + twitchChannel.Value.Length + 2; //offset PRIVMSG and channel name
        string content = response.Substring(splitPoint);
        return new TwitchMessage(content.ToLower(), chatter);
    }

    public TwitchChatter ParseChatterFromMessage(string response)
    {
        int splitPoint = response.IndexOf("color=") + 6;
        string color = response.Substring(splitPoint, 7);   //grab 7 characters of color

        splitPoint = response.IndexOf("display-name=") + 13;  //offset display name tag
        string chatName = response.Substring(splitPoint, response.Substring(splitPoint).IndexOf(";")); //grab string after display name tag until ;
        TwitchChatter chatter = new(chatName, color);
        return chatter;
    }

    public override void OnGUI()
    {
        if (GameManager.instance.isEndlessOrRelaxMode())
        {
            if (!nameInWorld.Value)
            {
                GUI.Label(new Rect(50, Screen.height - 120, 2000, 100),
                "<b><size=32>Order from: <color=" + currentOrder.chatter.color + ">" + currentOrder.chatter.name + "</color></size></b>");
            }
            int count = queue.Count();
            GUI.Label(new Rect(50, Screen.height - 80, 1000, 100),
                "<b><size=26><color=#AAAAAA>" + queue.Count() + " " + (count == 1 ? "Order" : "Orders") + " in Queue</color></size></b>");
        }

        GUI.Label(new Rect(Screen.width - 150, Screen.height - 20, 500, 25),
                "<b><size=12><color=#AAAAAA>Twitch Integration loaded</color></size></b>");
    }

    //take one chat message order (comma-seperated orders, each with a main + addons) and convert it to a list of OrdedItemNew
    public static List<OrdedItemNew> ConvertOrder(string orders)
    {
        List<OrdedItemNew> converted = new();

        List<string> orderItems = orders.Split(',').ToList(); //split into individual order items
        foreach (string order in orderItems)
        {
            if (converted.Count() >= 4) break; //if 4 valid orders have been registered, stop

            string orderTrimmed = order.Trim().ToLower();
            Melon<Core>.Logger.Msg("Order: " + orderTrimmed);

            bool withTheLot = false;

            string main;
            List<string> addons = new();
            if (OrderLookup.ContainsKey(orderTrimmed)) main = orderTrimmed;    //if whole order is just a main dish, assign directly and skip addons
            else
            {
                string[] items = orderTrimmed.Split(' ');
                main = items[0];

                if (orderTrimmed.EndsWith("with the lot")) withTheLot = true;

                for (int i = 1; i < items.Count(); i++)
                    addons.Add(items[i]);
                addons = addons.Distinct().ToList(); //eliminate duplicate addons
            }
            bool valid = IsOrderValid(main, addons);
            if (!valid && !withTheLot) continue;

            OrdedItemNew newOrder = new();
            newOrder.objectID = OrderLookup[main];

            if (withTheLot) //if ordered with the lot, add all available addons
            {
                int addonCount = 0;
                List<GameManager.Addon> available = GameManager.instance.availableOrders[OrderIDToOrderIndex[OrderLookup[main]]].availableAddons;
                available.Sort((a, b) => UnityEngine.Random.Range(-10, 11));    //randomise order of available addon
                foreach (GameManager.Addon addon in available)
                {
                    if (addonCount >= addonsMax.Value) break; //if max addon limit is reached, stop adding addons
                    newOrder.additions.Add(addon.addonID);
                    addonCount++;
                }
            }
            else //else add addons from order
            {
                foreach (string addon in addons)
                    newOrder.additions.Add(AddonsLookup[addon]);
            }
            converted.Add(newOrder);
        }
        return converted;
    }

    //take one item order (main + addons) as strings and check if it is valid
    public static bool IsOrderValid(string main, List<string> addons)
    {
        if (addons.Count() > addonsMax.Value) return false;
        if (!OrderLookup.ContainsKey(main)) return false;   //if main dish doesn't exist return false

        if (main == "salad" && !addons.Contains("lettuce")) return false;   //salad must have lettuce as an addon

        //check if given addons are available for main dish
        GameManager.AvailableOrders availableOrders = GameManager.instance.availableOrders[OrderIDToOrderIndex[OrderLookup[main]]];
        //make list of all available addons for this main dish
        List<int> availableAddonIDs = new List<int>();
        foreach (GameManager.Addon addon in availableOrders.availableAddons)
        {
            availableAddonIDs.Add(addon.addonID);
        }

        foreach (string addon in addons)
        {
            if (!AddonsLookup.ContainsKey(addon)) return false; //check if addon is valid in AddonsLookup, else it crashes below on invalid addons
            if (!availableAddonIDs.Contains(AddonsLookup[addon]))
                return false; //if not all addons are available for main dish return false
        }

        return true;

    }


    //check if chatter is still on cooldown
    public bool IsChatterOnCooldown(string name)
    {
        if (chatterCooldown.Value > 0)
        {
            IEnumerable<TwitchOrder> chatterOrders = queue.Where(o => o.chatter.name == name);  //grab every order from chatter
            TwitchOrder order = chatterOrders.LastOrDefault();  //grab last order from chatter
            if (order.chatter.name == null) return false; //check if queue has no orders with the name
            float timeUntilCooldown = chatterCooldown.Value - (Time.realtimeSinceStartup - order.timestamp);
            if (timeUntilCooldown > 0)
            {
                return true;
            }
        }
        return false;
    }

    //check if chatter already has max orders in queue
    public bool IsChatterOverMax(string name)
    {
        if (chatterMax.Value > 0)
        {
            IEnumerable<TwitchOrder> chatterOrders = queue.Where(o => o.chatter.name == name);  //grab every order from chatter
            return (chatterOrders.Count() >= chatterMax.Value);
        }
        return false;
    }

    //spawn the name of the current order's chatter above the customer
    public static void AddNameToCustomer(TwitchChatter chatter)
    {
        Customer currentCustomer = CustomersController.instance.GetCurrentCustomer();

        GameObject o = new();
        o.transform.SetParent(currentCustomer.headRotate, false);
        TextMeshPro tmp = o.AddComponent<TextMeshPro>();

        o.transform.Rotate(new Vector3(0, 180, 0));
        o.transform.localScale *= 0.02f;
        o.transform.localPosition += new Vector3(0, 0.32f, 0);
        //o.transform.localPosition += new Vector3(-5.05f, -.85f, 1.9f);


        tmp.enableWordWrapping = false;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;

        tmp.richText = true;

        tmp.text = "<color=" + chatter.color + ">" + chatter.name + "</color>";
    }

    //spawn the current order's chatter's messages in front of the customer
    public static void AddMessageToCustomer(string text)
    {
        if (messageTextObject) GameObject.Destroy(messageTextObject);
        Customer currentCustomer = CustomersController.instance.GetCurrentCustomer();

        GameObject o = new();
        messageTextObject = o;
        o.transform.SetParent(currentCustomer.headRotate, false);
        TextMeshPro tmp = o.AddComponent<TextMeshPro>();

        o.transform.Rotate(new Vector3(0, 180, 0));
        o.transform.localScale *= 0.015f;
        o.transform.localPosition += new Vector3(0, 0.08f, 0.2f);

        tmp.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 5);

        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;

        tmp.text = text;
        AudioController.instance.SpawnShakeSoundAtPos(currentCustomer.headRotate.position); //spawn sound when new text spawns
        Melon<Core>.Logger.Msg("Displaying " + text + " next to customer");
    }

    //ring the bell if possible
    public static void TryToRingBell()
    {
        float bellCooldown = 0.2f;

        float now = Time.realtimeSinceStartup;
        //Melon<Core>.Logger.Msg("Bells left " + bellsLeft);
        if (now - bellLastRungAt > bellCooldown && bellsLeft > 0)
        {
            ReactionController.instance.InvokeReaction("ServiceBellSound");
            bellLastRungAt = now;
            bellsLeft--;
            bellsRung++;
        }
    }


    //overwrite regular random order generation in endless mode, to instead grab from the queue (filled by twitch chat)
    [HarmonyPatch(typeof(Customer), "GenerateEndlessOrder")]
    private static class GenerateEndlessOrderFromQueue
    {
        private static bool Prefix()
        {
            Customer currentCustomer = CustomersController.instance.GetCurrentCustomer();
            currentCustomer.endlessOrder.Clear();

            //queue.Enqueue(new TwitchOrder(ConvertOrder("Hotdog lettuce, beer, burger ketchup onion, soda"), "Goodigo")); //example order

            if (!queue.Any())
            {
                Melon<Core>.Logger.Msg("No Twitch orders in queue, letting game generate random order");
                return true;
            }

            currentOrder = queue.Dequeue();
            List<OrdedItemNew> orders = currentOrder.items;

            foreach (OrdedItemNew order in orders)
            {
                currentCustomer.endlessOrder.Add(order);
            }
            if (Core.nameInWorld.Value) AddNameToCustomer(currentOrder.chatter);
            return false;
        }
    }

    //clear chatter for current order when customer walks away, so it disappears from the gui
    [HarmonyPatch(typeof(Customer), "GoAway")]
    private static class ClearChatter
    {
        private static void Postfix()
        {
            currentOrder.chatter = new TwitchChatter("", "");
            GameObject.Destroy(messageTextObject); //clear chat message next to customer
            bellsLeft = 0; //cancel all currently queued up bell rings
            bellsRung = 0; //reset chatter bell ring count
        }
    }

    //don't update steam leaderboard while mod is loaded
    [HarmonyPatch(typeof(SteamLeaderboardManager), "UpdateScore")]
    private static class DontUpdateLeaderboard
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    //if knife collides with customer, skip the order and apply penalty
    [HarmonyPatch(typeof(Knife), "OnCollisionEnter")]
    private static class KnifeSkip
    {
        private static void Prefix(Collision other, Knife __instance)
        {
            if (other.gameObject.GetComponent<Customer>() != null)
            {
                Customer currentCustomer = CustomersController.instance.GetCurrentCustomer();
                //other.gameObject.GetComponent<Customer>().KnifeDialog();  //show 'Be careful with that' dialogue
                GameObject.Destroy(__instance.gameObject);
                ReactionController.instance.InvokeReaction("PlayXBadParticle");

                //set ordered to true so that knife sequence doesn't play again in OnCollisionEnter
                typeof(Customer).GetField("ordered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(currentCustomer, true);

                Melon<Core>.Logger.Msg("KNIFE THROWN");
                currentCustomer.GoAway();
                if (skipPenalty.Value > 0) TimerController.instance.SetCurrentSeconds(0f - skipPenalty.Value);
            }
        }
    }

    //change result of random customer skin picking to instead pick from config (if available, else continue with random skin)
    [HarmonyPatch(typeof(CustomersController), "GetRandomNonRepeating")]
    private static class GetFixedCustomer
    {
        private static void Postfix(ref int __result)
        {
            if (queue.TryPeek(out TwitchOrder o))   //there is an order in the queue
            {
                if (chatterSkins.HasEntry(o.chatter.name))
                {  //chatter has a skin set in the config
                    __result = ((MelonPreferences_Entry<int>)chatterSkins.GetEntry(o.chatter.name)).Value;
                }
            }
        }
    }
}