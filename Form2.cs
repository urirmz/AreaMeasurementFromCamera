using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MedicionCamara
{
    public partial class Form2 : Form
    {
        private List<MeasuredObject> objects;
        private Graphics imageGraphics;
        public Form2(List<MeasuredObject> objectList)
        {
            InitializeComponent();

            objects = objectList;

            updateTable();

            imageGraphics = pictureBox1.CreateGraphics();
        }

        private void updateTable()
        {
            DataTable list = new DataTable();
            list.Columns.Add("No.", typeof(int));
            list.Columns.Add("Área (mm2)", typeof(string));

            for (int i = 0; i < objects.Count; i++)
            {
                list.Rows.Add(new object[] { i + 1, objects.ElementAt(i).getArea() });
            }

            dataGridView1.DataSource = list;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int index = dataGridView1.CurrentCell.RowIndex;
            if (index > -1 && index < objects.Count)
            {
                Bitmap image = new Bitmap(objects.ElementAt(index).getImage());
                Rectangle rectangle = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
                imageGraphics.DrawImage(image, rectangle);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            float totalArea = 0;
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                totalArea += float.Parse(row.Cells[1].Value.ToString());
            }
            textBox1.Text = totalArea.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            float totalArea = 0;
            foreach (MeasuredObject measuredObject in objects)
            {
                totalArea += measuredObject.getArea();
            }
            textBox1.Text = totalArea.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int index = dataGridView1.CurrentCell.RowIndex;
            if (index != -1)
            {
                objects.RemoveAt(index);
                updateTable();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            objects.Clear();
            updateTable();
        }
    }
}
