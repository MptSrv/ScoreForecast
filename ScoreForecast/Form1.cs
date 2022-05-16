using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ScoreForecast
{
    public partial class Form1 : Form
    {
        private Outcome[] _outcomes;

        public Form1()
        {
            InitializeComponent();

            Init();
        }

        private void Init()
        {
            dataGridView1.Rows.Add();
            dataGridView1.Rows[0].HeaderCell.Value = "Хозяева";
            dataGridView1.Rows.Add();
            dataGridView1.Rows[1].HeaderCell.Value = "Гости";

            for (int i = 1; i < dataGridView1.Columns.Count; i++)
            {
                dataGridView1.Columns[i].ReadOnly = true; // (n + 1)-ый гол возможно проставить только после выставления n-го гола
            }

            _outcomes = new Outcome[0];
        }          

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            bool isHostGoal = Convert.ToBoolean(dataGridView1.Rows[0].Cells[e.ColumnIndex].Value);
            bool isGuestGoal = Convert.ToBoolean(dataGridView1.Rows[1].Cells[e.ColumnIndex].Value);

            // В одном столбце - только один гол (~ радиокнопка)
            if (isHostGoal && isGuestGoal)
            {
                dataGridView1.Rows[0].Cells[e.ColumnIndex].Value = false;
                dataGridView1.Rows[1].Cells[e.ColumnIndex].Value = false;
                dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = true;
            }

            if (e.ColumnIndex == dataGridView1.Columns.Count - 1)
                return;

            if (isHostGoal || isGuestGoal)
            {
                dataGridView1.Columns[e.ColumnIndex + 1].ReadOnly = false; // после выставления n-го гола, можно проставить (n + 1)-ый
            }
            else
            {
                // Удаление n-го гола означает удаление всех голов с номером > n
                for (int i = e.ColumnIndex + 1; i < dataGridView1.Columns.Count; i++)
                {
                    dataGridView1.Rows[0].Cells[i].Value = false;
                    dataGridView1.Rows[1].Cells[i].Value = false;
                    dataGridView1.Columns[i].ReadOnly = true;
                }
            }
        }

        private void dataGridView1_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            dataGridView1.EndEdit();            

            int hostScore = 0;
            int guestScore = 0;

            Outcome[] temporaryBuffer = new Outcome[dataGridView1.Columns.Count];
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                bool isHostGoal = Convert.ToBoolean(dataGridView1.Rows[0].Cells[i].Value);
                bool isGuestGoal = Convert.ToBoolean(dataGridView1.Rows[1].Cells[i].Value);

                hostScore += isHostGoal ? 1 : 0;
                guestScore += isGuestGoal ? 1 : 0;

                temporaryBuffer[i] = isHostGoal ? Outcome.Host : Outcome.Guest;
            }

            _outcomes = new Outcome[hostScore + guestScore];
            Array.Copy(temporaryBuffer, _outcomes, _outcomes.Length); // помещаем в массив outcomes текущий счет (упорядоченный)
            

            label1.Text = $"Текущий счет: {hostScore} : {guestScore}";
        }

        private void button1_Click(object sender, EventArgs e)
        {         
            /*
             * Оставшееся время в минутах; в расчетах нумерация массивов начинается с нуля
             * */
            int remainingTime = (int)numericUpDown1.Value;
            int startGoal = (int)numericUpDown2.Value - 1;
            int endGoal = (int)numericUpDown3.Value - 1;

            if (_outcomes.Length > endGoal)
            {
                MessageBox.Show("Test", "Неверные входные данные", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            OutcomeForecast outcomeForecast = new OutcomeForecast(remainingTime, _outcomes, startGoal, endGoal);

            var poisson = outcomeForecast.Calculate();            
            label5.Text = poisson.ToString(); // выводим результаты расчета вероятностой модели на основе распределения Пуассона

            var monteCarlo = outcomeForecast.GetTestResult();
            label6.Text = monteCarlo.ToString(); // выводим результаты моделирования методом Монте-Карло
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                dataGridView1.Columns[i].DefaultCellStyle.BackColor = Color.White;
            }
            int startGoal = (int)numericUpDown2.Value - 1;
            int endGoal = (int)numericUpDown3.Value - 1;

            for (int i = startGoal; i <= endGoal; i++)
            {
                dataGridView1.Columns[i].DefaultCellStyle.BackColor = Color.LightGreen;
            }
        }        
    }
}
