using System;
using System.Collections.Generic;
using System.Text;
using ExchangeSharp;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using StockExchange.Messages;
using System.Threading.Tasks;
using StockExchange.Information;
using System.Linq;

namespace StockExchange
{
    public struct Emoji
    {
        public static string CheckMark = Char.ConvertFromUtf32(9989);
        public static string PreviousArrow = Char.ConvertFromUtf32(9194);
        public static string NextArrow = Char.ConvertFromUtf32(9193);
    }
    class TelegramBot
    {
        private readonly IInteractive _interactive;
        private string _key;
        private TelegramBotClient _bot;
        List<User> _users;
        private List<MainMessage> _messages;//bot states
        private readonly StockExchanges _stockExchanges;

        public TelegramBot(IInteractive interactive, string key)
        {
            _interactive = interactive;
            _key = key;

            try
            {
                _bot = new TelegramBotClient(_key);
            } 
            catch(System.ArgumentException exception)
            {
                _interactive.OutputAsync(exception.Message);
            }

            _users = new List<User>();

            _messages = new List<MainMessage>()
            {
                new Start(),
                new ChooseStockExchange(),
                new ChooseMarketSymbol(),
                new Clear(),
                new Back(),
                new Previous(),
                new Next()
            };
            _stockExchanges = new StockExchanges();
        }

        private List<List<InlineKeyboardButton>> Convert_ButtonNames_ToInlineButtons(List<string> buttonNames)
        {
            var buttons = new List<List<InlineKeyboardButton>>();
            foreach(var buttonName in buttonNames)
            {
                buttons.Add(new List<InlineKeyboardButton> { new InlineKeyboardButton() { Text = buttonName, CallbackData = buttonName }});
            }
            return buttons;
        }

        private long GetChatId(Update update)
        {
            return update.Message == null ? update.CallbackQuery.From.Id : update.Message.Chat.Id;
        }

        private User GetUser(long chatId)
        {
            foreach (var user in _users)
            {
                if (user.ChatId == chatId)
                {
                    return user;
                }
            }
            User _user;
            _users.Add(_user = new User(chatId));
            return _user;
        }

