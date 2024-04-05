using OpenCvSharp.Flann;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
            DataGridViewSelectedRowCollection savedSelectedRows = dataGridView1.SelectedRows;
            DataTable list = new DataTable();
            list.Columns.Add("No.", typeof(int));
            list.Columns.Add("Área (mm2)", typeof(string));
            list.Columns.Add("Amperes", typeof(string));

            for (int i = 0; i < objects.Count; i++)
            {
                list.Rows.Add(new object[] { i + 1, objects[i].getArea(), objects[i].getAmperes() });
            }

            dataGridView1.DataSource = list;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                int index = row.Index;
                if (index < savedSelectedRows.Count)
                {
                    row.Selected = savedSelectedRows[index].Selected;
                }                
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            int index = dataGridView1.CurrentCell.RowIndex;
            if (index >= 0 && index < objects.Count)
            {
                Bitmap image = new Bitmap(objects[index].getImage());
                Rectangle rectangle = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
                imageGraphics.DrawImage(image, rectangle);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            float totalArea = 0;
            float totalAmperes = 0;
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int index = row.Index;
                if (index >= 0 && index < objects.Count)
                {
                    totalArea += objects[index].getArea();
                    totalAmperes += objects[index].getAmperes();
                }
            }
            textBox1.Text = totalArea.ToString() + " mm2";
            textBox2.Text = totalAmperes.ToString() + " A";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            float totalArea = 0;
            float totalAmperes = 0;
            foreach (MeasuredObject measuredObject in objects)
            {
                totalArea += measuredObject.getArea();
                totalAmperes += measuredObject.getAmperes();
            }
            textBox1.Text = totalArea.ToString() + " mm2";
            textBox2.Text = totalAmperes.ToString() + " A";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int index = row.Index;
                if (index >= 0 && index < objects.Count)
                {
                    objects.Insert(index + 1, objects[index].clone());
                }
            }
            updateTable();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            for (int i = dataGridView1.Rows.Count - 1; i >= 0; i--)
            {
                objects.Insert(i + 1, objects[i].clone());
            }
            updateTable();
        }

        private void button5_Click(object sender, EventArgs e)
        {            
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                int index = row.Index;
                if (index >= 0 && index < objects.Count)
                {
                    objects.RemoveAt(index);
                }
            }
            updateTable();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            objects.Clear();
            updateTable();
        }
    }
}
