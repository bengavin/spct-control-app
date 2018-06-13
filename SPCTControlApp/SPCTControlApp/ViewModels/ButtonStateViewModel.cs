using GalaSoft.MvvmLight;
using Xamarin.Forms;

namespace SPCTControlApp.ViewModels
{
    public class ButtonStateViewModel : ViewModelBase
    {
        public ButtonStateViewModel(int row, int col)
        {
            Row = row;
            Column = col;
        }

        public ButtonStateViewModel() : this(0, 0) { }

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
                    BorderColor = Color.Yellow;
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
