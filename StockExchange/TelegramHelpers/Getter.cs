using System;
using System.Collections.Generic;
using System.Text;
using StockExchange.Messages;
using StockExchange.TelegramBot;

namespace StockExchange.TelegramHelpers
{
    public static class Getter
    {
        public static long GetChatId(Telegram.Bot.Types.Update update)
        {
            if(update == null)
            {
                throw new ArgumentNullException();
            }
            return update.Message == null ? update.CallbackQuery.From.Id : update.Message.Chat.Id;
        }

        public static User GetUser(long chatId, List<User> users)
        {
            if(users == null)
            {
                throw new ArgumentNullException();
            }
            foreach (var user in users)
            {
                if (user.ChatId == chatId)
                {
                    return user;
                }
            }
            User newUser;
            users.Add(newUser = new User(chatId));
            return newUser;
        }

        //Determine the type of command.And if command exist
        public static MainMessage GetMessage(string text, out bool ifMessageExist, List<MainMessage> messages)
        {
            if(text == null || messages == null)
            {
                throw new ArgumentNullException();
            }
            ifMessageExist = false;
            foreach (var message in messages)
            {
                if (message.Message == text)
                {
                    ifMessageExist = true;
                    return message;
                }
            }
            return null;
        }
    }
}
