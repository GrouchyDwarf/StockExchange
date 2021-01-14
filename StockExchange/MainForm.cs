using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExchangeSharp;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockExchange
{
    public partial class MainForm : Form
    {
        private readonly List<ExchangeAPI> _stockExchanges;
        private Telegram.Bot.TelegramBotClient _bot;
        private ExchangeAPI _stockExchange;
        private string _globalSymbol;
        private Telegram.Bot.Types.Message _lastSentMessage;
        private string _dataType;
        private List<string> _dataTypes;
        public MainForm()
        {
            InitializeComponent();
            _stockExchanges = new List<ExchangeAPI>();
            _stockExchanges.Add(new ExchangeBinanceAPI());
            _stockExchanges.Add(new ExchangeBitfinexAPI());
            _stockExchanges.Add(new ExchangeBittrexAPI());
            _stockExchanges.Add(new ExchangeCoinbaseAPI());
            _stockExchanges.Add(new ExchangePoloniexAPI());
            _dataTypes = new List<string>();
            _dataTypes.Add("Trades");
            _dataTypes.Add("Tickers");
            _dataTypes.Add("Candles");
        }
        private async void RunButton_Click(object sender, EventArgs e)
        {
            var key = TextBox.Text;
            try
            {
                _bot = new Telegram.Bot.TelegramBotClient(key);
                await _bot.SetWebhookAsync("");

                var keyboardButtons = new List<List<KeyboardButton>>();
                keyboardButtons.Add(new List<KeyboardButton> { new KeyboardButton("Выберите биржу") });
                keyboardButtons.Add(new List<KeyboardButton> { new KeyboardButton("Выберите символ") });
                keyboardButtons.Add(new List<KeyboardButton> { new KeyboardButton("Выберите данные") });
                keyboardButtons.Add(new List<KeyboardButton> { new KeyboardButton("Следить") });

                var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                {
                    Keyboard = keyboardButtons
                };
                //long chatId = 764606140;
                
                Update[] firstUpdate = new Update[0];
                int offset = 0;
                long chatId = 0;
                while (true)
                {
                    firstUpdate = await _bot.GetUpdatesAsync(0);
                    if (firstUpdate.Length != 0)
                    {
                        chatId = firstUpdate[0].Message.Chat.Id;
                        break;
                    }
                }
                await _bot.SendTextMessageAsync(chatId, "Главная", replyMarkup:keyboard);
                if (firstUpdate.Length >= 1)
                {
                    offset = firstUpdate[firstUpdate.Length - 1].Id + 1;
                }
                while (true)
                {
                    var updates = await _bot.GetUpdatesAsync(offset);
                    foreach(var update in updates)
                    {
                        var message = update.Message;
                        var botMessage = "";
                        if (message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
                        {
                            List<List<KeyboardButton>> buttons = new List<List<KeyboardButton>>();
                            if(message.Text == "Выберите биржу")
                            {
                                foreach (var stockExchange in _stockExchanges)
                                {
                                    buttons.Add(new List<KeyboardButton> { new KeyboardButton(stockExchange.Name) });
                                }
                                
                                keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                                {
                                    Keyboard = buttons
                                };
                                botMessage = "Выберите биржу!";
                            }
                            else if(message.Text == "Выберите символ")
                            {
                                if (_stockExchange != null)
                                {
                                    var marketSymbols = await ExchangeAPI.GetExchangeAPI(_stockExchange.Name).GetMarketSymbolsAsync();
                                    foreach (var marketSymbol in marketSymbols)
                                    {
                                        buttons.Add(new List<KeyboardButton> { new KeyboardButton(await _stockExchange.ExchangeMarketSymbolToGlobalMarketSymbolAsync(marketSymbol)) });
                                    }
                                    keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                                    {
                                        Keyboard = buttons
                                    };
                                    botMessage = "Выберите символ!";
                                }
                                else
                                {
                                    botMessage = "Сначала определитесь с биржой!";
                                }
                            }
                            else if(message.Text == "Выберите данные")
                            {
                                foreach(var dateType in _dataTypes)
                                {
                                    buttons.Add(new List<KeyboardButton> { new KeyboardButton(dateType)});
                                }
                                keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                                {
                                    Keyboard = buttons
                                };
                                botMessage = "Выберите данные!";
                            }
                            else if(message.Text == "Следить")
                            {
                                using var socket = await _stockExchange.ConnectWebSocketAsync(_stockExchange.BaseUrlWebSocket,
                                async (IWebSocket isocket, byte[] info) => { await _bot.SendTextMessageAsync(chatId, $"{info}"); });
                                if (_stockExchange == null || _globalSymbol == null || _dataType == null)
                                {
                                    botMessage = "Сначала определитесь с биржой,данными и глобальным символом";
                                }
                                else
                                {
                                    if (_dataType == "Trades")
                                    {
                                        /*var trades = */await _stockExchange.GetTradesWebSocketAsync(async trade =>
                                       {
                                           if (trade.Key == _globalSymbol)
                                           {
                                               var tradeMessage = $"Ticker {trade.Key}\nPrice:{trade.Value.Price}; Amount:{trade.Value.Amount}";
                                               if (_lastSentMessage == null || !_lastSentMessage.Text.Contains(_globalSymbol))
                                               {
                                                   _lastSentMessage = await _bot.SendTextMessageAsync(chatId, tradeMessage);
                                               }
                                               else
                                               {
                                                   _lastSentMessage = await _bot.EditMessageTextAsync(chatId, _lastSentMessage.MessageId, tradeMessage);
                                               }
                                           }
                                       }/*, await _stockExchange.GlobalMarketSymbolToExchangeMarketSymbolAsync(_globalSymbol)*/);
                                    }
                                    else if (_dataType == "Tickers")
                                    {
                                        /*var _ticker = */await _stockExchange.GetTickersWebSocketAsync(async tickers => 
                                        {
                                            var tickerMessage = "";
                                            foreach (var ticker in tickers)
                                            {
                                                if (ticker.Key == _globalSymbol)
                                                {
                                                    tickerMessage = $"Ticker {ticker.Key}; Value: {ticker.Value}";
                                                    if (_lastSentMessage == null || !_lastSentMessage.Text.Contains(ticker.Key))
                                                    {
                                                        _lastSentMessage = await _bot.SendTextMessageAsync(chatId, tickerMessage);
                                                    }
                                                    else
                                                    {
                                                        _lastSentMessage = await _bot.EditMessageTextAsync(chatId, _lastSentMessage.MessageId, tickerMessage);
                                                    }
                                                }
                                            }
                                        }/*, await _stockExchange.GlobalMarketSymbolToExchangeMarketSymbolAsync(_globalSymbol)*/);
                                    }
                                    else if(_dataType == "Candles")
                                    {
                                        var candle = new MarketCandle();
                                        await _stockExchange.GetTradesWebSocketAsync(async trade =>
                                        {    
                                            if (_globalSymbol == trade.Key)
                                            {
                                                if (candle.ExchangeName == _globalSymbol)
                                                {
                                                    candle.HighPrice = Math.Max(candle.HighPrice, trade.Value.Price);
                                                    candle.LowPrice = Math.Min(candle.LowPrice, trade.Value.Price);
                                                    candle.ClosePrice = trade.Value.Price;
                                                    _lastSentMessage = await _bot.EditMessageTextAsync(chatId, _lastSentMessage.MessageId, $"Exchange Name:{candle.ExchangeName};" +
                                                        $"Open Price:{candle.OpenPrice}; Low Price: {candle.LowPrice}; High Price: {candle.HighPrice}; Close Price: {candle.ClosePrice}");
                                                }
                                                else
                                                {
                                                    candle.ExchangeName = trade.Key;
                                                    candle.OpenPrice = trade.Value.Price;
                                                    candle.LowPrice = trade.Value.Price;
                                                    _lastSentMessage = await _bot.SendTextMessageAsync(chatId, $"Exchange Name:{candle.ExchangeName};" +
                                                        $"Open Price:{candle.OpenPrice}");
                                                }
                                            }
                                        });
                                    }
                                }
                            }
                            buttons.Add(new List<KeyboardButton> { new KeyboardButton("Назад") });
                            if(message.Text == "Назад")
                            {
                                botMessage = "Главная";
                                keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                                {
                                    Keyboard = keyboardButtons
                                };
                            }
                            if (botMessage != "")
                            {
                                await _bot.SendTextMessageAsync(chatId, botMessage, replyMarkup: keyboard);
                            }
                            if(_dataType != null || _dataType != message.Text)
                            {
                               foreach(var dataType in _dataTypes)
                                {
                                    if(message.Text == dataType)
                                    {
                                        _dataType = message.Text;
                                    }
                                } 
                            }
                            if (_stockExchange == null || _stockExchange.Name != message.Text)
                            {
                                foreach (var stockExchange in _stockExchanges)
                                {
                                    if (message.Text == stockExchange.Name)
                                    {
                                        _stockExchange = stockExchange;
                                        break;
                                    }
                                }
                            }
                            if(_stockExchange != null && (_globalSymbol == null || _globalSymbol != message.Text))
                            {
                                var marketSymbols = await ExchangeAPI.GetExchangeAPI(_stockExchange.Name).GetMarketSymbolsAsync();
                                foreach (var marketSymbol in marketSymbols)
                                {
                                    var marketSymbolConverted = await _stockExchange.ExchangeMarketSymbolToGlobalMarketSymbolAsync(marketSymbol);
                                    if (message.Text == marketSymbolConverted)
                                    {
                                        _globalSymbol = marketSymbolConverted;
                                        break;
                                    }
                                }
                            }
                        }
                        offset = update.Id + 1;
                    }
                }


                //bot.OnCallbackQuery += async (object src, Telegram.Bot.Args.CallbackQueryEventArgs ev) =>
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException exception)
            {
                TextBox.Text = exception.Message;
            }
            catch(Exception exception)
            {
                TextBox.Text = exception.Message;
            }
        }

        
    }
}











/*
private async void BotOnCallbackQueryReceived(object src, Telegram.Bot.Args.CallbackQueryEventArgs ev)
{
    //var message = ev.CallbackQuery.Message;
    if (ev.CallbackQuery.Data == "Binance")
    {
        int count = _stockExchanges.Count;
        var buttons = new List<List<KeyboardButton>>();
        foreach (var stockExchange in _stockExchanges)
        {
            buttons.Add(new List<KeyboardButton> { new KeyboardButton(stockExchange.Name) });
        }
        var stockExchangesKeyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
        {
            Keyboard = buttons
        };
        var updates = await _bot.GetUpdatesAsync(0);
        await _bot.SendTextMessageAsync(updates[0].Message.Chat.Id, "Выберите биржу!", replyMarkup: stockExchangesKeyboard);
    }
    else if (ev.CallbackQuery.Data == "Выберите символ")
    {

    }
    else if (ev.CallbackQuery.Data == "Выберите данные")
    {

    }
}
*/