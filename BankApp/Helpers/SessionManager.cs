using Kurs_Bank.Models;
using System;

namespace Kurs_Bank.Helpers
{
    public static class SessionManager
    {
        private static Client _currentClient;

        public static Client CurrentClient
        {
            get
            {
                if (_currentClient == null)
                {
                    throw new InvalidOperationException("Пользователь не авторизован");
                }
                return _currentClient;
            }
            set => _currentClient = value;
        }

        public static bool IsAuthenticated => _currentClient != null;

        public static void Clear()
        {
            _currentClient = null;
        }
    }
}