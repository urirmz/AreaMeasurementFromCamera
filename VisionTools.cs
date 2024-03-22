using OpenCvSharp;
using System;
using System.Collections.Generic;
using static MVSDK_Net.IMVDefine;

namespace MedicionCamara
{    
    public class VisionTools
    {
        private Mat matrix;
        private Mat histogram;
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
                bitmap = new System.Drawing.Bitmap(1920, 1200);
            }
            return bitmap;
        }

        public System.Drawing.Bitmap getHistogramAsBitmap()
        {
            double minValue;
            double maxValue;
            Cv2.MinMaxLoc(histogram, out minValue, out maxValue);

            int width = 320;
            int height = 160;
            Mat render = new Mat(new Size(width, height), MatType.CV_8UC3, Scalar.All(255));
            Scalar color = Scalar.All(60);

            Mat scaledHistogram = histogram.Clone();
            scaledHistogram = scaledHistogram * (maxValue != 0 ? height / maxValue : 0.0);

            for (int i = 0; i < 256; ++i)
            {
                int binaryWidth = (int)((double)width / 256);
                render.Rectangle
                (
                    new Point(i * binaryWidth, render.Rows - (int)scaledHistogram.Get<float>(i)),
                    new Point((i + 1) * binaryWidth, render.Rows),
                    color,
                    -1
                );
            }

            System.Drawing.Bitmap bitmap = null;
            try
            {
                if (render.Type() == MatType.CV_8UC1)
                {
                    bitmap = new System.Drawing.Bitmap(render.Width, render.Height, (int)render.Step(), System.Drawing.Imaging.PixelFormat.Format8bppIndexed, render.Data);
                    System.Drawing.Imaging.ColorPalette palette = bitmap.Palette;
                    for (int i = 0; i <= 255; i++)
                    {
                        palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                    }
                    bitmap.Palette = palette;
                }
                else
                {
                    bitmap = new System.Drawing.Bitmap(render.Width, render.Height, (int)render.Step(), System.Drawing.Imaging.PixelFormat.Format24bppRgb, render.Data);
                }
            }
            catch
            {
                bitmap = new System.Drawing.Bitmap(width, height);
            }
            return bitmap;
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

        public void equalize()
        {
            Cv2.EqualizeHist(matrix, matrix);
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
        }

        public void setEdges()
        {
            Cv2.Canny(matrix, matrix, 100, 200);
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
    }

    public struct PictureRectangle
    {
        public System.Drawing.Point start { get; set; }
        public System.Drawing.Point end { get; set; }        
    }
    
}
