using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MedicionCamara
{
    public partial class Form1 : Form
    {
        private CameraManager cameras;        
        private VisionTools vision;
        private Graphics cameraGraphics;
        private Graphics visionGraphics;
        private Graphics histogramGraphics;
        private PictureRectangle rectangle;
        private bool calibrationMode;
        private List<MeasuredObject> measuredObjects;

        public Form1()
        {
            InitializeComponent();

            cameras = new CameraManager();
            vision = new VisionTools();
            measuredObjects = new List<MeasuredObject>();

            dataGridView1.Rows.Add(3);
            dataGridView1.Rows[0].Cells[0].Value = "Código";
            dataGridView1.Rows[1].Cells[0].Value = "Serie";
            dataGridView1.Rows[2].Cells[0].Value = "Marca";
            dataGridView1.Rows[3].Cells[0].Value = "Modelo";

            button8.Visible = false;
            button9.Visible = false;
            button13.Visible = false;

            cameraGraphics = pictureBox1.CreateGraphics();
            visionGraphics = pictureBox2.CreateGraphics();
            histogramGraphics = pictureBox3.CreateGraphics();

            textBox1.Text = cameras.getIMVVersion();
        }

        public void drawImageOnGraphics(ref Graphics graphics, PictureBox pictureBox, Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, pictureBox.Width, pictureBox.Height);
            graphics.DrawImage(bitmap, rect);
        }

        public void drawRectangleOnGraphics(ref Graphics graphics, PictureRectangle rectangle, Color color)
        {
            Pen pen = new Pen(color, 3);
            int startX = Math.Min(rectangle.start.X, rectangle.end.X);
            int startY = Math.Min(rectangle.start.Y, rectangle.end.Y);
            int width = Math.Abs(rectangle.start.X - rectangle.end.X);
            int height = Math.Abs(rectangle.start.Y - rectangle.end.Y);
            graphics.DrawRectangle(pen, startX, startY, width, height);
        }

        public void drawContoursOnGraphics(ref Graphics graphics, PictureBox pictureBox, OpenCvSharp.Point[][] contours, Color color)
        {
            Pen pen = new Pen(color, 3);
            double scaleX = (double) pictureBox.Width / vision.getMatrix().Width;
            double scaleY = (double) pictureBox.Height / vision.getMatrix().Height;

            foreach (OpenCvSharp.Point[] contour in contours) 
            {
                for (int i = 1; i < contour.Length; i++) 
                {
                    Point point1 = new Point((int)(contour[i - 1].X * scaleX), (int) (contour[i - 1].Y * scaleY));
                    Point point2 = new Point((int)(contour[i].X * scaleX), (int)(contour[i].Y * scaleY));
                    graphics.DrawLine(pen, point1, point2);
                }
            }
        }

        public void drawContoursOnGraphics(ref Graphics graphics, PictureBox pictureBox, OpenCvSharp.Point[][] contours)
        {
            double scaleX = (double)pictureBox.Width / vision.getMatrix().Width;
            double scaleY = (double)pictureBox.Height / vision.getMatrix().Height;

            foreach (OpenCvSharp.Point[] contour in contours)
            {
                for (int i = 1; i < contour.Length; i++)
                {
                    Random random = new Random();
                    Pen pen = new Pen(Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)), 3);
                    Point point1 = new Point((int)(contour[i - 1].X * scaleX), (int)(contour[i - 1].Y * scaleY));
                    Point point2 = new Point((int)(contour[i].X * scaleX), (int)(contour[i].Y * scaleY));
                    graphics.DrawLine(pen, point1, point2);
                }
            }
        }

        public void drawThresholdOnHistogram(ref Graphics graphics, PictureBox pictureBox, int threshold, Color color)
        {
            Pen pen = new Pen(color, 3);
            double scaleX = (double)pictureBox.Width / vision.get2DHistogram().Width;

            Point point1 = new Point((int)(threshold * scaleX), 0);
            Point point2 = new Point((int)(threshold * scaleX), pictureBox.Height);

            graphics.DrawLine(pen, point1, point2);
        }

        public void drawCalibrationPoints(ref Graphics graphics, PictureBox pictureBox, List<List<OpenCvSharp.Point2f>> calibrationPoints, Color color)
        {
            Pen pen = new Pen(color, 3);
            double scaleX = (double)pictureBox.Width / vision.getMatrix().Width;
            double scaleY = (double)pictureBox.Height / vision.getMatrix().Height;

            foreach (List<OpenCvSharp.Point2f> vector in calibrationPoints)
            {
                for (int i = 1; i < vector.Count; i++)
                {
                    Point point1 = new Point((int)(vector[i - 1].X * scaleX), (int)(vector[i - 1].Y * scaleY));
                    Point point2 = new Point((int)(vector[i].X * scaleX), (int)(vector[i].Y * scaleY));
                    graphics.DrawLine(pen, point1, point2);
                    graphics.DrawEllipse(pen, new RectangleF(point1.X - 3, point1.Y - 3, 6, 6));
                }
                graphics.DrawEllipse(pen, new RectangleF((float) (vector[vector.Count - 1].X * scaleX) - 3, (float)(vector[vector.Count - 1].Y * scaleY) - 3, 6, 6));
            }
        }

        public void toggleCalibrationMode()
        {
            if (calibrationMode)
            {
                button4.Visible = true;
                button5.Visible = true;
                button6.Visible = true;
                button7.Visible = true;

                button8.Visible = false;
                button9.Visible = false;

                button8.Text = "Confirmar";

                calibrationMode = false;
            }
            else
            {
                button4.Visible = false;
                button5.Visible = false;
                button6.Visible = false;
                button7.Visible = false;

                button8.Visible = true;
                button9.Visible = true;

                calibrationMode = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Buscando dispositivos...";
            cameras.listDevices();
            label2.Text = cameras.getList().Count.ToString();

            if (cameras.getList().Count < 1)
            {
                textBox1.Text = "No hay cámaras disponibles";
            }
            else
            {
                textBox1.Text = "Dispositivos encontrados";

                dataGridView1.Rows[0].Cells[1].Value = cameras.getList().ElementAt(0).getKey();
                dataGridView1.Rows[1].Cells[1].Value = cameras.getList().ElementAt(0).getSerialNumber();
                dataGridView1.Rows[2].Cells[1].Value = cameras.getList().ElementAt(0).getVendor();
                dataGridView1.Rows[3].Cells[1].Value = cameras.getList().ElementAt(0).getModel();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int cameraId = 0;
            if (cameras.getList().Count() > 0)
            {
                textBox1.Text = cameras.connectCamera(cameraId);
            }
            else 
            {
                textBox1.Text = "No hay cámaras disponibles";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (cameras.getList().Count() > 0)
            {
                textBox1.Text = cameras.disconnectCamera();
            }
            else
            {
                textBox1.Text = "No hay cámaras disponibles";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Capturando imagen...";
            textBox2.Text = null;
            if (cameras.cameraIsReady())
            {
                textBox1.Text = cameras.takePicture();
                vision.setMatrixFromFrame(cameras.getLastFrame());
                if (vision.isReady())
                {
                    drawImageOnGraphics(ref cameraGraphics, pictureBox1, vision.getMatrixAsBitmap());
                    if (!calibrationMode)
                    {
                        vision.setHistogram();
                        drawImageOnGraphics(ref histogramGraphics, pictureBox3, vision.getHistogramAsBitmap());
                        drawThresholdOnHistogram(ref histogramGraphics, pictureBox3, vision.getThresholdValue(), Color.Red);
                    }
                }
                else
                {
                    textBox1.Text = "Error al tomar fotografía, vuelva a conectar la cámara";
                }                
            }
            else
            {
                textBox1.Text = "Ninguna cámara conectada";
            }
            
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            rectangle.start = pictureBox1.PointToClient(Cursor.Position);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            rectangle.end = pictureBox1.PointToClient(Cursor.Position);
            // Draw a rectangle when selecting the camera graphics
            // drawRectangleOnGraphics(ref cameraGraphics, rectangle, Color.Red);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Analizando contornos...";
            if (vision.isReady())
            {
                vision.setRegionOfInterest();
                drawImageOnGraphics(ref visionGraphics, pictureBox2, vision.getMatrixAsBitmap());
                drawImageOnGraphics(ref histogramGraphics, pictureBox3, vision.getRegionCalculation().getMatrixAsBitmap());
                drawContoursOnGraphics(ref histogramGraphics, pictureBox3, vision.getRegionCalculation().getLargestHull(), Color.Blue);
                if (vision.getRegionOfInterest().Width > 0 && vision.getRegionOfInterest().Height > 0 && !calibrationMode)
                {
                    try
                    {
                        drawRectangleOnGraphics(ref visionGraphics, vision.getRegionOfInterestAsPictureRectangle(pictureBox2), Color.Red);
                        textBox1.Text = "Objeto encontrado";
                    }
                    catch 
                    {
                        textBox1.Text = "Error al crear fotografía, vuelva a conectar la cámara";
                    }
                }
                else
                {
                    textBox1.Text = "Ningún objeto encontrado";
                }
            }
            else
            {
                textBox1.Text = "No hay ninguna imagen para analizar";
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Obteniendo medición...";
            if (vision.isReady())
            {
                if (vision.getRegionOfInterest().Width > 0 && vision.getRegionOfInterest().Height > 0)
                {
                    vision.binarizeRegionOfInterest();
                    drawImageOnGraphics(ref visionGraphics, pictureBox2, vision.getMatrixAsBitmap());
                    vision.setContoursFromBinary();
                    drawContoursOnGraphics(ref cameraGraphics, pictureBox1, vision.getContours(), Color.Green);

                    textBox2.Text = vision.getObjectMeasurement();
                    pictureBox3.Image = null;
                    textBox1.Text = "Medición obtenida";
                    if (textBox2.Text != "Calibración requerida" && textBox2.Text != "Error en la medición")
                    {
                        button13.Visible = true;
                    }
                }
                else
                {
                    textBox1.Text = "Es necesario buscar objetos antes de realizar medición";
                }
            }
            else
            {
                textBox1.Text = "No hay ningón objeto para medir";
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {            
            toggleCalibrationMode();
            vision.dumpCalibration();
            button4_Click(sender, e);
            button5_Click(sender, e);
            if (vision.isReady())
            {
                textBox1.Text = "Buscando patrón de calibración...";
                if (vision.setChessPattern())
                {
                    drawCalibrationPoints(ref visionGraphics, pictureBox1, vision.getCalibrationPoints(), Color.Fuchsia);
                    textBox1.Text = "Patrón encontrado";
                    return;
                }
            }
            textBox1.Text = "No se encontró ningún patrón de calibración";
            toggleCalibrationMode();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (button8.Text == "Confirmar")
            {
                if (vision.lensCorrection())
                {
                    button8.Text = "Continuar";
                    textBox1.Text = "Corrección de lente añadida, presione continuar";
                    return;
                }
            }
            else if (button8.Text == "Continuar") 
            {
                button4_Click(sender, e);
                button5_Click(sender, e);
                if (vision.isReady() && vision.setChessPattern())
                {                    
                    vision.setPixelsPerMm2();
                    drawImageOnGraphics(ref visionGraphics, pictureBox2, vision.getMatrixAsBitmap());
                    drawCalibrationPoints(ref visionGraphics, pictureBox1, vision.getCalibrationPoints(), Color.Orange);
                    textBox1.Text = "Calibración concluida con valor: " + vision.getPixelsPerMm2().ToString() + " pixeles/Mm2" ;
                    toggleCalibrationMode();
                    return;
                }
            }
            vision.dumpCalibration();
            toggleCalibrationMode();
            textBox1.Text = "Error en la calibración, vuelva a intentarlo";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            vision.dumpCalibration();
            toggleCalibrationMode();
            button11_Click(sender, e);

            textBox1.Text = "Calibración cancelada";
        }

        private void button10_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                RestoreDirectory = true,
                Filter = "Images (*.BMP;*.JPG;*.GIF,*.PNG,*.TIFF)|*.BMP;*.JPG;*.GIF;*.PNG;*.TIFF"
            };
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = openFileDialog1.SafeFileName;
                try
                {
                    vision.setMatrixFromFile(openFileDialog1.FileName);
                    drawImageOnGraphics(ref cameraGraphics, pictureBox1, vision.getMatrixAsBitmap());

                    vision.setHistogram();
                    drawImageOnGraphics(ref histogramGraphics, pictureBox3, vision.getHistogramAsBitmap());
                    drawThresholdOnHistogram(ref histogramGraphics, pictureBox3, vision.getThresholdValue(), Color.Red);
                }
                catch { textBox1.Text = "Error en archivo"; }
            }            
            else
            {
                textBox1.Text = "No es posible abrir archivo";
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            pictureBox3.Image = null;
            vision.reset();

            textBox1.Text = "Limpieza exitosa";
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Form listWindow = new Form2(measuredObjects)
            {
                Visible = true,
                Text = "Lista de objetos medidos"
            };
        }

        private void button13_Click(object sender, EventArgs e)
        {
            MemoryStream image = new MemoryStream();
            vision.getMatrixAsBitmap().Save(image, System.Drawing.Imaging.ImageFormat.Bmp);
            measuredObjects.Add(new MeasuredObject(vision.getObjectMeasurementAsFloat(), image));
            button13.Visible = false;
            textBox1.Text = "Objecto agregado a la lista";
        }
    }
}
