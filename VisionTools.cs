using OpenCvSharp;
using System;
using static MVSDK_Net.IMVDefine;

namespace MedicionCamara
{    
    public class VisionTools
    {
        private Mat matrix;
        private Point[][] contours;
        private Point[][] hulls;
        private KeyPoint[] keyPoints;
        private Rect regionOfInterest;

        public VisionTools() { }

        public VisionTools(Mat existingMatrix) { matrix = existingMatrix.Clone(); }

        public Mat getMatrix()
        {
            return matrix;
        }               

        public Point[][] getContours()
        {
            return contours;
        }        

        public KeyPoint[] getBlobs()
        {
            return keyPoints;
        }

        public Point[][] getHulls()
        {
            return hulls;
        }

        public Rect getRegionOfInterest()
        {
            return regionOfInterest;
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

        public KeyPoint getBiggestBlob()
        {
            KeyPoint biggestBlob = keyPoints[0];
            foreach (KeyPoint keyPoint in keyPoints)
            {
                if (keyPoint.Size > biggestBlob.Size)
                {
                    biggestBlob = keyPoint;
                }
            }
            return biggestBlob;
        }

        public void binarize()
        {   
            try
            {
                matrix = matrix.GaussianBlur(new Size(5, 5), 0);
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
                binarized = binarized.GaussianBlur(new Size(5, 5), 0);
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

        public void setBlobs()
        {
            SimpleBlobDetector.Params parameters = new SimpleBlobDetector.Params
            {
                //MinDistBetweenBlobs = 10, // 10 pixels between blobs
                //MinRepeatability = 1,

                //MinThreshold = 100,
                //MaxThreshold = 255,
                //ThresholdStep = 5,

                FilterByArea = true,
                MinArea = 100, // 10 pixels squared
                //MaxArea = 500,

                FilterByCircularity = false,
                //FilterByCircularity = true,
                //MinCircularity = 0.001f,

                FilterByConvexity = false,
                //FilterByConvexity = true,
                //MinConvexity = 0.001f,
                //MaxConvexity = 10,

                FilterByInertia = false,
                //FilterByInertia = true,
                //MinInertiaRatio = 0.001f,

                FilterByColor = true,
                BlobColor = 0
            };
            SimpleBlobDetector blobDetector = SimpleBlobDetector.Create(parameters);
            keyPoints = blobDetector.Detect(matrix);

            Cv2.DrawKeypoints(matrix, keyPoints, matrix, Scalar.Red, DrawMatchesFlags.DrawRichKeypoints);
        }

        public void setMatrixFromFrame(IMV_Frame frame)
        {
            matrix = new Mat((int)frame.frameInfo.height, (int)frame.frameInfo.width, MatType.CV_8UC1, frame.pData);
        }

        public void setMatrixFromExistingMatrix(Mat matrix)
        {
            this.matrix = matrix.Clone();
        }

        public void setEdges()
        {
            try
            {
                matrix = matrix.GaussianBlur(new Size(5, 5), 0);
                Cv2.Canny(matrix, matrix, 100, 200);
            }
            catch
            {
                Cv2.Canny(matrix, matrix, 100, 200);
            }            
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

            hulls = new Point[contours.Length][];
            for (int i = 0; i < contours.Length; i++)
            {
                hulls[i] = Cv2.ConvexHull(contours[i]);
            }

            return "Contornos encontrados: " + contours.Length.ToString();
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
            VisionTools region = new VisionTools(matrix);
            region.setEdges();
            region.binarize();
            region.setContours();

            Point[][] largestHull = region.getLargestHull();
            int x = int.MaxValue;
            int y = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            if (largestHull != null) 
            {
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

        public string countBlackPixels()
        {
            int count = (matrix.Cols * matrix.Rows) - Cv2.CountNonZero(matrix);
            return count.ToString() + " pixeles";
        }
    }

    public struct PictureRectangle
    {
        public System.Drawing.Point start { get; set; }
        public System.Drawing.Point end { get; set; }        
    }
    
}
