using System;
using OpenCvSharp;
using static MVSDK_Net.IMVDefine;

namespace MedicionCamara
{    
    public class VisionTools
    {
        private Mat matrix;
        private Mat histogram;
        private int histogramThreshold;
        private Point[][] contours;
        private Point[][] hulls;
        private Rect regionOfInterest;
        private VisionTools regionCalculation;

        public VisionTools() { }

        public VisionTools(Mat existingMatrix) { 
            matrix = existingMatrix.Clone(); 
        }
        
        public Mat getMatrix()
        {
            return matrix;
        }               

        public Point[][] getContours()
        {
            return contours;
        }        

        public Point[][] getHulls()
        {
            return hulls;
        }

        public Rect getRegionOfInterest()
        {
            return regionOfInterest;
        }

        public VisionTools getRegionCalculation()
        {
            return regionCalculation;
        }

        public PictureRectangle getRegionOfInterestAsPictureRectangle(System.Windows.Forms.PictureBox pictureBox)
        {
            double scaleX = (double)pictureBox.Width / matrix.Width;
            double scaleY = (double)pictureBox.Height / matrix.Height;
            int startX = (int)(regionOfInterest.X * scaleX);
            int startY = (int)(regionOfInterest.Y * scaleY);
            int endX = (int)(regionOfInterest.Width * scaleX) + startX;
            int endY = (int)(regionOfInterest.Height * scaleY) + startY;

            PictureRectangle pictureRectanle = new PictureRectangle();
            pictureRectanle.start = new System.Drawing.Point(startX, startY);
            pictureRectanle.end = new System.Drawing.Point(endX, endY);

            return pictureRectanle;
        }

        public System.Drawing.Bitmap getMatrixAsBitmap()
        {
            return matrixToBitmap(matrix);
        }

        public System.Drawing.Bitmap getHistogramAsBitmap()
        {
            return matrixToBitmap(get2DHistogram());
        }

        public Mat get2DHistogram()
        {
            return histogram1DTo2D(histogram, 320, 160);
        }

        public Point[][] getLargestContours()
        {
            int quantity = contours.Length / 2;

            Array.Sort(contours, (x, y) => x.Length.CompareTo(y.Length));
            Point[][] largestLengthContours = new Point[quantity][];
            int j = contours.Length - 1;
            for (int i = 0; i < largestLengthContours.Length; i++)
            {
                largestLengthContours[i] = contours[j];
                j--;
            }

            Array.Sort(largestLengthContours, (x, y) => Cv2.ContourArea(x).CompareTo(Cv2.ContourArea(y)));
            Point[][] largestAreaContours = new Point[quantity / 5][];
            j = largestLengthContours.Length - 1;
            for (int i = 0; i < largestAreaContours.Length; i++)
            {
                largestAreaContours[i] = largestLengthContours[j];
                j--;
            }

            return largestAreaContours;
        }

        public Point[][] getLargestHull()
        {
            Point[][] largestAreaHull = null;

            try
            {
                Array.Sort(hulls, (x, y) => Cv2.ContourArea(x).CompareTo(Cv2.ContourArea(y)));
                largestAreaHull = new Point[][] { hulls[hulls.Length - 1] };
            }            
            catch { }

            return largestAreaHull;
        }

        public int getThresholdValue()
        {            
            return histogramThreshold;
        }

        public void blur()
        {
            try
            {
                matrix = matrix.GaussianBlur(new Size(5, 5), 0);
            }
            catch { }
        }

        public void binarize()
        {   
            try
            {
                blur();
                matrix = matrix.Threshold(0, 255, ThresholdTypes.Otsu);
            }
            catch { }
        }

        public void binarizeRegionOfInterest()
        {
            try
            {
                Range rows = new Range(regionOfInterest.Top, regionOfInterest.Bottom);
                Range columns = new Range(regionOfInterest.Left, regionOfInterest.Right);

                Mat binarized = matrix.SubMat(rows, columns);
                binarized = binarized.MedianBlur(9);
                binarized = binarized.Threshold(0, 255, ThresholdTypes.Otsu);

                matrix = new Mat(matrix.Size(), matrix.Type(), Scalar.White);

                binarized.CopyTo(matrix.RowRange(rows).ColRange(columns));
            }
            catch { }
        }        

        public string countBlackPixels()
        {
            int count = (matrix.Cols * matrix.Rows) - Cv2.CountNonZero(matrix);
            return count.ToString() + " pixeles";
        }

        public void setMatrixFromFrame(IMV_Frame frame)
        {
            matrix = new Mat((int)frame.frameInfo.height, (int)frame.frameInfo.width, MatType.CV_8UC1, frame.pData);
        }

        public void setMatrixFromExistingMatrix(Mat matrix)
        {
            this.matrix = matrix.Clone();
        }

        public void setHistogram()
        {
            histogram = new Mat();
            Cv2.CalcHist
            (
                new Mat[] { matrix },
                new int[] { 0 },
                null,
                histogram,
                1,
                new int[] { 256 },
                new Rangef[] { new Rangef(0, 256) }
            );
            setHistogramThreshold();
        }

        public void setEdges()
        {
            Cv2.Canny(matrix, matrix, histogramThreshold, 200);
        }