        private async Task SendFirstMessageAsync(User user, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                                {
                                    new List<KeyboardButton>{new KeyboardButton(new Start().Message)},
                                    new List<KeyboardButton>{new KeyboardButton(new Clear().Message)}
                                }
            };
            await _bot.SendTextMessageAsync(chatId, "Главная", replyMarkup: replyKeyboard);
            user.IsFirstMessage = false;
        }
        //Determine the type of command.And if command exist
        private MainMessage GetMessage(string text, out bool ifMessageExist)
        {
            ifMessageExist = false;
            foreach (var message in _messages)
            {
                if (message.Message == text)
                {
                    ifMessageExist = true;
                    return message;
                }
            }
            return null;
        }
        //выводит все одной страницей
        private async Task SendOversizeMessageAsync(List<string> buttons, int limit, InlineKeyboardMarkup inlineKeyboard, User user, MainMessage message)
        {
            int modulo = buttons.Count % limit;
            for (int i = 0; i < Math.Ceiling((decimal)(buttons.Count / limit)); ++i)
            {
                var partButtons = new List<string>();
                if (i != Math.Ceiling((decimal)(buttons.Count / limit)) - 1)
                {
                    for (int j = i * limit; j < (i + 1) * limit; ++j)
                    {
                        partButtons.Add(buttons[j]);
                    }
                }
                else
                {
                    for (int j = (i + 1) * limit; j < (i + 1) * limit + modulo; ++j)
                    {
                        partButtons.Add(buttons[j]);
                    }
                }
                inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(partButtons));
                await _bot.SendTextMessageAsync(user.ChatId, message.Message, replyMarkup: inlineKeyboard);
            }
        }
        //выводит несколькими страницами
        private async Task SendOversizeMessageAsync(List<string> buttons, int limit, int pageNumber, InlineKeyboardMarkup inlineKeyboard, User user, MainMessage message)
        {
            int modulo = buttons.Count % limit;
            var partButtons = new List<string>();
            decimal totalNumberPages = Math.Ceiling((decimal)(buttons.Count / limit)) - 1;
            if(pageNumber != totalNumberPages)
            {
                for(int j = pageNumber * limit; j < (pageNumber + 1) * limit; ++j)
                {
                    partButtons.Add(buttons[j]);
                }
            }
            else
            {
                for(int j = (pageNumber + 1) * limit; j < (pageNumber + 1) * limit + modulo; ++j)
                {
                    partButtons.Add(buttons[j]);
                }
            }
            if (pageNumber < totalNumberPages) 
            {
                partButtons.Add(new Next().Message);
            }
            if(pageNumber > 0)
            {
                partButtons.Add(new Previous().Message);
            }
            inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(partButtons));
            await _bot.SendTextMessageAsync(user.ChatId, message.Message, replyMarkup: inlineKeyboard);
        }

        private async Task SendMessageAsync(User user, MainMessage message, int pageNumber)
        {
            InlineKeyboardMarkup inlineKeyboard = null;
            var isOversizeMessage = false;
            if (user.StockExchange == null && (message.Message == new Start().Message || message.Message == new Back().Message))
            {
                inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(new List<string>() { new ChooseStockExchange().Message }));
            }
            else
            {
                List<string> buttons = await message.OnSend();
                const int limit = 95;
                if(user.MarketSymbols.Count > 0 && message.GetType() == new ChooseMarketSymbol().GetType())
                {
                    foreach(var selectedMarketSymbol in user.MarketSymbols)
                    {
                        for(var marketSymbol = 0; marketSymbol < buttons.Count; ++marketSymbol)
                        {
                            if(await user.StockExchange.ExchangeMarketSymbolToGlobalMarketSymbolAsync(selectedMarketSymbol) == buttons[marketSymbol])
                            {
                                buttons[marketSymbol] = buttons[marketSymbol] + Emoji.CheckMark;
                            }
                        }
                    }
                }
                if (buttons.Count > limit)
                {
                    
                    await SendOversizeMessageAsync(buttons, limit, pageNumber, inlineKeyboard, user, message);
                    isOversizeMessage = true;
                }
                else
                {
                    inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(await message.OnSend()));
                }
            }
            if (!isOversizeMessage)
            {
                await _bot.SendTextMessageAsync(user.ChatId, message.Message, replyMarkup: inlineKeyboard);
            }
        }

        //todo:подумать над неправильным вводом без кнопок
        public async Task Run()
        {
            int offset = 0;
            #region Set offset considering old updates
            var oldUpdates = await _bot.GetUpdatesAsync(0);
            int pageNumber = 0;//для текста, превышающего лимит
            MainMessage intermediateMessageBetweenPages = null;
            if(oldUpdates.Length != 0)
            {
                offset = oldUpdates[oldUpdates.Length - 1].Id + 1;
            }
            #endregion
            MainMessage previousMessage = null;
            while (true)
            {
                var updates = await _bot.GetUpdatesAsync(offset);
                if(updates.Length != 0)
                {
                    foreach (var update in updates)
                    {
                        long chatId = GetChatId(update);
                        User user = GetUser(chatId);
                        if (user.IsFirstMessage)
                        {
                            await SendFirstMessageAsync(user, chatId);
                        }
                        MainMessage message = null;
                        if (update.Message != null)
                        {    
                            message = GetMessage(update.Message.Text, out bool ifMessageExist);
                            if (!ifMessageExist && !update.Message.From.IsBot)
                            {
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Выберите команду из предоставленных");
                                continue;
                            }
                        }
                        else
                        {   
                            //todo: сделать переменные чтобы не писать постоянно new
                            //       убрать повторы foreach(в функцию)
                            message = GetMessage(update.CallbackQuery.Data, out bool ifMessageExist);
                            /*if (!ifMessageExist && !update.CallbackQuery.From.IsBot)
                            {
                                await _bot.SendTextMessageAsync(update.CallbackQuery.From.Id, "Выберите команду из предоставленных");
                                continue;
                                
                            }*/
                            //ифы для записи в user
                            if(previousMessage.GetType() == new ChooseStockExchange().GetType())
                            {
                                foreach (var stockExchange in new StockExchanges().StockExchangesList)
                                {
                                    if (update.CallbackQuery.Data == stockExchange.Name)
                                    {
                                        user.StockExchange = stockExchange;
                                        user.MarketSymbols.Clear();
                                        message = new Start();
                                        break;
                                    }
                                }
                            } 
                            else if(previousMessage.GetType() == new ChooseMarketSymbol().GetType() || previousMessage.GetType() == new Previous().GetType() || previousMessage.GetType() == new Next().GetType())
                            {
                                if (update.CallbackQuery.Data != new Back().Message && update.CallbackQuery.Data != new Previous().Message && update.CallbackQuery.Data != new Next().Message)
                                {
                                    foreach (var marketSymbol in await new MarketSymbols(user.StockExchange.Name).GetMarketSymbols())
                                    {
                                        string currentSymbol = update.CallbackQuery.Data;
                                        if (update.CallbackQuery.Data.Contains(Emoji.CheckMark))
                                        {
                                            currentSymbol = currentSymbol.Remove(currentSymbol.IndexOf(Emoji.CheckMark));
                                        }
                                        if (await user.StockExchange.GlobalMarketSymbolToExchangeMarketSymbolAsync(currentSymbol) == marketSymbol)
                                        {
                                            var marketSymbolIsAlreadyRecorded = false;
                                            foreach(var oldMarketSymbol in user.MarketSymbols)
                                            {
                                                if(oldMarketSymbol== marketSymbol)
                                                {
                                                    marketSymbolIsAlreadyRecorded = true;
                                                    break;
                                                }
                                            }
                                            if (!marketSymbolIsAlreadyRecorded)
                                            {
                                                user.MarketSymbols.Add(marketSymbol);
                                            }
                                            message = new Start();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (message.GetType() == new ChooseMarketSymbol().GetType())
                        {
                            message.ExchangeAPI = user.StockExchange;
                            intermediateMessageBetweenPages = message;
                            pageNumber = 0;
                            await SendMessageAsync(user, intermediateMessageBetweenPages, pageNumber);
                        }
                        else if (message.GetType() == new Previous().GetType())
                        {
                            --pageNumber;
                            await SendMessageAsync(user, intermediateMessageBetweenPages, pageNumber);
                        }
                        else if (message.GetType() == new Next().GetType())
                        {
                            ++pageNumber;
                            await SendMessageAsync(user, intermediateMessageBetweenPages, pageNumber);
                        }
                        else
                        {
                            pageNumber = 0;
                            await SendMessageAsync(user, message, pageNumber);
                        }
                        previousMessage = message;
                        offset = updates[updates.Length - 1].Id + 1;
                    }
                }
            }
        }
    }
}
