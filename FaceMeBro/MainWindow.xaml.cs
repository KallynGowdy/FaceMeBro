using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.Win32;

namespace FaceMeBro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Endpoint = "https://eastus2.api.cognitive.microsoft.com/face/v1.0";

        private static readonly Dictionary<string, SolidColorBrush> emotionMap = new Dictionary<string, SolidColorBrush>()
        {
            {
                "Anger",
                Brushes.Red
            },
            {
                "Contempt",
                Brushes.Black
            },
            {
                "Disgust",
                Brushes.DarkOliveGreen
            },
            {
                "Fear",
                Brushes.DarkRed
            },
            {
                "Happiness",
                Brushes.Yellow
            },
            {
                "Neutral",
                Brushes.White
            },
            {
                "Sadness",
                Brushes.Blue
            },
            {
                "Surprise",
                Brushes.LawnGreen
            }
        };

        private string selectedFilePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                DefaultExt = ".jpg",
                Filter = "Photos (*.jpeg,*.png,*.gif)|*.jpeg;*.png;*.jpg;*.gif"
            };

            bool? result = dialog.ShowDialog(this);

            if (result.HasValue && result.Value)
            {
                selectedFilePath = dialog.FileName;
                var uri = new Uri(selectedFilePath);
                var img = new BitmapImage(uri);
                OutputImage.Source = img;
                OutputCanvas.Width = img.Width;
                OutputCanvas.Height = img.Height;
                ProgressLabel.Text = "Loading Faces...";
                Progress.Visibility = Visibility.Visible;
                ClearFaces();
                Task.Run(() => DetectFaces());
            }
        }

        private async void DetectFaces()
        {
            Face[] faces;
            using (FileStream stream = File.OpenRead(selectedFilePath))
            {
                using (FaceServiceClient client =
                    new FaceServiceClient(File.ReadAllText("./FaceApiSubscriptionKey.txt"), Endpoint))
                {
                    faces = await client.DetectAsync(
                        stream,
                        returnFaceId: true,
                        returnFaceLandmarks: true,
                        returnFaceAttributes: new[] { FaceAttributeType.Emotion });
                }
            }

            Dispatcher.Invoke(() =>
            {
                foreach (var face in faces)
                {
                    // Draw the face rectangle on the output canvas.
                    var rect = CalculateRectForFace(face);
                    OutputCanvas.Children.Add(rect);
                    Canvas.SetTop(rect, face.FaceRectangle.Top);
                    Canvas.SetLeft(rect, face.FaceRectangle.Left);
                }

                ProgressLabel.Text = "Wating for user input...";
                Progress.Visibility = Visibility.Collapsed;
            });
        }

        private void ClearFaces()
        {
            if (OutputCanvas.Children.Count > 1)
            {
                OutputCanvas.Children.RemoveRange(1, OutputCanvas.Children.Count - 1);
            }
        }

        private static Rectangle CalculateRectForFace(Face face)
        {
            return new Rectangle()
            {
                Height = face.FaceRectangle.Height,
                Width = face.FaceRectangle.Width,
                Fill = Brushes.Transparent,
                Stroke = CalculateColorForFace(face),
                StrokeThickness = 20
            };
        }

        private static SolidColorBrush CalculateColorForFace(Face face)
        {
            var bestEmotion = face.FaceAttributes.Emotion.ToRankedList().First();
            return emotionMap[bestEmotion.Key];
        }
    }
}
