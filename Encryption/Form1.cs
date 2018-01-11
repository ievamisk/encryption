using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;


namespace Encryption
{
    public partial class Form1 : Form
    {
        private string backupPath;
        private string zipPath;
        private string newDir;
        private string selectedPath;
        private int count;
        StreamWriter sw;
        public ManualResetEvent mre = new ManualResetEvent(true);

        Thread runTask;

        bool pause;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    directoryBox.Text = (fbd.SelectedPath).ToString();
                    selectedPath = directoryBox.Text;
                    backupPath = @"" + selectedPath + "-Copy";
                    zipPath = @"" + selectedPath + "\\" + "Zipped.zip";
                }
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            
            pause = false;
            if (encryptButton.Checked)
            {
                runTask = new Thread(() => EncryptFile(selectedPath));
                runTask.Start();

            }
            else if (decryptButton.Checked)
            {
                runTask = new Thread(() => DecryptFile(selectedPath));
                runTask.Start();
            }
        }

        #region ENCRYPTION
        private void moveItems(string startPath, string destinationPath)
        {
            try
            {
                Directory.CreateDirectory(backupPath);

                foreach (var subdirectory in Directory.GetDirectories(startPath))
                {
                    string dirName = Path.GetFileName(subdirectory);
                    newDir = String.Format(@"" + destinationPath + "\\" + dirName);
                    Directory.Move(subdirectory, Path.Combine(subdirectory, newDir));   
                }

                ZipFile.CreateFromDirectory(backupPath, zipPath);

                foreach (var file in Directory.GetFiles(startPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinationPath, fileName);
                    File.Copy(file, destFile);
                    count++;
                }
                this.BeginInvoke((MethodInvoker)delegate
                {
                    progressBar1.Value = 0;
                    progressBar1.Maximum = count * 2;
                    progressBar1.Minimum = 0;

                });

                foreach (var file in Directory.GetFiles(backupPath)) {
                    if (file.Contains("Zipped.zip"))
                    {
                        File.Delete(file);
                    }

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Ooops! Problem with moving items :(");
            }
}


        
        public byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public void EncryptFile(string path)
        {
            string password = "12345678";
            moveItems(selectedPath, backupPath);

            foreach (var file in Directory.GetFiles(path))
            {
                mre.WaitOne(Timeout.Infinite);
                byte[] bytesToBeEncrypted = File.ReadAllBytes(file);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

                File.WriteAllBytes(file, bytesEncrypted);

                this.BeginInvoke((MethodInvoker)delegate
                {
                    progressBar1.Value +=1;

                });
            }
            CreateHash(selectedPath);
            MessageBox.Show("Encryption successful!");

        }

        public string GetHash(string file)
        {
            var sBuilder = new StringBuilder();

            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(file));

                for (int i = 0; i < hash.Length; i++)
                {
                    sBuilder.Append(hash[i].ToString("x2"));
                }
            }
            return sBuilder.ToString();
        }

        public void CreateHash(string path)
        {
            try
            {
                sw = new StreamWriter(@"D:\Users\ievam\Desktop\hash.txt", true);

                foreach (string file in Directory.GetFiles(path))
                {
                    sw.WriteLine(GetHash(file));
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        progressBar1.Value += 1;

                    });   
                }
                sw.Close();
            }
            catch (Exception)
            {
               MessageBox.Show("Oops! Something went wrong...");
            }
        }
        #endregion

        #region DECRYPTION
        public byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public void DecryptFile(string path)
        {
            string password = "12345678";
            string[] hashes = File.ReadAllLines(@"D:\Users\ievam\Desktop\hash.txt");
            int count = Directory.GetFiles(path).Length;

            this.BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = count;
                progressBar1.Minimum = 0;
            });

            foreach (string file in Directory.GetFiles(path))
            {
                mre.WaitOne(Timeout.Infinite);

                if (hashes.Contains(GetHash(file)))
                {
                    byte[] bytesToBeDecrypted = File.ReadAllBytes(file);
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

                    passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

                    byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

                    File.WriteAllBytes(file, bytesDecrypted);

                    if (file.Contains("Zipped.zip")) {
                        unzipSubfolders(zipPath, selectedPath);
                    }
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        progressBar1.Value += 1;

                    });
                }
                else
                {
                    MessageBox.Show("Oops, different hash!");
                }
            }
            MessageBox.Show("Decryption successful!");

            DirectoryInfo di = new DirectoryInfo(backupPath);

           }

        private void unzipSubfolders(string startPath, string destinationPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(startPath, selectedPath);
                File.Delete(startPath);
            }
            catch (Exception)
            {
                MessageBox.Show("Problem with unzipping the directory.");
            }
        }

        private void deleteItems(string startPath)
        {
            DirectoryInfo di = new DirectoryInfo(startPath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subdirectory in di.GetDirectories())
            {
                foreach (FileInfo file in subdirectory.GetFiles())
                {
                    file.Delete();
                }
                subdirectory.Delete();
            }
        }

        #endregion

        private void cancelButton_Click(object sender, EventArgs e)
        {
            try
            {
                runTask.Abort();
                deleteItems(selectedPath);
                moveItems(backupPath, selectedPath);
                foreach (var file in Directory.GetFiles(selectedPath))
                {
                    if (file.Contains("Zipped.zip"))
                    {
                        File.Delete(file);
                    }
                }
                MessageBox.Show("Successfully canceled!");
            }
            catch (Exception)
            {
                MessageBox.Show("Oops! Something went wrong...");
            }
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            pause = !pause;

            if (pause) 
            {
                pauseButton.Text = "Continue";
                mre.Reset();
                textBox1.Text = "Process is stopped";
            }
            else
            {
                pauseButton.Text = "Pause";
                mre.Set();
                textBox1.Text = "Process is continued";

            }
        }
    }
}


