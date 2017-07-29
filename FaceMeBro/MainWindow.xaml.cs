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

        /// <summary>
        /// A dictionary that maps emotions to colors.
        /// </summary>
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
                Task.Run(() => DetectFaces(selectedFilePath));
            }
        }

        /// <summary>
        /// Asynchronously detects faces in the selected image.
        /// </summary>
        /// <param name="filePath">The file path to the image that the faces should be detected from.</param>
        private async Task DetectFaces(string filePath)
        {
            var faces = await LoadFaces(filePath);
            Dispatcher.Invoke(() => { DisplayFaces(faces); });
        }

        /// <summary>
        /// Displays the given array of faces on the image.
        /// </summary>
        /// <param name="faces"></param>
        private void DisplayFaces(Face[] faces)
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
        }

        /// <summary>
        /// Asynchronously loads a list of faces from the API.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task<Face[]> LoadFaces(string filePath)
        {
            Face[] faces;
            using (FileStream stream = File.OpenRead(filePath))
            {
                using (FaceServiceClient client =
                    new FaceServiceClient(File.ReadAllText("./FaceApiSubscriptionKey.txt"), Endpoint))
                {
                    faces = await client.DetectAsync(
                        stream,
                        returnFaceId: true,
                        returnFaceLandmarks: true,
                        returnFaceAttributes: new[] {FaceAttributeType.Emotion});
                }
            }
            return faces;
        }

        /// <summary>
        /// Clears the display of all face rectangles.
        /// </summary>
        private void ClearFaces()
        {
            if (OutputCanvas.Children.Count > 1)
            {
                OutputCanvas.Children.RemoveRange(1, OutputCanvas.Children.Count - 1);
            }
        }

        /// <summary>
        /// Calculates the rectangle for the given face.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Calculates the color that the given face should use.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private static SolidColorBrush CalculateColorForFace(Face face)
        {
            var bestEmotion = face.FaceAttributes.Emotion.ToRankedList().First();
            return emotionMap[bestEmotion.Key];
        }
    }
}
