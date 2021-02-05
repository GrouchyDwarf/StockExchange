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
using System.Timers;

namespace StockExchange
{
    public struct Emoji
    {
        public static string CheckMark = Char.ConvertFromUtf32(9989);
        public static string PreviousArrow = Char.ConvertFromUtf32(9194);
        public static string NextArrow = Char.ConvertFromUtf32(9193);
    }
    public class TelegramBot
    {
        private readonly IInteractive _interactive;
        private string _key;
        private TelegramBotClient _bot;
        List<User> _users;
        private List<MainMessage> _messages;//bot states
        private readonly StockExchanges _stockExchanges;
        private Timer _timer;
        private bool _timeToUpdate;
        private bool _ifTimerStarted;
        private double _periodUpdate;
        private int _candlesLimit;
        private int _candlesCounter;

        public TelegramBot(IInteractive interactive, string key)
        {
            _interactive = interactive;
            _key = key;

            try
            {
                _bot = new TelegramBotClient(_key);
            }
            catch (System.ArgumentException exception)
            {
                _interactive.OutputAsync(exception.Message);
            }

            _users = new List<User>();

            _messages = new List<MainMessage>()
            {
                new Start(),
                new ChooseStockExchange(),
                new ChooseMarketSymbol(),
                new Back(),
                new Previous(),
                new Next(),
                new ChooseDataType()
            };
            _stockExchanges = new StockExchanges();
            _timeToUpdate = true;
            _periodUpdate = 10000;
            _timer = new Timer(_periodUpdate);
            _timer.Elapsed += Func;
            _ifTimerStarted = false;
            _candlesLimit = 10;
            _candlesCounter = 0;
        }

        private void Func(Object source, System.Timers.ElapsedEventArgs e)
        {
            _timeToUpdate = true;
        }

        private List<List<InlineKeyboardButton>> Convert_ButtonNames_ToInlineButtons(List<string> buttonNames)
        {
            var buttons = new List<List<InlineKeyboardButton>>();
            foreach (var buttonName in buttonNames)
            {
                buttons.Add(new List<InlineKeyboardButton> { new InlineKeyboardButton() { Text = buttonName, CallbackData = buttonName } });
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

        private async Task<int> SendMessageAsync(User user, long chatId)
        {
            var message = await _bot.SendTextMessageAsync(chatId, "Главная");
            return message.MessageId;
        }

        private async Task<int> SendFirstMessageAsync(User user, long chatId)
        {
            var replyKeyboard = new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                                {
                                    new List<KeyboardButton>{new KeyboardButton(new Start().Message)}
                                }
            };
            await _bot.SendTextMessageAsync(chatId, "Главная", replyMarkup: replyKeyboard);
            user.IsFirstMessage = false;
            return await SendMessageAsync(user, chatId);
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
        private async Task EditOversizeMessageAsync(List<string> buttons, int limit, int pageNumber, InlineKeyboardMarkup inlineKeyboard, User user, MainMessage message, int messageId)
        {
            int modulo = buttons.Count % limit;
            var partButtons = new List<string>();
            decimal totalNumberPages = Math.Ceiling((decimal)(buttons.Count / limit)) - 1;
            if (pageNumber != totalNumberPages)
            {
                for (int j = pageNumber * limit; j < (pageNumber + 1) * limit; ++j)
                {
                    partButtons.Add(buttons[j]);
                }
            }
            else
            {
                for (int j = (pageNumber + 1) * limit; j < (pageNumber + 1) * limit + modulo; ++j)
                {
                    partButtons.Add(buttons[j]);
                }
            }
            if (pageNumber < totalNumberPages)
            {
                partButtons.Add(new Next().Message);
            }
            if (pageNumber > 0)
            {
                partButtons.Add(new Previous().Message);
            }
            inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(partButtons));
            await _bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
            await _bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, replyMarkup: inlineKeyboard);
        }

        private List<string> MarkButtons(List<string> allStrings, List<string> selectedStrings, string mark)
        {
            foreach (var selectedString in selectedStrings)
            {
                for (var i = 0; i < allStrings.Count; ++i)
                {
                    if (selectedString == allStrings[i])
                    {
                        allStrings[i] = allStrings[i] + mark;
                    }
                }
            }
            return allStrings;
        }

