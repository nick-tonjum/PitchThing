using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Spectrogram;
using NAudio;
using NAudio.Wave;
using Image = System.Windows.Controls.Image;
using NAudio.Dsp;
using Spectrogram;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using Window = System.Windows.Window;
using Microsoft.Win32;
using System.Windows.Forms;
using ScottPlot;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Drawing.Imaging;
using System.IO;
using ComboBox = System.Windows.Controls.ComboBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;
using Button = System.Windows.Controls.Button;
using ScottPlot.Plottable;
using Ellipse = System.Windows.Shapes.Ellipse;
using System.Collections.Concurrent;
using NAudio.Wave.SampleProviders;
using System.Diagnostics;
using System.Reflection.Metadata;
using TextBox = System.Windows.Controls.TextBox;

namespace PitchThing
{
    public partial class MainWindow : Window
    {
        Grid mainGrid;
        public static string SelectedAudioFile;
        public static Line controlLine = new Line()
        {
            Stroke = System.Windows.Media.Brushes.HotPink,
            StrokeThickness = 10,
            X1 = 0,
            X2 = 0,
            Y1 = 0,
            Y2 = 0
        };
        public static Canvas canvas;
        int MinFreq = 14000;
        int MaxFreq = 14800;
        Image image = new Image();
        Image resultimage = new Image();
        //create WPF slider with a min of 0 and a max of 100
        Slider slider = new Slider()
        {
            Minimum = 0,
            Maximum = 100,
            Value = 38,
            Width = 200,
            Height = 30,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
        Slider dbSlider = new Slider()
        {
            Minimum = 0,
            Maximum = 10,
            Value = 2.5,
            Width = 200,
            Height = 30,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };

        Slider intensitySlider = new Slider()
        {
            Minimum = 0,
            Maximum = 10,
            Value = 1,
            Width = 200,
            Height = 30,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };

        ComboBox colortype = new ComboBox()
        {
            Width = 100,
            Height = 30,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };

        System.Windows.Controls.TextBox basefreq;
        public MainWindow()
        {

            InitializeComponent();
            controlLine.MouseMove += (sender, e) =>
            {
                if (isDragging)
                {
                    controlLine.Y1 = e.GetPosition(canvas).Y;
                    controlLine.Y2 = e.GetPosition(canvas).Y;
                }
            };
            this.MinHeight = 100;
            this.MinWidth = 100;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //allow wav, flac, and mp3

            openFileDialog.Filter = "Audio files (*.wav)|*.wav;";
            if (openFileDialog.ShowDialog() == true)
            {
                string filename = openFileDialog.FileName;
                SelectedAudioFile = filename;

                (double[] audio, int sampleRate) = ReadMono(filename);

                int fftSize = 8192;
                int targetWidthPx = 50000;
                int stepSize = audio.Length / targetWidthPx;

                var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, minFreq: MinFreq, maxFreq: MaxFreq);
                //mel
                sg.Colormap = Colormap.Turbo;
                sg.Add(audio);
                //sg.SaveImage("song.png", ;
                BitmapImage bitmapImage = new BitmapImage();

                using (MemoryStream memory = new MemoryStream())
                {
                    sg.GetBitmap(intensity: 38, dB: true, dBScale: 2.5).Save(memory, ImageFormat.Png); // Assuming sg is a System.Drawing.Bitmap
                    memory.Position = 0;

                    bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                }

                image.Source = bitmapImage;
                ScrollViewer scrollViewer = new ScrollViewer();
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

                //stretch image to stretch vertically to fit the window, don't maintain aspect ratio



                Button generateButton = new Button()
                {
                    Content = "Apply",
                    Width = 100,
                    Height = 30,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    Margin = new Thickness(10, 10, 0, 0)
                };
                generateButton.Click += (sender, e) =>
                {
                    GenerateButtonClick();
                };

                Button generateButton2 = new Button()
                {
                    Content = "Apply to another file",
                    Width = 200,
                    Height = 30,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    Margin = new Thickness(10, 10, 0, 0)
                };
                generateButton2.Click += (sender, e) =>
                {
                    GenerateButtonClick(true);
                };

                canvas = new Canvas();
                ImageBrush imageBrush = new ImageBrush();
                imageBrush.ImageSource = bitmapImage;
                canvas.Background = imageBrush;
                canvas.Width = bitmapImage.PixelWidth;
                controlLine.X1 = 0;
                controlLine.X2 = bitmapImage.PixelWidth;
                controlLine.Y1 = bitmapImage.PixelHeight / 2;
                controlLine.Y2 = bitmapImage.PixelHeight / 2;
                canvas.Children.Add(controlLine);
                canvas.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                canvas.MouseDown += Canvas_MouseDown;
                canvas.MouseMove += Canvas_MouseMove;
                canvas.MouseUp += Canvas_MouseUp;
                canvas.SizeChanged += Canvas_SizeChanged;

                scrollViewer.Content = canvas;


                mainGrid = new Grid();
                //create two rows, one of which takes up 80% of the window and the other takes up 20%
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.3, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0.3, GridUnitType.Star) });
                mainGrid.Children.Add(scrollViewer);
                mainGrid.Children.Add(intensitySlider);
                basefreq = new TextBox()
                {
                    Width = 100,
                    Height = 30,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,
                    Margin = new Thickness(10, 10, 0, 0),
                    Text = "0"
                };
                mainGrid.Children.Add(basefreq);
                //mainGrid.Children.Add(colortype);
                StackPanel buttonstack = new StackPanel();
                buttonstack.Orientation = System.Windows.Controls.Orientation.Horizontal;
                buttonstack.Children.Add(generateButton);
                buttonstack.Children.Add(generateButton2);
                mainGrid.Children.Add(buttonstack);