        public string setContours()
        {
            HierarchyIndex[] hierarchyIndexes;
            Cv2.FindContours
            (
                image: matrix,
                contours: out contours,
                hierarchy: out hierarchyIndexes,
                mode: RetrievalModes.External,
                method: ContourApproximationModes.ApproxSimple
            );

            return "Contornos encontrados: " + contours.Length.ToString();
        }

        public void setHulls()
        {
            hulls = new Point[contours.Length][];
            for (int i = 0; i < contours.Length; i++)
            {
                hulls[i] = Cv2.ConvexHull(contours[i]);
            }
        }

        public string setContoursFromBinary()
        {
            Mat inversedMatrix = matrix.Threshold(127, 255, ThresholdTypes.BinaryInv);
            VisionTools binary = new VisionTools(inversedMatrix);
            string message = binary.setContours();
            contours = binary.getContours();
            return message;
        }

        public void setRegionOfInterest()
        {
            regionCalculation = new VisionTools(matrix);
            regionCalculation.blur();
            regionCalculation.setEdges();
            regionCalculation.binarize();
            regionCalculation.setContours();
            regionCalculation.setHulls();

            Point[][] largestHull = regionCalculation.getLargestHull();

            if (largestHull != null) 
            {

                int x = int.MaxValue;
                int y = int.MaxValue;
                int maxX = int.MinValue;
                int maxY = int.MinValue;

                foreach (Point point in largestHull[0])
                {
                    x = Math.Min(x, point.X);
                    y = Math.Min(y, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                int margin = 20;

                x -= margin;
                y -= margin;
                maxX += margin;
                maxY += margin;

                regionOfInterest = new Rect(x, y, maxX - x, maxY - y);
            }
        }

        public void setHistogramThreshold()
        {
            Mat smooth = smoothHistogram(histogram);

            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            int maxValueIndex = 0;
            histogramThreshold = 0;
            int j = smooth.Rows - 5;
            for (int i = 0; i < smooth.Rows - 4; i++)
            {
                if (smooth.Get<float>(i - 5) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i - 4) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i - 3) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i - 2) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i - 1) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i) < minValue && i < maxValueIndex &&
                    smooth.Get<float>(i + 1) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i + 2) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i + 3) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i + 4) > smooth.Get<float>(i) &&
                    smooth.Get<float>(i + 5) > smooth.Get<float>(i))
                {
                    histogramThreshold = i;
                    minValue = smooth.Get<float>(i);
                }
                if (smooth.Get<float>(j) > maxValue)
                {
                    maxValueIndex = j;
                    maxValue = smooth.Get<float>(j);
                }
                j--;
            }
        }

        public static System.Drawing.Bitmap matrixToBitmap(Mat matrix)
        {
            System.Drawing.Bitmap bitmap = null;
            try
            {
                if (matrix.Type() == MatType.CV_8UC1)
                {
                    bitmap = new System.Drawing.Bitmap(matrix.Width, matrix.Height, (int)matrix.Step(), System.Drawing.Imaging.PixelFormat.Format8bppIndexed, matrix.Data);
                    System.Drawing.Imaging.ColorPalette palette = bitmap.Palette;
                    for (int i = 0; i <= 255; i++)
                    {
                        palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                    }
                    bitmap.Palette = palette;
                }
                else
                {
                    bitmap = new System.Drawing.Bitmap(matrix.Width, matrix.Height, (int)matrix.Step(), System.Drawing.Imaging.PixelFormat.Format24bppRgb, matrix.Data);
                }
            }
            catch
            {
                bitmap = new System.Drawing.Bitmap(matrix.Width, matrix.Height);
            }
            return bitmap;
        }

        public static Mat histogram1DTo2D(Mat histogram, int width, int height)
        {
            double minValue;
            double maxValue;
            Cv2.MinMaxLoc(histogram, out minValue, out maxValue);

            Mat render = new Mat(new Size(width, height), MatType.CV_8UC3, Scalar.All(255));
            Scalar color = Scalar.All(60);
            Mat scaledHistogram = histogram.Clone() * (maxValue != 0 ? height / maxValue : 0.0);

            for (int i = 0; i < 256; ++i)
            {
                int columnWidth = (int)((double)width / 256);
                render.Rectangle
                (
                    new Point(i * columnWidth, render.Rows - (int)scaledHistogram.Get<float>(i)),
                    new Point((i + 1) * columnWidth, render.Rows),
                    color,
                    -1
                );
            }

            return render;
        }

        public static Mat smoothHistogram(Mat histogram)
        {
            Mat smoothHistogram = histogram.Clone();
            for (int i = 0; i < smoothHistogram.Rows - 4; i++)
            {
                float pixelAverage = (smoothHistogram.Get<float>(i) + smoothHistogram.Get<float>(i + 1) + smoothHistogram.Get<float>(i + 2) + smoothHistogram.Get<float>(i + 3) + smoothHistogram.Get<float>(i + 4)) / 5;
                smoothHistogram.Set(i, pixelAverage);
            }
            return smoothHistogram;
        }
    }

    public struct PictureRectangle
    {
        public System.Drawing.Point start { get; set; }
        public System.Drawing.Point end { get; set; }        
    }
    
}