        private async Task EditMessageAsync(User user, MainMessage message, int pageNumber, int messageId)
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
                if (user.Data.Count > 0 && message.GetType() == new ChooseMarketSymbol().GetType())
                {
                    var globalSelectedSymbols = new List<string>();
                    foreach (var selectedMarketSymbol in user.Data.Select(d => d.MarketSymbol).ToList())
                    {
                        globalSelectedSymbols.Add(await user.StockExchange.ExchangeMarketSymbolToGlobalMarketSymbolAsync(selectedMarketSymbol));
                    }
                    buttons = MarkButtons(buttons, globalSelectedSymbols, Emoji.CheckMark);
                }
                else if (user.DataTypes.Count > 0 && message.GetType() == new ChooseDataType().GetType())
                {
                    buttons = MarkButtons(buttons, user.DataTypes, Emoji.CheckMark);
                }
                if (buttons.Count > limit)
                {

                    await EditOversizeMessageAsync(buttons, limit, pageNumber, inlineKeyboard, user, message, messageId);
                    isOversizeMessage = true;
                }
                else
                {
                    inlineKeyboard = new InlineKeyboardMarkup(Convert_ButtonNames_ToInlineButtons(buttons));
                }
            }
            if (!isOversizeMessage)
            {
                await _bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
                await _bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, inlineKeyboard);
            }
        }

        private bool CheckIfRecorded(List<string> records, string newRecord)
        {
            var isAlreadyRecorded = false;
            foreach (var record in records)
            {
                if (record == newRecord)
                {
                    isAlreadyRecorded = true;
                    break;
                }
            }
            return isAlreadyRecorded;
        }

        private string DeleteMark(string record, string mark)
        {
            if (record.Contains(mark))
            {
                return record.Remove(record.IndexOf(mark));
            }
            return record;
        }

        private async Task OnGetDataFromWebSockets()
        {
            foreach (var user in _users)
            {
                if (user.DataTypes.Count > 0 && user.Data.Count > 0)
                {
                    foreach (var dataType in user.DataTypes)
                    {
                        foreach (var data in user.Data)
                        {
                            if (dataType == new Trades().Message)
                            {
                                await user.StockExchange.GetTradesWebSocketAsync(async trade =>
                                {
                                    await Task.FromResult(data.Trade = $"Ticker {trade.Key}\nPrice:{trade.Value.Price}; Amount:{trade.Value.Amount}");
                                }, data.MarketSymbol);
                            }
                            else if (dataType == new Tickers().Message)
                            {
                                //todo:добавить фильтр в библиотеку,где его нет
                                await user.StockExchange.GetTickersWebSocketAsync(async tickers =>
                                {
                                    foreach (var ticker in tickers)
                                    {
                                        await Task.FromResult(data.Ticker = $"Ticker {ticker.Key}; Value: {ticker.Value}");
                                    }
                                }, data.MarketSymbol);
                            }
                            else if (dataType == new Candles().Message)
                            {
                                if(data.Candles == null || _candlesLimit + 1 == _candlesCounter)
                                {
                                    data.Candles = new Stack<MarketCandle>();
                                    _candlesCounter = 0;
                                }
                                if (_timeToUpdate || data.Candles.Count == 0)
                                {
                                    if(data.Candles.Count == 0)
                                    {
                                        _timer.Start();
                                    }
                                    if (!_ifTimerStarted)
                                    {
                                        _timer.Enabled = true;
                                    }
                                    data.Candles.Push(new MarketCandle());
                                    _timeToUpdate = false;
                                    ++_candlesCounter;
                                }
                                var candle = data.Candles.Peek();
                                await user.StockExchange.GetTradesWebSocketAsync(async trade =>
                                {
                                    if (candle == data.Candles.Peek())
                                    {
                                        if (candle.ExchangeName != null)
                                        {
                                            candle.HighPrice = Math.Max(candle.HighPrice, trade.Value.Price);
                                            candle.LowPrice = Math.Min(candle.LowPrice, trade.Value.Price);
                                            candle.ClosePrice = trade.Value.Price;
                                        }
                                        else
                                        {
                                            candle.ExchangeName = trade.Key;
                                            candle.OpenPrice = trade.Value.Price;
                                            candle.LowPrice = decimal.MaxValue;
                                        }
                                    }
                                    await Task.FromResult("Success");
                                }, data.MarketSymbol);
                            }
                            string resultMessage = data.Ticker + "\n\n" + data.Trade + "\n\n";
                            int candlesNumber = 1;
                            foreach (var candle in new Stack<MarketCandle>(data.Candles))//constructor returns reversed stack
                            {
                                    if (candle.LowPrice != decimal.MaxValue)
                                    {
                                        resultMessage += $"{candlesNumber++}.Exchange Name:{candle.ExchangeName}; Open Price:{candle.OpenPrice}; Low Price: {candle.LowPrice}; High Price: {candle.HighPrice}; Close Price: {candle.ClosePrice}\n";
                                    }
                                    else
                                    {
                                        resultMessage += $"{candlesNumber++}.Exchange Name: {candle.ExchangeName}; Open Price:{candle.OpenPrice};\n";
                                    }
                            }
                            if (!string.IsNullOrWhiteSpace(resultMessage))
                            {
                                if (data.Message == null)
                                {
                                    data.Message = await _bot.SendTextMessageAsync(user.ChatId, resultMessage);
                                }
                                else if (data.Message.Text != resultMessage.Trim())
                                {
                                    data.Message = await _bot.EditMessageTextAsync(user.ChatId, data.Message.MessageId, resultMessage);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal async Task<(int messageId, int offset)> StartNewWebsocket(Update update, User user, long chatId, Update[] updates, int oldMessageId)
        {
            await _bot.DeleteMessageAsync(chatId, oldMessageId);
            int messageId = await SendMessageAsync(user, chatId);
            int offset = updates[updates.Length - 1].Id + 1;
            return (messageId, offset);
        }

        //todo:подумать над неправильным вводом без кнопок
        public async Task Run()
        {
            try
            {
                int offset = 0;
                #region Set offset considering old updates
                var oldUpdates = await _bot.GetUpdatesAsync(0);
                int pageNumber = 0;//для текста, превышающего лимит
                MainMessage intermediateMessageBetweenPages = null;
                if (oldUpdates.Length != 0)
                {
                    offset = oldUpdates[oldUpdates.Length - 1].Id + 1;
                }
                int messageId = 0;
                #endregion
                MainMessage previousMessage = null;
                while (true)
                {
                    var updates = await _bot.GetUpdatesAsync(offset);
                    if (updates.Length != 0)
                    {
                        foreach (var update in updates)
                        {
                            long chatId = GetChatId(update);
                            User user = GetUser(chatId);
                            if (user.IsFirstMessage)
                            {
                                messageId = await SendFirstMessageAsync(user, chatId);
                                continue;
                            }
                            MainMessage message = null;
                            if (update.Message != null)
                            {
                                message = GetMessage(update.Message.Text, out bool ifMessageExist);
                                if (message != null && message.GetType() == new Start().GetType())
                                {
                                    (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                                }
                                if (!ifMessageExist && !update.Message.From.IsBot)
                                {
                                    (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                                    await _bot.EditMessageTextAsync(chatId, messageId, "Выберите команду из предоставленных");
                                    break;
                                }
                            }
                            else
                            {
                                message = GetMessage(update.CallbackQuery.Data, out bool ifMessageExist);
                                if (message != null && message.GetType() == new Start().GetType())
                                {
                                    (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                                }
                                //ифы для записи в user
                                if (previousMessage.GetType() == new ChooseStockExchange().GetType())
                                {
                                    foreach (var stockExchange in new StockExchanges().StockExchangesList)
                                    {
                                        if (update.CallbackQuery.Data == stockExchange.Name)
                                        {
                                            user.StockExchange = stockExchange;
                                            user.Data.Clear();
                                            message = new Start();
                                            break;
                                        }
                                    }
                                }
                                else if (previousMessage.GetType() == new ChooseMarketSymbol().GetType() || previousMessage.GetType() == new Previous().GetType() || previousMessage.GetType() == new Next().GetType())
                                {
                                    if (update.CallbackQuery.Data != new Back().Message && update.CallbackQuery.Data != new Previous().Message && update.CallbackQuery.Data != new Next().Message)
                                    {
                                        foreach (var marketSymbol in await new MarketSymbols(user.StockExchange.Name).GetMarketSymbols())
                                        {
                                            string currentSymbol = DeleteMark(update.CallbackQuery.Data, Emoji.CheckMark);
                                            if (await user.StockExchange.GlobalMarketSymbolToExchangeMarketSymbolAsync(currentSymbol) == marketSymbol)
                                            {
                                                if (!CheckIfRecorded(user.Data.Select(d => d.MarketSymbol).ToList(), marketSymbol))
                                                {
                                                    user.Data.Add(new Data { MarketSymbol = marketSymbol });
                                                }
                                                message = new Start();
                                                break;
                                            }
                                        }
                                    }
                                }
                                else if (previousMessage.GetType() == new ChooseDataType().GetType())
                                {
                                    if (update.CallbackQuery.Data != new Back().Message)
                                    {
                                        string currentData = DeleteMark(update.CallbackQuery.Data, Emoji.CheckMark);
                                        foreach (var dataType in await new ChooseDataType().OnSend())
                                        {
                                            if (currentData == dataType)
                                            {
                                                if (!CheckIfRecorded(user.DataTypes, dataType))
                                                {
                                                    user.DataTypes.Add(dataType);
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
                                await EditMessageAsync(user, intermediateMessageBetweenPages, pageNumber, messageId);
                            }
                            else if (message.GetType() == new Previous().GetType())
                            {
                                --pageNumber;
                                await EditMessageAsync(user, intermediateMessageBetweenPages, pageNumber, messageId);
                            }
                            else if (message.GetType() == new Next().GetType())
                            {
                                ++pageNumber;
                                await EditMessageAsync(user, intermediateMessageBetweenPages, pageNumber, messageId);
                            }
                            else
                            {
                                pageNumber = 0;
                                await EditMessageAsync(user, message, pageNumber, messageId);
                            }
                            previousMessage = message;
                            offset = updates[updates.Length - 1].Id + 1;
                        }
                    }
                    await OnGetDataFromWebSockets();
                }
            }
            catch(Exception ex)
            {
                await _interactive.OutputAsync(ex.Message);
            }
        }
    }
}
