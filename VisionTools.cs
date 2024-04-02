using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenCvSharp;
using static MVSDK_Net.IMVDefine;

namespace MedicionCamara
{    
    public class VisionTools
    {
        private Mat matrix, histogram;
        private int threshold;
        private Point[][] contours, hulls;
        private Rect regionOfInterest;
        private VisionTools regionCalculation;
        List<List<Point2f>> calibrationPoints;
        private double[,] undistortionMatrix; 
        private double[] distortionCoefficients;
        private float pixelsPerMm2;

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

        public PictureRectangle getRegionOfInterestAsPictureRectangle(PictureBox pictureBox)
        {
            return regionOfInterestToPictureRectangle(matrix, regionOfInterest, pictureBox);
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

        public Point[][] getLargestContour()
        {
            return largestContourInArray(contours);
        }

        public Point[][] getLargestHull()
        {
            return largestContourInArray(hulls);
        }

        public int getThresholdValue()
        {            
            return threshold;
        }

        public string getObjectMeasurement()
        {
            if (pixelsPerMm2 == 0)
            {
                return "Calibración requerida";
            }
            else
            {
                int blackPixels = blackPixelsInMatrix(matrix);
                if (blackPixels > 0)
                {
                    float objectMeasurement = blackPixels / pixelsPerMm2;
                    return objectMeasurement.ToString() + " mm2";
                }
                return "Error en la medición";
            }            
        }

        public float getObjectMeasurementAsFloat()
        {
            return blackPixelsInMatrix(matrix) / pixelsPerMm2;
        }

        public List<List<Point2f>> getCalibrationPoints()
        {
            return calibrationPoints;
        }

        public float getPixelsPerMm2()
        {
            return pixelsPerMm2;
        }

        public void blur()
        {
            try
            {
                matrix = matrix.GaussianBlur(new Size(5, 5), 0);
            }
            catch { }
        }

        public void binarizeOtsu()
        {   
            try
            {
                matrix = matrix.Threshold(0, 255, ThresholdTypes.Otsu);
            }
            catch { }
        }

        public void binarizeRegionOfInterest()
        {           
            Range rows = new Range(regionOfInterest.Top, regionOfInterest.Bottom);
            Range columns = new Range(regionOfInterest.Left, regionOfInterest.Right);

            VisionTools subMatrix = new VisionTools(matrix.SubMat(rows, columns).MedianBlur(9));
            subMatrix.setHistogram();
            Mat binarized = subMatrix.getMatrix().Threshold(subMatrix.getThresholdValue(), 255, ThresholdTypes.Binary);

            matrix = new Mat(matrix.Size(), matrix.Type(), Scalar.White);
            binarized.CopyTo(matrix.RowRange(rows).ColRange(columns));
        }

        public bool isReady()
        {
            if (matrix != null)
            {
                return matrix.Cols > 0 && matrix.Rows > 0;
            }
            return false;
        }

        public bool lensCorrection()
        {
            try
            {
                Size patternSize = new Size(9, 6);
                float lengthOfChessSquareInMm = 24.5f;

                List<List<Point3f>> objectPoints = new List<List<Point3f>>() { new List<Point3f>() };

                float pointX = 0, pointY = 0, pointZ = 0;
                for (int i = 0; i < patternSize.Height; i++)
                {
                    for (int j = 0; j < patternSize.Width; j++)
                    {
                        Point3f newPoint = new Point3f(pointX, pointY, pointZ);
                        objectPoints.ElementAt(0).Add(newPoint);
                        pointX += lengthOfChessSquareInMm;
                    }
                    pointX = 0;
                    pointY += lengthOfChessSquareInMm;
                }

                Size imageSize = new Size(matrix.Width, matrix.Height);
                Vec3d[] rotationVector;
                Vec3d[] traslationVector;

                undistortionMatrix = new double[3, 3];
                distortionCoefficients = new double[5];

                Cv2.CalibrateCamera
                (
                    objectPoints,
                    calibrationPoints,
                    imageSize,
                    undistortionMatrix,
                    distortionCoefficients,
                    out rotationVector,
                    out traslationVector
                );

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void dumpCalibration()
        {
            calibrationPoints = null;
            undistortionMatrix = null;
            distortionCoefficients = null;
        }

        public void reset()
        {
            dumpCalibration();
            matrix = null;
            histogram = null;
            threshold = 0;
            contours = null;
            hulls = null;
            regionCalculation = null;
            pixelsPerMm2 = 0;
    }

        public void setMatrixFromFrame(IMV_Frame frame)
        {
            matrix = new Mat((int)frame.frameInfo.height, (int)frame.frameInfo.width, MatType.CV_8UC1, frame.pData);
            if (undistortionMatrix != null && distortionCoefficients != null)
            {
                try
                {
                    Mat original = matrix.Clone();
                    Cv2.Undistort(original, matrix, InputArray.Create(undistortionMatrix), InputArray.Create(distortionCoefficients));

                    int border = 50;
                    Rect cleanArea = new Rect(border, border, original.Cols - (border * 2), original.Rows - (border * 2));
                    matrix = original.Clone(cleanArea);
                }
                catch { }
            }
        }

        public void setMatrixFromFile(string path)
        {            
            matrix = Cv2.ImRead(path, ImreadModes.Grayscale);
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
            int range = 100;
            Cv2.Canny(matrix, matrix, range, threshold + range);
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
            regionCalculation.setHistogram();
            regionCalculation.blur();
            regionCalculation.setEdges();
            regionCalculation.blur();
            regionCalculation.binarizeOtsu();
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

                x = Math.Max(x - margin, 0);
                y = Math.Max(y - margin, 0);
                maxX = Math.Min(maxX + margin, regionCalculation.getMatrix().Cols);
                maxY = Math.Min(maxY + margin, regionCalculation.getMatrix().Cols); ;

                regionOfInterest = new Rect(x, y, maxX - x, maxY - y);
            }
        }

        public void setHistogramThreshold()
        {
            threshold = thresholdFromHistogram(histogram);
        }

        public bool setChessPattern()
        {
            Size patternSize = new Size(9, 6);
            Point2f[] chessCorners;
            if (Cv2.FindChessboardCorners(matrix, patternSize, out chessCorners, ChessboardFlags.AdaptiveThresh | ChessboardFlags.NormalizeImage))
            {
                chessCorners = Cv2.CornerSubPix(matrix, chessCorners, new Size(11, 11), new Size(-1, -1), TermCriteria.Both(30, 0.001));
                calibrationPoints = new List<List<Point2f>>
                {
                    chessCorners.ToList()
                };
                return true;
            }
            return false;            
        }

        public void setPixelsPerMm2()
        {
            binarizeRegionOfInterest();

            int widthBlackSquares = 5;
            int heightBlackSquares = 7;
            float lengthOfChessSquareInMm = 24.5f;

            float blackMm2InPattern = widthBlackSquares * heightBlackSquares * (float) Math.Pow(lengthOfChessSquareInMm, 2);
            float blackPixelsInPattern = blackPixelsInMatrix(matrix);

            pixelsPerMm2 = blackPixelsInPattern / blackMm2InPattern;
        }

        public static System.Drawing.Bitmap matrixToBitmap(Mat matrix)
        {
            System.Drawing.Bitmap bitmap;
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

        public static Point[][] largestContourInArray(Point[][] array)
        {
            Point[][] largestAreaContour = null;

            try
            {
                Array.Sort(array, (x, y) => Cv2.ContourArea(x).CompareTo(Cv2.ContourArea(y)));
                largestAreaContour = new Point[][] { array[array.Length - 1] };
            }
            catch { }

            return largestAreaContour;
        }

        public static PictureRectangle regionOfInterestToPictureRectangle(Mat matrix, Rect regionOfInterest, PictureBox pictureBox)
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

        public static int blackPixelsInMatrix(Mat matrix)
        {
            return (matrix.Cols * matrix.Rows) - matrix.CountNonZero();
        }

        public static int thresholdFromHistogram(Mat histogram)
        {
            Mat smooth = smoothHistogram(histogram);

            int threshold = 0;
            float minValue = float.MaxValue;
            float maxValue = float.MinValue;
            int maxValueIndex = 0;
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
                    threshold = i;
                    minValue = smooth.Get<float>(i);
                }
                if (smooth.Get<float>(j) > maxValue)
                {
                    maxValueIndex = j;
                    maxValue = smooth.Get<float>(j);
                }
                j--;
            }

            return threshold;
        }
    }

    public struct PictureRectangle
    {
        public System.Drawing.Point start { get; set; }
        public System.Drawing.Point end { get; set; }        
    }

    public struct MeasuredObject
    {
        private float area;
        private MemoryStream image;

        public MeasuredObject(float area, MemoryStream image)
        {
            this.area = area;
            this.image = image;
        }

        public float getArea()
        {
            return area;
        }

        public MemoryStream getImage()
        {
            return image;
        }
    }
    
}
