using System.Buffers.Binary;
using System.Text;
using Force.Crc32;

namespace N64Hasher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Button click event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            lbFileName.Text = "";
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == DialogResult.OK)
            {
                computeHash(fd.FileName);
            }
        }

        /// <summary>
        /// TODO: Refactor into reusable components.
        /// </summary>
        /// <param name="fileName"></param>
        private string computeHash(string fileName)
        {
            var returnHash = string.Empty;
            uint bootCodeHash;
            var fs = new FileStream(fileName, FileMode.Open);
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = new byte[4096];
                byte[] bootBytes = new byte[4032];
                fs.Read(inputBytes, 0, 4096);
                // Check Endianness based on alligned byte value, account for 64DD
                var isByteSwapped = (BitConverter.ToUInt32(inputBytes) == 0x80371240 || BitConverter.ToUInt32(inputBytes) == 0x80270740) ? true : false;
                var isLittleEndian = (BitConverter.ToUInt32(inputBytes) == 0x12408037 || BitConverter.ToUInt32(inputBytes) == 0x07408027) ? true : false;
                if (isLittleEndian)
                {
                    byte[] reversedBytes = new byte[4096];
                    for (int ii = 0; ii < inputBytes.Length; ii += 4)
                    {
                        if (ii < inputBytes.Length)
                        {
                            byte[] convertedBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(inputBytes, ii)));
                            for (int kk = 0; kk < convertedBytes.Length; kk += 2)
                            {
                                byte tempByte = convertedBytes[kk];
                                convertedBytes[kk] = convertedBytes[kk + 1];
                                convertedBytes[kk + 1] = tempByte;
                            }
                            for (int jj = 0; jj < convertedBytes.Length; jj++)
                            {
                               reversedBytes[ii + jj] = convertedBytes[3 - jj];
                            }
                        }
                    }
                    Array.Copy(reversedBytes, inputBytes, 4096);
                    Array.Copy(inputBytes, 0x40, bootBytes, 0, 4032);

                }
                else
                {
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
                var gameName = getGameTitle(inputBytes, (char)inputBytes[0x3E]);
                if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(gameName.Remove('\0'))) gameName = (string)Path.GetFileName(fileName).Split('.')[0];
                var videoType = getVideoType(inputBytes);
                var gameCode = getSerialCode(inputBytes);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                bootCodeHash = Crc32Algorithm.Compute(bootBytes, 0, bootBytes.Length);
                returnHash = Convert.ToHexString(hashBytes).ToLower();
                string videoregion = getVideoType(inputBytes);
                if (!string.IsNullOrEmpty(videoregion))
                {
                    returnHash = returnHash + $" {videoregion}";
                }

                // Get the CIC based on known hashes
                var cic = string.Empty;
                switch (bootCodeHash)
                {

                    case 0x587BD543:
                    case 0x99FC79A5:
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
                returnHash = returnHash + $"|{cic}";

                // If the rom is using the AHRH, get values directly from the rom, else use mapper.
                if (isAHRH(inputBytes))
                {
                    returnHash = returnHash + getHomeBrewSettings(inputBytes);
                }
                else
                {
                    var primaryStorage = getPrimaryStorage(gameCode);
                    if (!string.IsNullOrEmpty(primaryStorage))
                    {
                        returnHash = returnHash + $"|{primaryStorage}";
                    }
                    var secondaryStorage = getSecondaryStorage(gameCode);
                    if (!string.IsNullOrEmpty(secondaryStorage))
                    {
                        returnHash = returnHash + $"|{secondaryStorage}";
                    }
                    var rpak = getRpak(gameCode);
                    if (!string.IsNullOrEmpty(rpak))
                    {
                        returnHash = returnHash + $"|rpak";
                    }
                    var tpak = getTpak(gameCode);
                    if (!string.IsNullOrEmpty(tpak))
                    {
                        returnHash = returnHash + $"|tpak";
                    }
                    var rtc = getRtc(gameCode);
                    if(!string.IsNullOrEmpty(rtc))
                    {
                        returnHash = returnHash + $"|rtc";
                    }
                }
                returnHash = returnHash + $" # {gameName}";
            }
            return returnHash;
        }

        /// <summary>
        /// Check if the rom is using AHRH
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private bool isAHRH(byte[] bytes)
        {
            string code = Encoding.UTF8.GetString(bytes, 0x3C, 2);
            return code == "ED";
        }

        /// <summary>
        /// Using the Advanced Homebrew ROM Header 
        /// https://n64brew.dev/wiki/ROM_Header#Homebrew_ROM_Header_special_flags
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string getHomeBrewSettings(byte[] bytes)
        {
            string returnString = string.Empty;
            string rpak = (bytes[0x34] & 0x01) == 1 ? "|rpak" : string.Empty;
            string cpak = (bytes[0x34] & 0x02) == 1 ? "|cpak" : string.Empty;
            string tpak = (bytes[0x34] & 0x03) == 1 ? "|tpak" : string.Empty;
            string mouse = (bytes[0x34] & 0x80) == 1 ? "|mouse" : string.Empty; // currently not implemented
            string vru = (bytes[0x34] & 0x81) == 1 ? "|vru" : string.Empty;  // currently not implemented
            string gcc = (bytes[0x34] & 0x82) == 1 ? "|gcpad" : string.Empty; // currently not implemented
            string radkey = (bytes[0x34] & 0x83) == 1 ? "|rnkeybd" : string.Empty; // currently not implemented
            string gckey = (bytes[0x34] & 0x84) == 1 ? "|gckey" : string.Empty; // currently not implemented
            string rtc = (bytes[0x3F] & 0x1) == 1 ? "|rtc" : string.Empty;
            List<string> saves = new List<string>();
            if ((bytes[0x3F] & 0x10) == 1) saves.Add("|eeprom512");
            if ((bytes[0x34] & 0x20) == 1) saves.Add("|eeprom2k");
            if ((bytes[0x34] & 0x30) == 1) saves.Add("|sram32k");
            if ((bytes[0x34] & 0x40) == 1) saves.Add("|sram96k");
            if ((bytes[0x34] & 0x50) == 1) saves.Add("|flash128k");
            //if ((bytes[0x34] & 0x60) == 1) saves.Add("|sram128k"); // currently not implemented
            if(saves.Count > 0)
            {
                foreach(string s in saves)
                {
                    returnString = returnString + s;
                }
            }
            returnString = returnString + cpak + rpak + tpak + rtc;
            return returnString;
        }

        /// <summary>
        /// Get the string title, converted to Unicode, from the rom
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        private string getGameTitle(byte[] bytes, char region)
        {
            int codepage;
            switch (region)
            {
                case 'A':
                case 'E':
                case 'G':
                case 'N':
                    codepage = 437;
                    break;
                case 'B':
                    codepage = 860;
                    break;
                case 'C':
                    codepage = 936;
                    break;
                case 'D':
                case 'F':
                case 'H':
                case 'I':
                case 'L':
                case 'P':
                case 'S':
                case 'U':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    codepage = 858;
                    break;
                case 'J':
                    codepage = 932;
                    break;
                case 'K':
                    codepage = 949;
                    break;
                default:
                    codepage = 437;
                    break;
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return Encoding.GetEncoding(codepage).GetString(bytes, 0x20, 0x14).Trim();
        }

        /// <summary>
        /// Drag and Drop event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            textBox1.Enabled = false;
            button1.Enabled = false;
            textBox1.Text = string.Empty;
            lbFileName.Text = string.Empty;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            // Check to see if the item dropped was a folder
            if (Directory.Exists(files[0]))
            {
                textBox1.Text = "Working...";
                var fileNames = Directory.EnumerateFiles(files[0], "*.*64", SearchOption.AllDirectories);
                int totalFiles = fileNames.Count();
                lbFileName.Text = $"Total files found: {totalFiles}";
                SaveFileDialog sfd = new SaveFileDialog();
                int counter = 1;

                sfd.ShowDialog();
                try
                {
                    if (File.Exists(sfd.FileName))
                    {
                        File.Delete(sfd.FileName);
                    }
                    using (StreamWriter sr = new StreamWriter(sfd.FileName))
                    {
                        foreach (string file in fileNames)
                        {
                            textBox1.Text = $"Working... computing file {counter} out of {totalFiles}.";
                            textBox1.Update();
                            sr.WriteLine(computeHash(file));
                            counter++;
                        }
                    }
                    textBox1.Text = $"Complete! Saved to {sfd.FileName}";
                }
                catch(UnauthorizedAccessException)
                {
                    MessageBox.Show("You do not have permission to this directory.");
                    textBox1.Text = $"Error. No permission.";
                }

            }
            else
            {
                lbFileName.Text = files[0].Split('\\').Last();
                textBox1.Text = computeHash(files[0]);
            }
            textBox1.Enabled = true;
            button1.Enabled = true;
        }

        /// <summary>
        /// Drag and Drop decorator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// Get the unique game code from bytestream
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string getSerialCode(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes, 0x3B, 3);
        }

        /// <summary>
        /// Get video type (pal/NTSC)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private string getVideoType(byte[] bytes)
        {
            var region = bytes[0x3E];
            switch ((char)region)
            {
                case ('C'):
                case ('D'):
                case ('F'):
                case ('H'):
                case ('I'):
                case ('L'):
                case ('P'):
                case ('S'):
                case ('U'):
                case ('W'):
                case ('X'):
                case ('Y'):
                case ('Z'):
                    return "pal";
                case ('A'):
                case ('B'):
                case ('E'):
                case ('G'):
                case ('J'):
                case ('K'):
                case ('N'):
                default:
                    return "ntsc";
            }

        }

        /// <summary>
        /// Primary cart storage by unique retail game code
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        private string getPrimaryStorage(string gameCode)
        {
            switch (gameCode)
            {
                case "NO7":
                case "NO2":
                case "NBY":
                case "NDY":
                case "NAG":
                case "NS3":
                case "NAY":
                case "NBS":
                case "NBE":
                case "NAS":
                case "NAR":
                case "NAC":
                case "NAM":
                case "N32":
                case "NAH":
                case "NLC":
                case "NBM":
                case "NBV":
                case "NBJ":
                case "NB4":
                case "NBX":
                case "NBQ":
                case "NZO":
                case "NNS":
                case "NMU":
                case "NBF":
                case "NBC":
                case "NBP":
                case "NHA":
                case "NBD":
                case "NBO":
                case "NOW":
                case "NBL":
                case "NBU":
                case "NB3":
                case "NCL":
                case "NCD":
                case "ND3":
                case "ND4":
                case "NTS":
                case "N2V":
                case "NV2":
                case "NPK":
                case "NP2":
                case "NCR":
                case "NCG":
                case "NGT":
                case "NCU":
                case "NT4":
                case "NDW":
                case "NGA":
                case "NDE":
                case "NDM":
                case "NDH":
                case "NDN":
                case "NDZ":
                case "NWI":
                case "NST":
                case "NET":
                case "NMX":
                case "NEG":
                case "NG2":
                case "NHG":
                case "NFR":
                case "NFS":
                case "N9F":
                case "N8I":
                case "F7I":
                case "N7I":
                case "NFG":
                case "NKA":
                case "NFF":
                case "NFD":
                case "NF0":
                case "NF9":
                case "NSI":
                case "NGP":
                case "NG5":
                case "NGX":
                case "NGD":
                case "NGL":
                case "NX3":
                case "NX2":
                case "NGM":
                case "NGN":
                case "NGC":
                case "NHS":
                case "NHC":
                case "NHX":
                case "NHK":
                case "NHW":
                case "NHV":
                case "NHY":
                case "NHT":
                case "NH5":
                case "NWB":
                case "NWS":
                case "NIS":
                case "NJP":
                case "N3H":
                case "NDS":
                case "NJE":
                case "NCO":
                case "NGS":
                case "NJ5":
                case "NP6":
                case "NFY":
                case "NKI":
                case "NSB":
                case "NK4":
                case "NKK":
                case "NLG":
                case "N8M":
                case "N9M":
                case "NMD":
                case "NFL":
                case "N2M":
                case "NMJ":
                case "NMM":
                case "NKT":
                case "NM9":
                case "NHM":
                case "NV3":
                case "NAI":
                case "NMB":
                case "NBR":
                case "NMG":
                case "NMS":
                case "NM4":
                case "NMY":
                case "NP9":
                case "NMR":
                case "NNM":
                case "N9C":
                case "NN2":
                case "NNB":
                case "NXG":
                case "NBA":
                case "NB2":
                case "NWZ":
                case "NB9":
                case "NJA":
                case "N9B":
                case "NNL":
                case "NSO":
                case "NRI":
                case "NBI":
                case "NFB":
                case "NSZ":
                case "NBZ":
                case "NQ8":
                case "NQ9":
                case "NQB":
                case "NQC":
                case "NN9":
                case "NHO":
                case "NHL":
                case "NH9":
                case "NNC":
                case "NCE":
                case "NOF":
                case "NHN":
                case "NOM":
                case "NYP":
                case "NPP":
                case "NPD":
                case "NPX":
                case "NPL":
                case "NPU":
                case "NQK":
                case "NKQ":
                case "NQ2":
                case "NNG":
                case "NR6":
                case "NKR":
                case "NRA":
                case "NWQ":
                case "N2P":
                case "NRP":
                case "NRT":
                case "NY2":
                case "NFQ":
                case "NRD":
                case "N22":
                case "NRV":
                case "NRO":
                case "NRR":
                case "NRX":
                case "NRK":
                case "NR2":
                case "NCS":
                case "NSH":
                case "NRU":
                case "NSF":
                case "NSY":
                case "NDC":
                case "NSD":
                case "NSG":
                case "NDG":
                case "NTO":
                case "NS2":
                case "NSK":
                case "NK2":
                case "NDT":
                case "NPR":
                case "NIV":
                case "NSL":
                case "NR3":
                case "NB6":
                case "NBW":
                case "NS4":
                case "NSX":
                case "BSX":
                case "NSP":
                case "NPZ":
                case "NTW":
                case "NTA":
                case "NWK":
                case "NTT":
                case "NTF":
                case "NTQ":
                case "N3T":
                case "NGB":
                case "NTR":
                case "NGR":
                case "NL2":
                case "NTH":
                case "N3P":
                case "NT2":
                case "NTK":
                case "NTU":
                case "NRW":
                case "NV8":
                case "NVG":
                case "NVC":
                case "NVR":
                case "NA2":
                case "NVP":
                case "NWR":
                case "NW8":
                case "NWG":
                case "NWV":
                case "NWM":
                case "NW3":
                case "NWN":
                case "NWT":
                case "NWD":
                case "NWP":
                case "NJ2":
                case "N8W":
                case "NWO":
                case "NTI":
                case "NWW":
                case "NWX":
                case "NXF":
                case "NMZ":
                    return "cpak";
                case "NB7":
                case "NFU":
                case "NCW":
                case "NDO":
                case "ND2":
                case "N3D":
                case "NDR":
                case "NIM":
                case "NMV":
                case "NM8":
                case "NEV":
                case "NRZ":
                case "NR7":
                case "NGU":
                case "NEP":
                case "PEP":
                case "NYS":
                    return "eeprom2k";
                case "NGE":
                case "NHF":
                case "NTC":
                case "NOS":
                case "NER":
                case "NSA":
                case "NAB":
                case "NTN":
                case "NBK":
                case "NFH":
                case "NBH":
                case "NCT":
                case "NCH":
                case "NXO":
                case "NCX":
                case "NCZ":
                case "ND6":
                case "NDQ":
                case "NN6":
                case "NJM":
                case "NFW":
                case "NF2":
                case "NGV":
                case "NPG":
                case "NIJ":
                case "NIC":
                case "NLL":
                case "NLR":
                case "NDU":
                case "NFX":
                case "NLB":
                case "CLB":
                case "NMW":
                case "NML":
                case "NTM":
                case "NMI":
                case "NMO":
                case "NEA":
                case "NPW":
                case "NPT":
                case "NSU":
                case "NDK":
                case "NSV":
                case "NFP":
                case "NS6":
                case "NNA":
                case "NRS":
                case "NSW":
                case "NSC":
                case "NSM":
                case "NSS":
                case "NTX":
                case "NT6":
                case "NTP":
                case "NTJ":
                case "NRC":
                case "NTB":
                case "NIR":
                case "NVL":
                case "NWL":
                case "NWC":
                case "NWU":
                case "NAD":
                case "NYK":
                    return "eeprom512";
                case "NCC":
                case "NDA":
                case "NAF":
                case "NJF":
                case "NKJ":
                case "NZS":
                case "NM6":
                case "NCK":
                case "NMQ":
                case "NPF":
                case "CP2":
                case "NP3":
                case "NFN":
                case "NPN":
                case "NPO":
                case "NRH":
                case "NSQ":
                case "NT9":
                case "NW4":
                    return "flash128k";
                case "NTE":
                case "NVB":
                case "NB5":
                case "NYW":
                case "NFZ":
                case "CFZ":
                case "NG6":
                case "NIB":
                case "NZL":
                case "CZL":
                case "CZG":
                case "NKG":
                case "NMF":
                case "NAL":
                case "NOB":
                case "CPS":
                case "NRE":
                case "NW2":
                    return "sram32k";
                case "CDZ":
                    return "sram96k";
                default:
                    return string.Empty;

            }
        }

        /// <summary>
        /// Get secondary storage from unique game code
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        private string getSecondaryStorage(string gameCode)
        {
            switch (gameCode)
            {
                case "NGT":
                case "NMX":
                case "NNB":
                case "NPP":
                case "NPD":
                    return "eeprom2k";
                case "NS3":
                case "NBM":
                case "NBV":
                case "NMU":
                case "NBC":
                case "NHA":
                case "NBD":
                case "NP2":
                case "NCR":
                case "NCG":
                case "NCU":
                case "NDY":
                case "NFG":
                case "NKA":
                case "NGL":
                case "NGC":
                case "NFY":
                case "NKI":
                case "NKT":
                case "NMS":
                case "NMR":
                case "NMG":
                case "NRA":
                case "NK2":
                case "NB6":
                case "NTW":
                case "NWR":
                case "NMZ":
                    return "eeprom512";
                case "NRI":
                case "NWX":
                case "NSI":
                case "NGP":
                case "NJ5":
                case "NP6":
                case "NS4":
                case "NA2":
                case "NVP":
                    return "sram32k";
                default: return string.Empty;

            }
        }

        /// <summary>
        /// Get rpak based on unique game code
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        private string getRpak(string gameCode)
        {
            switch (gameCode)
            {
                case "NGE":
                case "NO7":
                case "NTE":
                case "NTC":
                case "NOS":
                case "NO2":
                case "NBY":
                case "NDY":
                case "NER":
                case "NSA":
                case "NAB":
                case "NSB":
                case "NBS":
                case "NBE":
                case "NAS":
                case "NAR":
                case "NAC":
                case "NAM":
                case "N32":
                case "NAH":
                case "NLC":
                case "NBV":
                case "NBN":
                case "NBK":
                case "NB7":
                case "NFH":
                case "NB4":
                case "NVB":
                case "NJQ":
                case "NBX":
                case "NBQ":
                case "NZO":
                case "NNS":
                case "NMU":
                case "NBF":
                case "NB5":
                case "NBP":
                case "NBH":
                case "NBD":
                case "NBL":
                case "NB3":
                case "NCL":
                case "NCD":
                case "NCT":
                case "N2V":
                case "NV2":
                case "NCB":
                case "NP2":
                case "NCH":
                case "NCG":
                case "NGT":
                case "NCC":
                case "NFU":
                case "NXO":
                case "NCW":
                case "NCZ":
                case "NT4":
                case "NDW":
                case "NDF":
                case "NGA":
                case "ND6":
                case "NDE":
                case "CDZ":
                case "NDO":
                case "ND2":
                case "N3D":
                case "NDN":
                case "NDZ":
                case "NWI":
                case "NMX":
                case "NEG":
                case "NG2":
                case "NFR":
                case "NFW":
                case "NF2":
                case "NFZ":
                case "CFZ":
                case "NFG":
                case "NKA":
                case "NFF":
                case "NFD":
                case "NF0":
                case "NG6":
                case "NG5":
                case "N3H":
                case "NGX":
                case "NGD":
                case "NGL":
                case "NX3":
                case "NGM":
                case "NGC":
                case "NHC":
                case "NPG":
                case "NHK":
                case "NHW":
                case "NHV":
                case "NHY":
                case "NHT":
                case "NWB":
                case "NIJ":
                case "NIC":
                case "NIS":
                case "NIB":
                case "NCO":
                case "NJF":
                case "NFY":
                case "NKJ":
                case "NK4":
                case "NKE":
                case "NKK":
                case "NLL":
                case "NZS":
                case "NZL":
                case "CZL":
                case "CZG":
                case "NLG":
                case "NLR":
                case "NDU":
                case "NFX":
                case "N8M":
                case "N9M":
                case "NMD":
                case "NFL":
                case "N2M":
                case "NMT":
                case "NKG":
                case "NMF":
                case "NLB":
                case "CLB":
                case "NMW":
                case "NMV":
                case "NM8":
                case "NM9":
                case "NM6":
                case "NHM":
                case "NML":
                case "NV3":
                case "NMB":
                case "NBR":
                case "NMI":
                case "NMG":
                case "NM3":
                case "NM4":
                case "NMY":
                case "NP9":
                case "NMR":
                case "N9C":
                case "NN2":
                case "NCK":
                case "NNB":
                case "NBA":
                case "NB2":
                case "NWZ":
                case "NJA":
                case "N9B":
                case "NNL":
                case "NEV":
                case "NBI":
                case "NFB":
                case "NSZ":
                case "NBZ":
                case "NQ8":
                case "NQ9":
                case "NQB":
                case "NQC":
                case "NN9":
                case "NHO":
                case "NHL":
                case "NH9":
                case "NNC":
                case "NAL":
                case "NCE":
                case "NOF":
                case "NMQ":
                case "NYP":
                case "NPD":
                case "NGP":
                case "NPX":
                case "NPT":
                case "NQK":
                case "NKQ":
                case "NQ2":
                case "NNG":
                case "NR6":
                case "NRA":
                case "NWQ":
                case "N2P":
                case "NRP":
                case "NFQ":
                case "NRD":
                case "N22":
                case "NRE":
                case "NRV":
                case "NRZ":
                case "NRO":
                case "NRR":
                case "NRH":
                case "NSU":
                case "NRG":
                case "NR2":
                case "NCS":
                case "NRU":
                case "NSF":
                case "NDC":
                case "NSD":
                case "NSK":
                case "NK2":
                case "NDT":
                case "NPR":
                case "NIV":
                case "NSV":
                case "NSL":
                case "NFP":
                case "NS6":
                case "NNA":
                case "NEP":
                case "PEP":
                case "NRS":
                case "NSQ":
                case "NR3":
                case "NBW":
                case "NSS":
                case "NSX":
                case "BSX":
                case "NSP":
                case "NPZ":
                case "NTA":
                case "NTX":
                case "NWK":
                case "NTJ":
                case "NTF":
                case "NTQ":
                case "N3T":
                case "NGB":
                case "NRC":
                case "NTR":
                case "NGR":
                case "NL2":
                case "NTH":
                case "NTB":
                case "N3P":
                case "NT2":
                case "NTK":
                case "NRW":
                case "NIR":
                case "NV8":
                case "NVG":
                case "NA2":
                case "NVP":
                case "NVL":
                case "NWL":
                case "NWG":
                case "NWV":
                case "NWM":
                case "NW3":
                case "NWN":
                case "NW2":
                case "NWF":
                case "NWC":
                case "NWD":
                case "NWP":
                case "NWO":
                case "NTI":
                case "NW4":
                case "NWW":
                case "NWX":
                case "NXF":
                case "NYK":
                case "NYS":
                    return "rpak";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Get RTC based on unique game code (currently only Animal Crossing/GameShark)
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        private string getRtc (string gameCode)
        {
            switch(gameCode)
            {
                case "NAF":
                    return "rtc";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Get Tpak based on unique game code
        /// </summary>
        /// <param name="gameCode"></param>
        /// <returns></returns>
        private string getTpak(string gameCode)
        {
            switch (gameCode)
            {
                case "NCG":
                case "NP6":
                case "NML":
                case "CPS":
                case "CP2":
                case "NP3":
                case "NPO":
                case "NB6":
                case "NS4":
                case "N0H":
                    return "tpak";
                default: return string.Empty;
            }
        }
    }
}