                StackPanel imagecontrolstack = new StackPanel();
                imagecontrolstack.Orientation = System.Windows.Controls.Orientation.Horizontal;
                imagecontrolstack.Children.Add(colortype);
                imagecontrolstack.Children.Add(slider);
                imagecontrolstack.Children.Add(dbSlider);
                mainGrid.Children.Add(imagecontrolstack);
                Grid.SetRow(scrollViewer, 0);
                Grid.SetRow(imagecontrolstack, 1);
                Grid.SetRow(intensitySlider, 2);
                Grid.SetRow(basefreq, 3);
                //Grid.SetRow(dbSlider, 2);
                //Grid.SetRow(colortype, 3);
                Grid.SetRow(buttonstack, 4);
                foreach (var colortype in Colormap.GetColormapNames())
                {
                    this.colortype.Items.Add(colortype);
                }
                this.colortype.SelectedItem = "Turbo";
                colortype.SelectionChanged += (sender, e) =>
                {
                    sg.Colormap = Colormap.GetColormap(colortype.SelectedItem.ToString());
                    BitmapImage bitmapImage = new BitmapImage();

                    using (MemoryStream memory = new MemoryStream())
                    {
                        sg.GetBitmap(intensity: slider.Value, dB: true, dBScale: dbSlider.Value).Save(memory, ImageFormat.Png); // Assuming sg is a System.Drawing.Bitmap
                        memory.Position = 0;

                        bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                    }

                    image.Source = bitmapImage;
                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = bitmapImage;
                    canvas.Background = imageBrush;
                };

                slider.ValueChanged += (sender, e) =>
                {
                    BitmapImage bitmapImage = new BitmapImage();

                    using (MemoryStream memory = new MemoryStream())
                    {
                        sg.GetBitmap(intensity: slider.Value, dB: true, dBScale: dbSlider.Value).Save(memory, ImageFormat.Png); // Assuming sg is a System.Drawing.Bitmap
                        memory.Position = 0;

                        bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                    }

                    image.Source = bitmapImage;
                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = bitmapImage;
                    canvas.Background = imageBrush;
                };

