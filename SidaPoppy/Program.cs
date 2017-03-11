using System;
using EloBuddy;
using EloBuddy.SDK.Events;
namespace Sida
{
    class Program
    {
        public static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Poppy)
            {
                return;
            }
            Config.MyMenu();
            ModeControler.ModeManager();
        }
    }
}
