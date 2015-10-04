using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace DominantColour {
    public class Program {
        static void Main(string[] args) {
            string inputArg = args[0];
            int k = 3;

            if (Directory.Exists(inputArg) == true) {
                foreach (string file in Directory.EnumerateFiles(Path.GetFullPath(inputArg), "*.*", SearchOption.AllDirectories)) {
                    GetDominantColour(file, k);
                }
                return;
            }

            if (File.Exists(inputArg) == true) {
                GetDominantColour(inputArg, k);
            }

            Console.WriteLine("Unable to open {0}. Ensure it's a file or directory", inputArg);
        }

        public class KMeansClustering {
            private readonly double _sensitivity;

            public KMeansClustering(double sensitivity = 0.0d) {
                _sensitivity = sensitivity;
            }

            public IList<Color> Calculate(int k, IList<Color> colours) {
                Random random = new Random();
                List<KDataPoint> clusters = new List<KDataPoint>();
                List<int> usedIndexes = new List<int>();
                while (clusters.Count < k) {
                    int index = random.Next(0, colours.Count);
                    if (usedIndexes.Contains(index) == true) {
                        continue;
                    }

                    usedIndexes.Add(index);
                    KDataPoint cluster = new KDataPoint(colours[index], _sensitivity);
                    clusters.Add(cluster);
                }

                bool updated = false;
                do {
                    updated = false;
                    foreach (Color colour in colours) {
                        double shortestDistance = float.MaxValue;
                        KDataPoint closestCluster = null;

                        foreach (KDataPoint cluster in clusters) {
                            double distance = cluster.DistanceFromCentre(colour);
                            if (distance < shortestDistance) {
                                shortestDistance = distance;
                                closestCluster = cluster;
                            }
                        }

                        closestCluster.Add(colour);
                    }

                    foreach (KDataPoint cluster in clusters) {
                        if (cluster.RecalculateCentre() == true) {
                            updated = true;
                        }
                    }
                } while (updated == true);

                return clusters.OrderByDescending(c => c.PriorCount).Select(c => c.Centre).ToList();
            }
        }

        public class KDataPoint {
            private readonly double _threshold;
            private readonly List<Color> _colours;

            public KDataPoint(Color centre, double threshold = 10d) {
                _threshold = threshold;
                Centre = centre;
                _colours = new List<Color>();
            }

            public Color Centre { get; set; }
            public int PriorCount { get; set;}

            public void Add(Color colour) {
                _colours.Add(colour);
            }

            public bool RecalculateCentre() {
                Color updatedCentre;

                if (_colours.Count > 0) {
                    float r = 0;
                    float g = 0;
                    float b = 0;

                    foreach (Color color in _colours) {
                        r += color.R;
                        g += color.G;
                        b += color.B;
                    }

                    updatedCentre = Color.FromArgb((int)Math.Round(r / _colours.Count), (int)Math.Round(g / _colours.Count), (int)Math.Round(b / _colours.Count));
                } else {
                    updatedCentre = Color.FromArgb(0, 0, 0, 0);
                }

                double distance = EuclideanDistance(Centre, updatedCentre);
                Centre = updatedCentre;

                PriorCount = _colours.Count;
                _colours.Clear();

                return distance > _threshold;
            }

            public double DistanceFromCentre(Color colour) {
                return EuclideanDistance(colour, Centre);
            }

            private static double EuclideanDistance(Color c1, Color c2) {
                double distance = Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2);

                return Math.Sqrt(distance);
            }
        }


        private static void GetDominantColour(string inputFile, int k) {
            using (Image image = Image.FromFile(inputFile)) {
                const int maxResizedDimension = 200;
                Size resizedSize;
                if (image.Width > image.Height) {
                    resizedSize = new Size(maxResizedDimension, (int)Math.Floor((image.Height / (image.Width * 1.0f)) * maxResizedDimension));
                } else {
                    resizedSize = new Size((int)Math.Floor((image.Width / (image.Width * 1.0f)) * maxResizedDimension), maxResizedDimension);
                }

                using (Bitmap resized = new Bitmap(image, resizedSize)) {
                    List<Color> colors = new List<Color>(resized.Width * resized.Height);
                    for (int x = 0; x < resized.Width; x++) {
                        for (int y = 0; y < resized.Height; y++) {
                            colors.Add(resized.GetPixel(x, y));
                        }
                    }

                    KMeansClustering cluster = new KMeansClustering(5.0d);
                    IList<Color> kColors = cluster.Calculate(3, colors);


                    foreach (Color color in kColors) {
                        Console.WriteLine("K: {0} (#{1:x2}{2:x2}{3:x2})", color, color.R, color.G, color.B);
                    }

                    int swatchHeight = 20;
                    using (Bitmap bmp = new Bitmap(resized.Width, resized.Height + swatchHeight)) {
                        using (Graphics gfx = Graphics.FromImage(bmp)) {
                            gfx.DrawImage(resized, new Rectangle(0, 0, resized.Width, resized.Height));

                            int swatchWidth = (int)Math.Floor(bmp.Width / (k * 1.0f));
                            for (int i = 0; i < k; i++) {
                                using (SolidBrush brush = new SolidBrush(kColors[i])) {
                                    gfx.FillRectangle(brush, new Rectangle(i * swatchWidth, resized.Height, swatchWidth, swatchHeight));
                                }
                            }
                        }

                        string outputFile = string.Format("{0}.output.png", Path.GetFileNameWithoutExtension(inputFile));
                        bmp.Save(outputFile, ImageFormat.Png);
                        Process.Start("explorer.exe", outputFile);
                    }
                }
            }
        }
    }
}