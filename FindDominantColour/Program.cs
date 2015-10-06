using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using FindDominantColour;

namespace DominantColour {
    public class Program {
        static void Main(string[] args) {
            string inputArg = args[0];
            const int k = 3;

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

                    KMeansClusteringCalculator clustering = new KMeansClusteringCalculator();
                    IList<Color> dominantColours = clustering.Calculate(k, colors, 5.0d);

                    Console.WriteLine("Dominant colours for {0}:", inputFile);
                    foreach (Color color in dominantColours) {
                        Console.WriteLine("K: {0} (#{1:x2}{2:x2}{3:x2})", color, color.R, color.G, color.B);
                    }

                    const int swatchHeight = 20;
                    using (Bitmap bmp = new Bitmap(resized.Width, resized.Height + swatchHeight)) {
                        using (Graphics gfx = Graphics.FromImage(bmp)) {
                            gfx.DrawImage(resized, new Rectangle(0, 0, resized.Width, resized.Height));

                            int swatchWidth = (int)Math.Floor(bmp.Width / (k * 1.0f));
                            for (int i = 0; i < k; i++) {
                                using (SolidBrush brush = new SolidBrush(dominantColours[i])) {
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