                dbSlider.ValueChanged += (sender, e) =>
                {
                    BitmapImage bitmapImage = new BitmapImage();

                    using (MemoryStream memory = new MemoryStream())
                    {
                        sg.GetBitmap(intensity: slider.Value, dB: true, dBScale: dbSlider.Value).Save(memory, ImageFormat.Png); // Assuming sg is a System.Drawing.Bitmap
                        memory.Position = 0;

                        bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                    }

                    image.Source = bitmapImage;
                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = bitmapImage;
                    canvas.Background = imageBrush;
                };
                this.Content = mainGrid;
                //make the image zoomable



            }
            else
            {
                //quit the application
                System.Windows.Application.Current.Shutdown();
            }
        }

        (double[] audio, int sampleRate) ReadMono(string filePath, double multiplier = 16_000)
        {
            using var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int bytesPerSample = afr.WaveFormat.BitsPerSample / 8;
            int sampleCount = (int)(afr.Length / bytesPerSample);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => x * multiplier));
            return (audio.ToArray(), sampleRate);
        }


        private bool isDrawing = false;
        private bool isDragging = false;
        List<Ellipse> dragObjects = new List<Ellipse>();
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                isDrawing = true;
                Canvas_MouseMove(sender, e);
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = true;
                Point p = e.GetPosition(canvas);
            }

        }

        List<Ellipse> canvasPoints = new List<Ellipse>();
        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            // ... (Same as before)
            if (isDragging)
            {

            }
            if (isDrawing)
            {
                Point p = e.GetPosition(canvas);
                System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
                ellipse.Width = 10;
                ellipse.Height = 10;
                ellipse.Fill = System.Windows.Media.Brushes.Black;
                ellipse.StrokeThickness = 10;
                ellipse.Stroke = System.Windows.Media.Brushes.Black;
                ellipse.Margin = new Thickness(p.X - 5, p.Y - 5, 0, 0);
                if (canvasPoints.Count > 1)
                {
                    Ellipse previousLine = canvasPoints[canvasPoints.Count - 2];
                    if (ellipse.Margin.Left <= previousLine.Margin.Left)
                    {
                        return;
                    }
                }
                ellipse.MouseDown += (sender, e) =>
                {
                    //if left click, delete point
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        canvas.Children.Remove(ellipse);
                        canvasPoints.Remove(ellipse);
                    }
                };
                canvas.Children.Add(ellipse);
                canvasPoints.Add(ellipse);

                //create line between new point and previous point
                if (canvasPoints.Count > 1)
                {
                    Ellipse previousLine = canvasPoints[canvasPoints.Count - 2];
                    Line line = new Line();
                    line.Stroke = System.Windows.Media.Brushes.Black;
                    line.StrokeThickness = 5;
                    line.X1 = previousLine.Margin.Left + 5;
                    line.Y1 = previousLine.Margin.Top + 5;
                    line.X2 = ellipse.Margin.Left + 5;
                    line.Y2 = ellipse.Margin.Top + 5;
                    canvas.Children.Add(line);
                }
            }
        }

        public void GenerateButtonClick(bool differentfile = false)
        {

            string filetoprocess = SelectedAudioFile;
            if (differentfile)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Filter = "Audio files (*.wav)|*.wav;";
                if (openFileDialog.ShowDialog() == true)
                {
                    string filename = openFileDialog.FileName;
                    filetoprocess = filename;

                }
            }
            double controlfreq = ((((canvas.ActualHeight - controlLine.Y1) / canvas.ActualHeight) * (MaxFreq - MinFreq)) + MinFreq); ;
            Dictionary<int, double> ellipsefrequencies = new Dictionary<int, double>();
            long totalSamples = 0;

            using (WaveFileReader reader = new WaveFileReader(filetoprocess))
            {
                totalSamples = reader.SampleCount;
            }
            foreach (Ellipse ellipse in canvasPoints)
            {
                double y = ((((canvas.ActualHeight - ellipse.Margin.Top) / canvas.ActualHeight) * (MaxFreq - MinFreq)) + MinFreq);
                //convert ellipse top margin to actual position on the canvas


                ellipsefrequencies.TryAdd(Convert.ToInt32(Math.Floor((ellipse.Margin.Left / canvas.ActualWidth) * totalSamples)), y);
            };

            //create new text file named "mapping.txt"
            string appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string mappingfile = appdatapath + "\\AudioPitchThing\\mapping.txt";
            //clear file
            //delete file if it exists
            if (File.Exists(mappingfile))
            {
                File.Delete(mappingfile);
            }
            File.WriteAllText(mappingfile, String.Empty);
            //write to file

            double newbasefreq = controlfreq;
            if (basefreq.Text != "0")
            {
                newbasefreq = Convert.ToDouble(basefreq.Text);
            }

            foreach (var kvp in ellipsefrequencies)
            {
                double shiftamount = (controlfreq - kvp.Value) * intensitySlider.Value;
                double newoutputline = newbasefreq + shiftamount;
                double semitones = (12 * Math.Log((newbasefreq) / (newoutputline), 2));
                //double intensity2 = (controlfreq - (kvp.Value + Convert.ToDouble(basefreq.Text))) * intensitySlider.Value;
                //intensity2 = controlfreq - intensity2;
                //double semitones = (12 * Math.Log((intensity2) / controlfreq, 2));
                File.AppendAllText(mappingfile, kvp.Key + " " + semitones + Environment.NewLine);
            }
            string rubberbandlocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\rubberband\\rubberband.exe";
            //rubberband.exe --pitchmap C:\Users\nicho\AppData\Roaming\AudioPitchThing\mapping703.txt "D:\Downloads\queenlive74\WQ.wav" "D:\Downloads\queenlive74\1598loth.wav" --ignore-clipping
            string arguments = "--pitchmap \"" + mappingfile + "\" \"" + filetoprocess + "\" \"" + appdatapath + "\\AudioPitchThing\\output_file.wav\"" + " --ignore-clipping";
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(rubberbandlocation, arguments);
            process.StartInfo = startInfo;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();



            (double[] audio, int sampleRate) = ReadMono(appdatapath + "\\AudioPitchThing\\output_file.wav");

            int fftSize = 8192;
            int targetWidthPx = 50000;
            int stepSize = audio.Length / targetWidthPx;

            var sg = new SpectrogramGenerator(sampleRate, fftSize, stepSize, minFreq: MinFreq, maxFreq: MaxFreq);
            sg.Colormap = Colormap.Turbo;
            sg.Add(audio);

            BitmapImage bitmapImage = new BitmapImage();

            using (MemoryStream memory = new MemoryStream())
            {
                sg.GetBitmap(intensity: 38, dB: true, dBScale: 2.5).Save(memory, ImageFormat.Png); // Assuming sg is a System.Drawing.Bitmap
                memory.Position = 0;

                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }

            resultimage.Source = bitmapImage;

            ScrollViewer resultscrollViewer = new ScrollViewer();
            resultscrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            resultscrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

            Canvas resultcanvas = new Canvas();
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = bitmapImage;
            resultcanvas.Background = imageBrush;
            resultcanvas.Width = bitmapImage.PixelWidth;
            resultcanvas.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            resultscrollViewer.Content = resultcanvas;

            mainGrid.Children.Add(resultscrollViewer);
            Grid.SetRow(resultscrollViewer, 5);




        }


        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // ... (Same as before)
            if (e.ChangedButton == MouseButton.Right)
            {
                isDrawing = false;
            }
            if (e.ChangedButton == MouseButton.Left)
            {
                isDragging = false;
                dragObjects.Clear();
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Adjust the drawings based on window size changes

            foreach (var child in canvas.Children)
            {
                try
                {
                    if (child is Line line)
                    {
                        line.X1 *= e.NewSize.Width / e.PreviousSize.Width;
                        line.X2 *= e.NewSize.Width / e.PreviousSize.Width;
                        line.Y1 *= e.NewSize.Height / e.PreviousSize.Height;
                        line.Y2 *= e.NewSize.Height / e.PreviousSize.Height;
                    }
                    else if (child is Ellipse ellipse)
                    {
                        ellipse.Margin = new Thickness(ellipse.Margin.Left * e.NewSize.Width / e.PreviousSize.Width,
                                                   ellipse.Margin.Top * e.NewSize.Height / e.PreviousSize.Height, 0, 0);
                    }
                }
                catch
                {

                }
            }
        }


    }
}
