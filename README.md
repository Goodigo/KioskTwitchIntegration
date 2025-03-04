# KioskTwitchIntegration
Twitch Integration Mod using Melon Loader for the Endless &amp; Relax modes in Kiosk, letting Twitch chatters place orders and interact with the game.
## Features
- Twitch chatters can place their own orders, via a chat command or a channel point reward
- Customers walking up to the window will show the name of the chatter who placed the order
- While a chatter's order is up their chat messages will display on the customer ingame, and they can ring the service bell
- Chatters can use a chat command to pick their customer skin from the 10 customer models in the game, persisting through sessions
- Throwing a knife at a customer will skip their order, giving a time penalty in Endless mode
## Setup
### Mod Installation
- Download the latest version of [Melon Loader](https://melonwiki.xyz) and install it to Kiosk
- Download the latest version of the mod from the [Releases page](https://github.com/Goodigo/KioskTwitchIntegration/releases)
- Navigate to your Kiosk folder (in your Steam library, rightclick Kiosk -> Manage -> Browse Local Files)
- Put the Kiosk_Twitch_Integration.dll in the Mods folder
- Start the game once to let it generate necessary config files
### Setting up a Twitch chatbot
- If you want the bot to use a seperate account instead of your main one, make a new Twitch account
- Head to the [Twitch Developer Console](https://dev.twitch.tv/console) and log in with the account you want your bot to use
- Register a new Application
- Generate an OAuth token, the easiest way is using [Twitch OAuth Token Generator](https://twitchapps.com/tokengen/) (just put that link as the OAuth Redirect URL for your Application, copy the Application's Client ID into the website, and set the scopes as chat:edit chat:read). This token can be used to read & write messages using your bot, so do not give it out to people you don't trust.
### Basic Configuration
- After starting the game once with the mod loaded, there should be a twitch.cfg in your Kiosk folder
- Add the name of the channel you want the bot to connect to, and the previously generated OAuth token for the bot
- Launching the game should now make the bot appear in the provided channel's chat, if not check the Melon Loader console for a reponse from the Twitch API (it's likely an issue with your OAuth token)
## Configuration Options (twitch.cfg)
- You can edit & save the config at any time, it will be reloaded upon exiting to main menu or restarting Endless/Relax mode
- **Channel:** Name of the Twitch channel you want the bot to connect to
- **Bot OAuth Token:** OAuth token for the Twitch Application you are running the bot on
- **Max Orders per Chatter:** Maximum number of orders a chatter can have in the order queue at once, set to 0 for unlimited
- **Order Cooldown:** Cooldown in seconds before a user can place another order
- **Maximum Addons per Dish:** Maximum number of addons a chatter can add to a main dish (e.g. Burger + Lettuce, Onion, Cheese, Mustard), set to 0 for unlimited
- **Channel Point Reward ID:** Leave empty if you want to use the chat command for placing orders, otherwise enter your Channel Point Reward's ID. To find the ID, create your Channel Point Reward (must require viewer to enter text). Then launch the game and redeem the reward in your chat, which should print out a message in the Melon Loader console containing the ID (saying something like *custom-reward-id=40c52426-4f7d-4d19-8eee-a2b20ca9b0a6*)
- **Show Chatter Name on Customer instead of on UI:** If set to true, will display the name of the chatter who placed the current order above the customer's head. If set to false, will instead display the name on the UI
- **Show Chat Messages from Current Customer Chatter:** If set to true, will display any messages posted by the chatter who placed the current order in front of the customer (there is no filters or moderation on these messages, so use at your own risk). If set to false, no messages will be displayed ingame
- **Maximum Bell Rings Per Order:** Maximum number of times a chatter whose order is currently up can ring the service bell for the duration of that order
- **Time Penalty for skipping Orders:** Time penalty in seconds that is applied in Endless mode when skipping an order by throwing a knife at the customer
## Usage
- Launching the game with the mod loaded should make your bot appear in your Twitch chat
- Available help commands: !kioskhelp (explaining the basics of how to place an order), !kioskoptions (listing the main dishes & addons you can order)
- Use !order to order up to 4 dishes, seperated by commas. You can add addons to each dish by seperating them with spaces. For example: *!order burger lettuce onion ketchup, soda, eggs & sausages, salad lettuce tomato*
- You can also use various shorthands (mainly 1-2 letter abbreviations and alternate names) for your orders, for example *!order h o m, p, nuggies* would order a hotdog with onion & mustard, pancakes, and nuggets
- Use !bell [amount] to ring the service bell howevery many times you like while your order is up, for example *!bell 12*
- Use !skin to show the available customer skins and set one, for example *!skin clown*. These are saved to the chatterSkins.cfg, so that a chatter's chosen skin will persist across sessions
## Contributing or editing the mod
- Melon Loader lets you easily set up a VisualStudio project that hooks into the game and decompiles its code for you, check out their [tutorial](https://melonwiki.xyz/#/modders/quickstart). Once that is set up, just copy the Core.cs into your project.
## Bugs, Suggestions & Requests
For any issues regarding setting up the mod or feature requests, send me a message on Discord (Username: Goodigo)
