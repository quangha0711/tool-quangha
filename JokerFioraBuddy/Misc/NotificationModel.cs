using System.Text.RegularExpressions;
using SharpDX;

namespace JokerFioraBuddy.Misc
{
    public class NotificationModel
    {
        private float _time;
        private float _v1;
        private float _v2;
        private string _v3;
        private Color _deepSkyBlue;

        public float StartTimer { get; set; }
        public float ShowTimer { get; set; }
        public float AnimationTimer { get; set; }
        public string ShowText { get; set; }
        public System.Drawing.Color Color { get; set; }

        public NotificationModel(float startTimer, float showTimer, float animationTimer, string showText, System.Drawing.Color color)
        {
            StartTimer = startTimer;
            ShowTimer = showTimer;
            AnimationTimer = animationTimer;
            var value = Regex.Replace(showText, ".{28}", "$0\n");
            ShowText = value;
            Color = color;
        }

        public NotificationModel(float time, float v1, float v2, string v3, Color deepSkyBlue)
        {
            _time = time;
            _v1 = v1;
            _v2 = v2;
            _v3 = v3;
            _deepSkyBlue = deepSkyBlue;
        }
    }
}
