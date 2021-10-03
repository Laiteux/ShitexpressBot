# ShitexpressBot [![License](https://img.shields.io/github/license/Laiteux/ShitexpressBot?color=blue&style=flat-square)](https://github.com/Laiteux/ShitexpressBot/blob/master/LICENSE)

An unofficial Telegram bot for ordering from [Shitexpress](https://www.shitexpress.com).

Official blog post: https://www.shitexpress.com/blog/telegram-bot/

Absolutely no data is stored, bot usage is 100% anonymous and official Shitexpress APIs are used for payment and everything.

Animals, stickers or even delivery countries may change in the future, so I added the ability to edit them depending on what Shitexpress.com offers. See the `Settings.json` file for that.

I have to say the code is pretty ugly, the `Order.cs` class is way too big and should obviously be refactored. I wrote the whole code in one go and couldn't be bothered to rewrite anything, I'm even using hacks such as reflection which I don't like so much here. I'm publishing the code as it, it perfectly works and I am way too lazy to refactor it for now. Maybe one day :D

Features:
- Place an order using the `/order` command, there's a pretty cool interactive menu you'll find easy to use
- Pay using Bitcoin
- Check order status with inline mode

Try it: https://t.me/ShitexpressBot

## Screenshots

![Order interactive menu](https://share.laiteux.dev/abmpiykb)

![Inline order status check](https://share.laiteux.dev/geaikgxy)

## Contribute

Your help and ideas are welcome, feel free to fork this repo and submit a pull request.

However, please make sure to follow the current code base/style.

## Contact

Telegram: [@Matty](https://t.me/Matty)

Email: matt@laiteux.dev

## Donate

If you would like to support this project, please consider donating.

Donations are greatly appreciated and a motivation to keep improving.

- Bitcoin: `1LaiteuxHEH4GsMC9aVmnwgUEZyrG6BiTH`
