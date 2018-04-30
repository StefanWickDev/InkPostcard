namespace ColourSliderLibrary
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Threading;

    /// <summary>
    /// See article: http://www.codeproject.com/KB/WPF/WPFColourSlider.aspx
    /// </summary>
    public class ColourSlider : Slider
    {
        private BitmapSource colourGradient;
        private object updateLock = new object();
        private bool isValueUpdating = false;
        private bool isFirstTime = true;

        static ColourSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColourSlider), new FrameworkPropertyMetadata(typeof(ColourSlider)));
        }

        public static readonly DependencyProperty SelectedColoursProperty =
            DependencyProperty.Register("SelectedColour", typeof(Color), typeof(ColourSlider),
            new UIPropertyMetadata(Colors.DarkBlue, new PropertyChangedCallback(SelectedColourChangedCallBack)));

        public ColourSlider()
        {
            //we need a good range so that there is a large number of possible colours
            this.Minimum = 0;
            this.Maximum = 1000;
            this.LargeChange = 50;
            this.SmallChange = 5;

            this.Background = new LinearGradientBrush(new GradientStopCollection() { 
                new GradientStop(Colors.Black, 0.0),
                new GradientStop(Colors.Red, 0.1),
                new GradientStop(Colors.Yellow, 0.25),
                new GradientStop(Colors.Lime, 0.4),
                new GradientStop(Colors.Aqua, 0.55),
                new GradientStop(Colors.Blue, 0.7),
                new GradientStop(Colors.Fuchsia, 0.9),
                new GradientStop(Colors.White, 0.98),
                new GradientStop(Colors.White, 1),
            });
        }

        public Color SelectedColour
        {
            get { return (Color)this.GetValue(SelectedColoursProperty); }
            set { this.SetValue(SelectedColoursProperty, value); }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (this.isFirstTime)
            {
                this.CacheBitmap();
                this.SetColour(this.SelectedColour);
                this.isFirstTime = false;
            }
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            // prevent value change occurring when we set the colour
            if (Monitor.TryEnter(this.updateLock, 0) && !this.isValueUpdating)
            {
                try
                {
                    this.isValueUpdating = true;

                    double width = this.VisualBounds.Width;
                    if (width != double.NegativeInfinity)
                    {
                        // work out the track position based on the control's width
                        int position = (int)(((newValue - base.Minimum) / (base.Maximum - base.Minimum)) * width);

                        this.SelectedColour = GetColour(this.colourGradient, position);
                    }
                }
                finally
                {
                    isValueUpdating = false;
                    Monitor.Exit(this.updateLock);
                }
            }

            base.OnValueChanged(oldValue, newValue);
        }

        protected Rect VisualBounds
        {
            get { return VisualTreeHelper.GetDescendantBounds(this); }
        }

        #region Private Methods

        private void SetColour(Color colour)
        {
            if (Monitor.TryEnter(this.updateLock, 0) && !this.isValueUpdating)
            {
                try
                {
                    Rect bounds = this.VisualBounds;
                    double currentDistance = int.MaxValue;
                    int currentPosition = -1;

                    for (int i = 0; i < bounds.Width; i++)
                    {
                        Color c = this.GetColour(this.colourGradient, i);
                        double distance = c.Distance(colour);

                        if (distance == 0.0)
                        {
                            //we cannot get a better match, break now
                            currentPosition = i;
                            break;
                        }

                        if (distance < currentDistance)
                        {
                            currentDistance = distance;
                            currentPosition = i;
                        }
                    }

                    base.Value = (currentPosition / bounds.Width) * (base.Maximum - base.Minimum);
                }
                finally
                {
                    Monitor.Exit(updateLock);
                }
            }
        }

        private Color GetColour(BitmapSource bitmap, int position)
        {
            if (position >= bitmap.Width - 1)
            {
                position = (int)bitmap.Width - 2;
            }

            CroppedBitmap cb = new CroppedBitmap(bitmap, new Int32Rect(position, (int)this.VisualBounds.Height / 2, 1, 1));
            byte[] tricolour = new byte[4];

            cb.CopyPixels(tricolour, 4, 0);
            Color c = Color.FromRgb(tricolour[2], tricolour[1], tricolour[0]);

            return c;
        }

        private void CacheBitmap()
        {
            Rect bounds = this.VisualBounds;
            RenderTargetBitmap source = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();

            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size)); 
            }

            source.Render(dv);
            this.colourGradient = source;
        }

        private static void SelectedColourChangedCallBack(DependencyObject property, DependencyPropertyChangedEventArgs args)
        {
            ColourSlider colourSlider = (ColourSlider)property;
            Color colour = (Color)args.NewValue;

            colourSlider.SetColour(colour);
        }

        #endregion
    }
}
