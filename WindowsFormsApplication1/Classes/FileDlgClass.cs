using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    class FileDlgClass
    {
        public string filebasename;
        public string filename = "";
        public string default_folder = "";

        public FileDlgClass()
        {
            filebasename = "";
            filename = "";
            default_folder = "";
        }

        public string Show()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string path = Path.Combine(Program.ApplicationPath_User, default_folder);
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "xls files (*.xls)|*.xls";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);
                filebasename = Path.GetFileNameWithoutExtension(filebasename);
            }
            openFileDialog = null;
            
            return filename;
        }
    }
     
}
