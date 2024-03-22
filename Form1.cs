using System;
using System.Drawing;
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
        private PictureRectangle rectangle;

        public Form1()
        {
            InitializeComponent();

            cameras = new CameraManager();
            vision = new VisionTools();

            dataGridView1.Rows.Add(3);
            dataGridView1.Rows[0].Cells[0].Value = "Código";
            dataGridView1.Rows[1].Cells[0].Value = "Serie";
            dataGridView1.Rows[2].Cells[0].Value = "Marca";
            dataGridView1.Rows[3].Cells[0].Value = "Modelo";

            cameraGraphics = pictureBox1.CreateGraphics();
            visionGraphics = pictureBox2.CreateGraphics();

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
        public void drawPolygonOnGraphics(ref Graphics graphics, PictureBox pictureBox, OpenCvSharp.Point[][] polygon, Color color)
        {
            Pen pen = new Pen(color, 3);
            double scaleX = (double)pictureBox.Width / vision.getMatrix().Width;
            double scaleY = (double)pictureBox.Height / vision.getMatrix().Height;

            for (int i = 1; i < polygon.Length; i++)
            {
                Point point1 = new Point((int)(polygon[i - 1][0].X * scaleX), (int)(polygon[i - 1][0].Y * scaleY));
                Point point2 = new Point((int)(polygon[i][0].X * scaleX), (int)(polygon[i][0].Y * scaleY));
                graphics.DrawLine(pen, point1, point2);
            }
        }

        public void drawBlobsOnGraphics(ref Graphics graphics, PictureBox pictureBox, OpenCvSharp.KeyPoint[] keyPoints, Color color)
        {
            Pen pen = new Pen(color, 3);
            double scaleX = (double)pictureBox.Width / vision.getMatrix().Width;
            double scaleY = (double)pictureBox.Height / vision.getMatrix().Height;
            
            foreach (OpenCvSharp.KeyPoint keyPoint in keyPoints)
            {
                int x = (int) (scaleX * keyPoint.Pt.X);
                int y = (int)(scaleY * keyPoint.Pt.Y);
                int size = 200;
                graphics.DrawRectangle(pen, x - (size / 2), y - (size / 2), size, size);
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
            if (cameras.cameraIsReady())
            {
                textBox1.Text = cameras.takePicture();
                Bitmap image = cameras.getLastPictureAsBitmap();
                if (image != null)
                {
                    drawImageOnGraphics(ref cameraGraphics, pictureBox1, image);
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
            drawRectangleOnGraphics(ref cameraGraphics, rectangle, Color.Red);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Analizando contornos...";
            vision.setMatrixFromFrame(cameras.getLastFrame());
            if (vision.getMatrix() != null)
            {
                vision.setRegionOfInterest();
                drawImageOnGraphics(ref visionGraphics, pictureBox2, vision.getMatrixAsBitmap());
                if (vision.getRegionOfInterest().Width > 0 && vision.getRegionOfInterest().Height > 0)
                {
                    drawRectangleOnGraphics(ref visionGraphics, vision.getRegionOfInterestAsPictureRectangle(pictureBox2), Color.Red);
                    textBox1.Text = "Objeto encontrado";
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
            if (vision.getMatrix() != null)
            {
                if (vision.getRegionOfInterest() != null)
                {
                    vision.binarizeRegionOfInterest();
                    drawImageOnGraphics(ref visionGraphics, pictureBox2, vision.getMatrixAsBitmap());
                    vision.setContoursFromBinary();
                    drawContoursOnGraphics(ref cameraGraphics, pictureBox1, vision.getContours(), Color.Green);

                    textBox1.Text = "Medición obtenida exitosamente";
                }
                else
                {
                    textBox1.Text = "Es necesario buscar objetos antes de binarizar";
                }
            }
            else
            {
                textBox1.Text = "No hay ninguna imagen para binarizar";
            }
        }
    }
}
