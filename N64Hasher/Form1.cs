using System.Buffers.Binary;
using Force.Crc32;

namespace N64Hasher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            lbFileName.Text = "";
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                computeHash(fd.FileName);
            }
        }

        private void computeHash(string fileName)
        {
            lbFileName.Text = fileName.Split('\\').Last();
            var returnHash = string.Empty;
            uint bootCodeHash;
            var bootCode = string.Empty;
            var isByteSwapped = fileName.Split('.').Last().Trim().ToLower() == "v64";
            var isLittleEndian = fileName.Split('.').Last().Trim() == "n64";
            var fs = new FileStream(fileName, FileMode.Open);
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = new byte[4096];
                byte[] bootBytes = new byte[4032];
                if (isLittleEndian)
                {
                    byte[] tempBytes = new byte[4096];
                    byte[] reversedBytes = new byte[4096];
                    fs.Read(tempBytes, 0, 4096);
                    for (int ii = 0; ii < tempBytes.Length; ii += 4)
                    {
                        if (ii < tempBytes.Length)
                        {
                            byte[] convertedBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(tempBytes, ii)));
                            for (int jj = 0; jj < convertedBytes.Length; jj++)
                            {
                                reversedBytes[ii + jj] = convertedBytes[jj];
                            }
                        }
                    }
                    Array.Copy(reversedBytes, inputBytes, 4096);
                    Array.Copy(tempBytes, 0x40, bootBytes, 0, 4032);

                }
                else
                {
                    fs.Read(inputBytes, 0, 4096);
                    fs.Position = 0x40;
                    fs.Read(bootBytes, 0, 4032);
                }
                fs.Close();

                if (isByteSwapped)
                {
                    for (int ii = 0; ii < inputBytes.Length; ii += 2)
                    {
                        byte tempByte = inputBytes[ii];
                        inputBytes[ii] = inputBytes[ii + 1];
                        inputBytes[ii + 1] = tempByte;
                    }
                    for (int ii = 0; ii < bootBytes.Length; ii += 2)
                    {
                        byte tempByte = bootBytes[ii];
                        bootBytes[ii] = bootBytes[ii + 1];
                        bootBytes[ii + 1] = tempByte;
                    }
                }
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                bootCodeHash = Crc32Algorithm.Compute(bootBytes, 0, bootBytes.Length);
                returnHash = Convert.ToHexString(hashBytes).ToLower();
            }
            textBox1.Text = returnHash;

            // Get the CIC based on the first 4032 bytes of boot code
            var cic = string.Empty;
            switch (bootCodeHash)
            {

                case 0x587BD543:
                    cic = "5101";
                    break;
                case 0x6170A4A1:
                    cic = "6101";
                    break;
                case 0x90BB6CB5:
                    cic = "6102";
                    break;
                case 0x0B050EE0:
                    cic = "6103";
                    break;
                case 0x98BC2C86:
                    cic = "6105";
                    break;
                case 0xACC8580A:
                    cic = "6106";
                    break;
                case 0x009E9EA3:
                    cic = "7102";
                    break;
                case 0x0E018159:
                    cic = "8303";
                    break;
                default:
                    cic = "6102";
                    break;
            }
            textBox2.Text = cic;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            computeHash(files[0]);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
    }
}