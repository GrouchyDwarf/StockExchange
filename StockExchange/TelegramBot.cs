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
using StockExchange.TelegramHelpers;
using StockExchange.Helpers;

namespace StockExchange
{
    public class TelegramBot
    {
        private readonly IInteractive _interactive;
        private readonly string _key;
        private readonly TelegramBotClient _bot;
        private readonly List<User> _users;
        private readonly List<MainMessage> _messages;//bot states
        private readonly Timer _timer;
        private bool _timeToUpdate;
        private readonly bool _ifTimerStarted;
        private readonly double _periodUpdate;
        private readonly int _candlesLimit;
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
            _timeToUpdate = true;
            _periodUpdate = 10000;
            _timer = new Timer(_periodUpdate);
            _timer.Elapsed += (source, elapsedEventArgs) => _timeToUpdate = true;
            _ifTimerStarted = false;
            _candlesLimit = 10;
            _candlesCounter = 0;
        }

        private async Task<int> SendChangingMessageAsync(long chatId)
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
            return await SendChangingMessageAsync(chatId);
        }

        //one page
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
                inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(partButtons));
                await _bot.SendTextMessageAsync(user.ChatId, message.Message, replyMarkup: inlineKeyboard);
            }
        }

        //several pages
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
            inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(partButtons));
            await _bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
            await _bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, replyMarkup: inlineKeyboard);
        }

        private async Task EditMessageAsync(User user, MainMessage message, int pageNumber, int messageId)
        {
            InlineKeyboardMarkup inlineKeyboard = null;
            var isOversizeMessage = false;
            if (user.StockExchange == null && (message.Message == new Start().Message || message.Message == new Back().Message))
            {
                inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(new List<string>() { new ChooseStockExchange().Message }));
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
                    buttons = Mark.MarkStrings(buttons, globalSelectedSymbols, Emoji.CheckMark);
                }
                else if (user.DataTypes.Count > 0 && message.GetType() == new ChooseDataType().GetType())
                {
                    buttons = Mark.MarkStrings(buttons, user.DataTypes, Emoji.CheckMark);
                }
                else if (user.StockExchange != null && message.GetType() == new ChooseStockExchange().GetType())
                {
                    buttons = Mark.MarkStrings(buttons, new List<string>() { user.StockExchange.Name }, Emoji.CheckMark);
                }
                if (buttons.Count > limit)
                {

                    await EditOversizeMessageAsync(buttons, limit, pageNumber, inlineKeyboard, user, message, messageId);
                    isOversizeMessage = true;
                }
                else
                {
                    inlineKeyboard = new InlineKeyboardMarkup(ButtonConverter.ButtonNamesToInlineButtons(buttons));
                }
            }
            if (!isOversizeMessage)
            {
                await _bot.EditMessageTextAsync(user.ChatId, messageId, message.Message);
                await _bot.EditMessageReplyMarkupAsync(user.ChatId, messageId, inlineKeyboard);
            }
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
                                if (data.Candles == null || _candlesLimit < _candlesCounter)
                                {
                                    data.Candles = new Stack<MarketCandle>();
                                    _candlesCounter = 0;
                                }
                                if (_timeToUpdate || data.Candles.Count == 0)
                                {
                                    if (data.Candles.Count == 0)
                                    {
                                        _timer.Start();
                                    }
                                    if (!_ifTimerStarted)
                                    {
                                        _timer.Enabled = true;
                                    }
                                    data.Candles.Push(new MarketCandle() { ExchangeName = "Нет покупок в этот период"});
                                    _timeToUpdate = false;
                                    ++_candlesCounter;
                                }
                                var candle = data.Candles.Peek();
                                await user.StockExchange.GetTradesWebSocketAsync(async trade =>
                                {
                                    if (candle == data.Candles.Peek())//synchronization
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
                            if (data.Candles != null)
                            {
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

        private async Task<(int messageId, int offset)> StartNewWebsocket(Update update, User user, long chatId, Update[] updates, int oldMessageId)
        {
            await _bot.DeleteMessageAsync(chatId, oldMessageId);
            int messageId = await SendChangingMessageAsync(chatId);
            int offset = updates[updates.Length - 1].Id + 1;
            user.StockExchange = null;
            user.DataTypes.Clear();
            user.Data.Clear();
            return (messageId, offset);
        }

        public async Task Run()
        {
            try
            {
                int offset = 0;
                #region Set offset considering old updates
                var oldUpdates = await _bot.GetUpdatesAsync(0);
                int pageNumber = 0;
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
                        var update = updates[0];
                        long chatId = Getter.GetChatId(update);
                        User user = Getter.GetUser(chatId, _users);
                        if (user.IsFirstMessage)
                        {
                            messageId = await SendFirstMessageAsync(user, chatId);
                            continue;
                        }
                        MainMessage message = null;
                        if (update.Message != null)
                        {
                            message = Getter.GetMessage(update.Message.Text, out bool ifMessageExist, _messages);
                            if (message != null && message.GetType() == new Start().GetType())
                            {
                                (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                            }
                            if (!ifMessageExist && !update.Message.From.IsBot)
                            {
                                (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                                await _bot.EditMessageTextAsync(chatId, messageId, "Выберите команду из предоставленных");
                                continue;
                            }
                        }
                        else
                        {
                            message = Getter.GetMessage(update.CallbackQuery.Data, out bool ifMessageExist, _messages);
                            if (message != null && message.GetType() == new Start().GetType())
                            {
                                (messageId, offset) = await StartNewWebsocket(update, user, chatId, updates, messageId);
                            }
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
                                        string currentSymbol = Mark.DeleteMark(update.CallbackQuery.Data, Emoji.CheckMark);
                                        if (await user.StockExchange.GlobalMarketSymbolToExchangeMarketSymbolAsync(currentSymbol) == marketSymbol)
                                        {
                                            if (!Recorder.CheckIfRecorded(user.Data.Select(d => d.MarketSymbol).ToList(), marketSymbol))
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
                                    string currentData = Mark.DeleteMark(update.CallbackQuery.Data, Emoji.CheckMark);
                                    foreach (var dataType in await new ChooseDataType().OnSend())
                                    {
                                        if (currentData == dataType)
                                        {
                                            if (!Recorder.CheckIfRecorded(user.DataTypes, dataType))
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
                    await OnGetDataFromWebSockets();
                }
            }
            catch (Exception ex)
            {
                await _interactive.OutputAsync(ex.Message);
            }
        }
    }
}
