using GalaSoft.MvvmLight;
using Xamarin.Forms;

namespace SPCTControlApp.ViewModels
{
    public class ButtonStateViewModel : ViewModelBase
    {
        // Give a decent ascending range of colors (greenish / pink leaning)
        private const int colorStep = 0xFFFFFF / 19;

        public ButtonStateViewModel(int row, int col, int color)
        {
            Row = row;
            Column = col;
            int colorVal = 0 + colorStep * color;
            Color = new Color((colorVal & 0xFF) / 255f, ((colorVal >> 8) & 0xFF) / 255f, ((colorVal >> 16) & 0xFF) / 255f, 0.8);
        }

        public ButtonStateViewModel() : this(0, 0, 0) { }

        public int Row { get; private set; }
        public int Column { get; private set; }

        private bool _isOn;
        public bool IsOn
        {
            get
            {
                return _isOn;
            }
            set
            {
                Set(() => IsOn, ref _isOn, value);
                if (value)
                {
                    BorderColor = Color.Black;
                }
                else
                {
                    BorderColor = Color;
                }
            }
        }

        private Color _color;
        public Color Color { get { return _color; } set { Set(() => Color, ref _color, value); } }

        private Color _borderColor;
        public Color BorderColor { get { return _borderColor; } set { Set(() => BorderColor, ref _borderColor, value); } }
    }
}
