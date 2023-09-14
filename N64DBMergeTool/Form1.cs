using System.Security.Cryptography.X509Certificates;

namespace N64DBMergeTool
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            lbDataBase.Text = "";
            lbAddtions.Text = "";
            OpenFileDialog ofc1 = new OpenFileDialog();
            OpenFileDialog ofc2 = new OpenFileDialog();
            if (!(ofc1.ShowDialog(this) == DialogResult.OK)) { return; }
            if (!(ofc2.ShowDialog(this) == DialogResult.OK) ) { return; }
            if (!ofc1.CheckFileExists || !ofc2.CheckFileExists) { return;  }
            var mainFile = ofc1.FileName;
            var injectFile = ofc2.FileName;
            lbDataBase.Text = mainFile;
            lbAddtions.Text = injectFile;
            lbDataBase.Update();
            lbAddtions.Update();
            List<lineItem> files = new List<lineItem>();
            if (File.Exists(ofc1.FileName + ".bak")) File.Delete(ofc1.FileName + ".bak");
            File.Copy(ofc1.FileName, ofc1.FileName + ".bak");
            using (StreamReader sr = new StreamReader(mainFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    var litem = new lineItem();
                    litem.hash = line.Split(' ')[0].Trim();
                    litem.capabilities = line.Split(" ")[1].Trim();
                    litem.name = line.Split(' ', 4, StringSplitOptions.TrimEntries)[3].Trim();
                    files.Add(litem);
                }
            }
            List<lineItem> additionFiles = new List<lineItem>();
            int startLineCount = files.Count;
            using (StreamReader sr = new StreamReader(injectFile))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    var litem = new lineItem();
                    litem.hash = line.Split(' ')[0].Trim();
                    litem.capabilities = line.Split(" ")[1].Trim();
                    litem.name = line.Split(' ', 4, StringSplitOptions.TrimEntries)[3].Trim();
                    additionFiles.Add(litem);
                }
            }
            int totalLinesToCheck = additionFiles.Count;
            List<lineItem> newItems = new List<lineItem>();
            foreach (lineItem additionFile in additionFiles)
            {
                if (!files.Any(x => x.hash.StartsWith(getHash(additionFile.hash))))
                {
                    files.Add(additionFile);
                    newItems.Add(additionFile);
                }
            }
            int finishLineCount = files.Count;
            int diffCount = finishLineCount - startLineCount;

            if (diffCount > 0)
            {
                var outputList = files.OrderBy(x => x.name).ToList();
                if (File.Exists(ofc1.FileName))
                {
                    File.Delete(ofc1.FileName);
                }
                using (StreamWriter sw = new StreamWriter(ofc1.FileName))
                {
                    foreach (var item in outputList)
                    {
                        sw.WriteLine(item.hash + " " + item.capabilities + " # " + item.name);
                    }
                }
                using (StreamWriter sw = new StreamWriter(ofc1.FileName + ".dif"))
                {
                    foreach (var item in newItems)
                    {
                        sw.WriteLine(item.hash + " " + item.capabilities + " # " + item.name);
                    }
                }
            }
            MessageBox.Show($"New listings added: {diffCount}");

        }

        private string getHash(string lineitem)
        {
            return lineitem.Split(' ')[0];
        }
    }
    class lineItem
    {
        public string hash;
        public string capabilities;
        public string name;
    }
}