using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Umi.RhinoServices.Context;

namespace ExportLoadsToFile
{
    public partial class Form1 : Form
    {
        public List<string> Data1 = new List<string>();

        public int count { get; private set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            count = 1;
            // Command list options
            //sList<string> optionList = new List<string>();
            foreach (var o in UmiContext.Current.GetObjects())
            {
                var series = o.Data.Select(kvp => kvp.Value);
                foreach (var m in series)
                {
                    var name = m.Name;
                    if (String.IsNullOrWhiteSpace(name) == true)
                        name = "";
                    var unit = m.Units;
                    if (String.IsNullOrWhiteSpace(unit) == true)
                        unit = "No unit";
                    var timestep = m.Resolution;
                    if (String.IsNullOrWhiteSpace(timestep) == true)
                        timestep = "";
                    //else
                    //    timestep = "- " + timestep.ToString();
                    string row = name.ToString() + "[" + unit.ToString() + "]" + timestep;
                    if (lstBoxItems.Items.Contains(row) == false)
                        lstBoxItems.Items.Add(row);

                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (var m in lstBoxItems.CheckedItems)
                Data1.Add(m.ToString());
            Dispose();
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Closed(object sender, System.EventArgs e)
        {
            count -= 1;
        }
    }
}
