using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Saplin.xOPS.UI.Misc;
using SkiaSharp;
using Xamarin.Forms;

namespace dopetest_xamarin
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }


        volatile bool breakTest = false;
        private volatile bool _invalidating;
        private TaskCompletionSource<bool> _taskCompletionSource;
        private SKColorF _textColor;
        private float _rotation;
        private SKPoint _point;
        private bool _resetCanvas;
        private string _dopesText;
        const int max = 600;

        void StartTestMT()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = absolute.Width;
            var height = absolute.Height;

            var i = 0;
            var processed = 0;

            var thread = new Thread(() =>
            {
                while (true)
                {
                    if (processed < i - 20)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    var label = new Label()
                    {
                        Text = "Dope",
                        TextColor = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()),
                        Rotation = rand.NextDouble() * 360
                    };

                    AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(label, new Rectangle(rand.NextDouble(), rand.NextDouble(), 80, 24));

                    absolute.Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        if (i > max)
                        {
                            absolute.Children.RemoveAt(0);
                        }

                        absolute.Children.Add(label);

                        processed++;
                    });

                    if (breakTest) break;

                    i++;
                }
            });

            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();

            var sw = new Stopwatch();
            sw.Start();
            long prevTicks = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;

            Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                if (stop.IsVisible)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return false;
                }

                var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                prevTicks = sw.ElapsedTicks;
                prevProcessed = processed;

                if (i > max)
                {
                    avgSum += r;
                    avgN++;
                }

                return true;
            });
        }

        void StartTestMT2()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = absolute.Width;
            var height = absolute.Height;

            const int step = 75;

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();

            var bankA = new Label[step];
            var bankB = new Label[step];

            Action<Label[]> addLabels = (Label[] labels) =>
            {
                for (int k = 0; k < step; k++)
                {
                    var label = new Label()
                    {
                        Text = "Dope",
                        TextColor = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()),
                        Rotation = rand.NextDouble() * 360
                    };

                    AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(label, new Rectangle(rand.NextDouble(), rand.NextDouble(), 80, 24));

                    labels[k] = label;
                }
            };

            addLabels(bankA);
            addLabels(bankB);

            var bank = bankA;

            Action loop = null;

            var i = 0;
            Task task = null;

            loop = () =>
            {
                if (breakTest)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return;
                }

                if (processed > max)
                {
                    absolute.Children.RemoveAt(0);
                }

                absolute.Children.Add(bank[i]);
                i++;

                if (i == step)
                {
                    if (task != null && task.Status != TaskStatus.RanToCompletion) task.Wait();
                    task = Task.Run(() => addLabels(bank));
                    if (bank == bankA) bank = bankB; else bank = bankA;
                    i = 0;
                }

                processed++;

                if (sw.ElapsedMilliseconds - prevMs > 500)
                {

                    var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                    prevTicks = sw.ElapsedTicks;
                    prevProcessed = processed;

                    if (processed > max)
                    {
                        dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                        avgSum += r;
                        avgN++;
                    }

                    prevMs = sw.ElapsedMilliseconds;
                }

                Device.BeginInvokeOnMainThread(loop);
            };

            sw.Start();


            Device.BeginInvokeOnMainThread(loop);
        }

        void StartTestST()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = absolute.Width;
            var height = absolute.Height;

            //const int step = 20;
            //var labels = new Label[step * 2];

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();

            Action loop = null;

            loop = () =>
            {
                var now = sw.ElapsedMilliseconds;

                if (breakTest)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return;
                }

                //60hz, 16ms to build the frame
                while (sw.ElapsedMilliseconds - now < 16)
                {
                    var label = new Label()
                    {
                        Text = "Dope",
                        TextColor = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()),
                        Rotation = rand.NextDouble() * 360
                    };

                    AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(label, new Rectangle(rand.NextDouble(), rand.NextDouble(), 80, 24));

                    if (processed > max)
                    {
                        absolute.Children.RemoveAt(0);
                    }

                    absolute.Children.Add(label);

                    processed++;

                    if (sw.ElapsedMilliseconds - prevMs > 500)
                    {

                        var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                        prevTicks = sw.ElapsedTicks;
                        prevProcessed = processed;

                        if (processed > max)
                        {
                            dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            avgSum += r;
                            avgN++;
                        }

                        prevMs = sw.ElapsedMilliseconds;
                    }
                }

                Device.BeginInvokeOnMainThread(loop);
            };

            sw.Start();

            Device.BeginInvokeOnMainThread(loop);
        }

        void StartTestGridST()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = grid.Width;
            var height = grid.Height;

            //const int step = 20;
            //var labels = new Label[step * 2];

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();

            Action loop = null;

            loop = () =>
            {
                if (breakTest)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return;
                }

                var now = sw.ElapsedMilliseconds;

                //60hz, 16ms to build the frame
                while (sw.ElapsedMilliseconds - now < 16)
                {
                    var label = new Label()
                    {
                        Text = "Dope",
                        TextColor = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()),
                        Rotation = rand.NextDouble() * 360,
                        TranslationX = rand.NextDouble() * width,
                        TranslationY = rand.NextDouble() * height
                    };


                    if (processed > max)
                    {
                        grid.Children.RemoveAt(0);
                    }

                    grid.Children.Add(label);

                    processed++;

                    if (sw.ElapsedMilliseconds - prevMs > 500)
                    {

                        var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                        prevTicks = sw.ElapsedTicks;
                        prevProcessed = processed;

                        if (processed > max)
                        {
                            //dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            _dopesText = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            avgSum += r;
                            avgN++;
                        }

                        prevMs = sw.ElapsedMilliseconds;
                    }
                }

                Device.BeginInvokeOnMainThread(loop);
            };

            sw.Start();

            Device.BeginInvokeOnMainThread(loop);
        }

        void StartTestCanvasST()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = absolute.Width;
            var height = absolute.Height;

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();

            Action loop = null;

            //sw.Start();
            //await InvalidateCanvas();
            //sw.Stop();
            //Debug.WriteLine("ELAPSED: " + sw.Elapsed.TotalMilliseconds);
            //return;

            loop = async () =>
            {
                var now = sw.ElapsedMilliseconds;

                if (breakTest)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return;
                }

                //60hz, 16ms to build the frame
                while (sw.ElapsedMilliseconds - now < 16)
                {
                    _textColor = new SKColorF(rand.NextFloat(), rand.NextFloat(), rand.NextFloat());
                    _rotation = rand.NextFloat() * 360;
                    var x = rand.NextFloat();
                    var y = rand.NextFloat();
                    _point = new SKPoint(x, y);

                    await InvalidateCanvas();

                    processed++;

                    if (sw.ElapsedMilliseconds - prevMs > 500)
                    {

                        var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                        prevTicks = sw.ElapsedTicks;
                        prevProcessed = processed;

                        if (processed > max)
                        {
                            dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            avgSum += r;
                            avgN++;
                        }

                        prevMs = sw.ElapsedMilliseconds;
                    }
                }

                Device.BeginInvokeOnMainThread(loop);
            };

            sw.Start();

            Device.BeginInvokeOnMainThread(loop);
        }

        async void StartTestCanvasST2()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = absolute.Width;
            var height = absolute.Height;

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();
            sw.Start();

            while (!breakTest)
            {
                var now = sw.ElapsedMilliseconds;

                    _textColor = new SKColorF(rand.NextFloat(), rand.NextFloat(), rand.NextFloat());
                    _rotation = rand.NextFloat() * 360;
                    var x = rand.NextFloat();
                    var y = rand.NextFloat();
                    _point = new SKPoint(x, y);

                    await InvalidateCanvas();

                    processed++;

                    if (sw.ElapsedMilliseconds - prevMs > 500)
                    {

                        var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                        prevTicks = sw.ElapsedTicks;
                        prevProcessed = processed;

                        if (processed > max)
                        {
                            dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            avgSum += r;
                            avgN++;
                        }

                        prevMs = sw.ElapsedMilliseconds;
                    }
            }


            if (breakTest)
            {
                var avg = avgSum / avgN;
                dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                return;
            }

        }

        void StartTestChangeST()
        {
            var rand = new Random2(0);

            breakTest = false;

            var width = grid.Width;
            var height = grid.Height;

            const int step = 20;
            var labels = new Label[step * 2];

            var processed = 0;

            long prevTicks = 0;
            long prevMs = 0;
            int prevProcessed = 0;
            double avgSum = 0;
            int avgN = 0;
            var sw = new Stopwatch();

            var texts = new string[] { "dOpe", "Dope", "doPe", "dopE" };

            Action loop = null;

            loop = () =>
            {
                if (breakTest)
                {
                    var avg = avgSum / avgN;
                    dopes.Text = string.Format("{0:0.00} Dopes/s (AVG)", avg).PadLeft(21);
                    return;
                }


                var now = sw.ElapsedMilliseconds;

                //60hz, 16ms to build the frame
                while (sw.ElapsedMilliseconds - now < 16)
                {

                    var label = new Label()
                    {
                        Text = "Dope",
                        TextColor = new Color(rand.NextDouble(), rand.NextDouble(), rand.NextDouble()),
                        Rotation = rand.NextDouble() * 360
                    };

                    AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(label, new Rectangle(rand.NextDouble(), rand.NextDouble(), 80, 24));

                    if (processed > max)
                    {
                        (absolute.Children[processed % max] as Label).Text = texts[(int)Math.Floor(rand.NextDouble() * 4)];
                    }
                    else absolute.Children.Add(label);

                    processed++;

                    if (sw.ElapsedMilliseconds - prevMs > 500)
                    {

                        var r = (double)(processed - prevProcessed) / ((double)(sw.ElapsedTicks - prevTicks) / Stopwatch.Frequency);
                        prevTicks = sw.ElapsedTicks;
                        prevProcessed = processed;

                        if (processed > max)
                        {
                            dopes.Text = string.Format("{0:0.00} Dopes/s", r).PadLeft(15);
                            avgSum += r;
                            avgN++;
                        }

                        prevMs = sw.ElapsedMilliseconds;
                    }
                }

                Device.BeginInvokeOnMainThread(loop);
            };

            sw.Start();

            Device.BeginInvokeOnMainThread(loop);
        }

        private void SetControlsAtStart()
        {
            startChangeST.IsVisible = startST.IsVisible = startGridST.IsVisible = startCanvasST.IsVisible = false;
            stop.IsVisible = dopes.IsVisible = true;
            absolute.Children.Clear();
            grid.Children.Clear();
            ClearCanvas();


            dopes.Text = "Warming up..";
        }

        private void ClearCanvas()
        {
            _resetCanvas = true;
            _taskCompletionSource = null;
            canvasview.InvalidateSurface();
        }

        void startMT_Clicked(System.Object sender, System.EventArgs e)
        {
            SetControlsAtStart();
            StartTestMT2();
        }

        void startST_Clicked(System.Object sender, System.EventArgs e)
        {
            SetControlsAtStart();
            StartTestST();
        }


        void startGridST_Clicked(System.Object sender, System.EventArgs e)
        {
            SetControlsAtStart();
            StartTestGridST();
        }

        void startChangeST_Clicked(System.Object sender, System.EventArgs e)
        {
            SetControlsAtStart();
            StartTestChangeST();
        }

        void startCanvasST_Clicked(System.Object sender, System.EventArgs e)
        {
            SetControlsAtStart();
            StartTestCanvasST();
        }

        void Stop_Clicked(System.Object sender, System.EventArgs e)
        {
            breakTest = true;
            stop.IsVisible = false;
            startChangeST.IsVisible = startST.IsVisible = startGridST.IsVisible = startCanvasST.IsVisible = true;
        }

        private Task InvalidateCanvas()
        {
            _taskCompletionSource = new TaskCompletionSource<bool>();
            Device.BeginInvokeOnMainThread(() => canvasview.InvalidateSurface());
            return _taskCompletionSource.Task;
        }

        void canvasview_PaintSurface(System.Object sender, SkiaSharp.Views.Forms.SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            if (_resetCanvas)
            {
                canvas.Clear();
                _resetCanvas = false;
                return;
            }

            SKPaint paint = new SKPaint
            {
                TextSize = 36,
                Color = (SKColor)_textColor
            };

            var point = new SKPoint(_point.X * info.Size.Width, _point.Y * info.Size.Height);

            canvas.RotateDegrees(_rotation, point.X, point.Y);
            canvas.DrawText("Dope", point, paint);

            _taskCompletionSource?.SetResult(true);
        }
    }
}
