﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.IO.Compression;
using static System.Net.Mime.MediaTypeNames;
using System.IO.Pipes;
using System.Text.Json;
using System.Diagnostics;

namespace Xilinx_programer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Boot_Info
        {
            public string hw { get; set; }
            public string offset { get; set; }

            public bool IsValid
            {
                get { return hw != null && offset != null; }
            }

            public Boot_Info(string hw, string offset)
            {
                this.hw = hw;
                this.offset = offset;
            }
        }

        Boot_Info boot_info;
        string boot_bin_path;
        System.Diagnostics.Process flash_process;


        public MainWindow()
        {
            InitializeComponent();

            this.boot_info = new Boot_Info("", "");
            this.boot_bin_path = "";
            this.flash_process = new System.Diagnostics.Process();
        }

        private string GetTemporaryDirectory()
        {
            string tempDirectory;

            do
            {
                tempDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

            } while (System.IO.Directory.Exists(tempDirectory));

            System.IO.Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "FLASH File (*.flash)|*.flash";

            if (openFileDialog.ShowDialog() == true)
            {


                string extractPath = this.GetTemporaryDirectory();
                bool is_BOOT_bin_found = false;
                bool is_BOOT_json_found = false;
                bool is_fw_info_found = false;
                bool is_sw_info_found = false;

                using (ZipArchive archive = ZipFile.OpenRead(openFileDialog.FileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == "BOOT.bin")
                        {
                            is_BOOT_bin_found = true;
                        }
                        else if (entry.FullName == "boot.json")
                        {
                            is_BOOT_json_found = true;
                        }
                        else if (entry.FullName == "fw.txt")
                        {
                            is_fw_info_found = true;
                        }
                        else if (entry.FullName == "sw.txt")
                        {
                            is_sw_info_found = true;
                        }
                        else
                        {
                            continue;
                        }




                        // Gets the full path to ensure that relative segments are removed.
                        string destinationPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(extractPath, entry.FullName));

                        entry.ExtractToFile(destinationPath);

                    }
                }

                if (!is_BOOT_bin_found)
                {
                    // Error
                    MessageBox.Show("BOOT.bin not found", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    this.boot_bin_path = System.IO.Path.Combine(extractPath, "BOOT.bin");
                }

                if (!is_BOOT_json_found)
                {
                    // Error
                    MessageBox.Show("BOOT.json not found", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (is_BOOT_json_found)
                {
                    using (FileStream fs = File.OpenRead(System.IO.Path.Combine(extractPath, "boot.json")))
                    {
                        using (var streamReader = new StreamReader(fs, Encoding.UTF8))
                        {
                            string boot_json_str = streamReader.ReadToEnd();
                            var boot_info_tmp = JsonSerializer.Deserialize<Boot_Info>(boot_json_str);

                            if (boot_info_tmp != null && boot_info_tmp.IsValid)
                            {
                                this.boot_info = boot_info_tmp;

                                this.tb_board_name.Text = boot_info_tmp.hw;
                                string img_path = System.IO.Path.GetFullPath(System.IO.Path.Combine("boards", boot_info_tmp.hw, "board.png"));

                                if (System.IO.File.Exists(img_path))
                                {
                                    this.imgBoard.Source = new BitmapImage(new Uri(img_path));

                                }
                            }
                            else
                            {
                                // error
                                MessageBox.Show("Parser boot info error", "Image Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                        }
                    }
                }

                this.txtBox_boot.Text = openFileDialog.FileName;
                this.btnProg.IsEnabled = true;

                if (is_fw_info_found)
                {
                    using (FileStream fs = File.OpenRead(System.IO.Path.Combine(extractPath, "fw.txt")))
                    {
                        using (var streamReader = new StreamReader(fs, Encoding.UTF8))
                        {
                            this.tb_fw_info.Text = streamReader.ReadToEnd();
                        }
                    }
                }

                if (is_sw_info_found)
                {
                    using (FileStream fs = File.OpenRead(System.IO.Path.Combine(extractPath, "sw.txt")))
                    {
                        using (var streamReader = new StreamReader(fs, Encoding.UTF8))
                        {
                            this.tb_sw_info.Text = streamReader.ReadToEnd();
                        }
                    }
                }

                this.btnProg.IsEnabled = true;
                this.gridImgInfo.Visibility = Visibility.Visible;
            }

        }

        private void Button_Prog_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(this.boot_bin_path))
            {
                using (ZipArchive archive = ZipFile.OpenRead(this.txtBox_boot.Text))
                {
                    string extractPath = this.GetTemporaryDirectory();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == "BOOT.bin")
                        {
                            // Gets the full path to ensure that relative segments are removed.
                            string destinationPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(extractPath, entry.FullName));

                            entry.ExtractToFile(destinationPath);
                            this.boot_bin_path = destinationPath;
                            break;
                        }

                    }

                }


            }

            this.Run_Prog_flash(this.boot_info.offset, this.boot_info.hw, this.boot_bin_path);

        }

        private void SetEnableForm(bool enable)
        {
            this.btnProg.IsEnabled = enable;
            this.btnOpen.IsEnabled = enable;
            this.prog_bar.Visibility = enable ? Visibility.Hidden : Visibility.Visible;
        }

        async private void Run_Prog_flash(string offset, string hw, string bin)
        {
            this.SetEnableForm(false);
            this.tb_prog.Foreground = Brushes.Black;
            this.tb_prog.Text = "Running...";
            this.prog_bar.Value = 0;

            string tmp_path = this.GetTemporaryDirectory();
            string vitis_path = System.IO.Path.GetFullPath(System.IO.Path.Combine("HWSRVR", "2022.2"));
            string board_dir = System.IO.Path.GetFullPath(System.IO.Path.Combine("boards", hw));

            ProgramFlash prog_flash = new ProgramFlash(tmp_path, vitis_path, board_dir, offset);

            prog_flash.OnReportProgress += delegate (string prog_info, int prog_val)
            {
                this.tb_prog.Text = prog_info;
                if (prog_val >= 0)
                {
                    this.prog_bar.Value = prog_val;
                }
            };

            
            var ret = await prog_flash.Run(bin);


            switch(ret)
            {
                case ProgramFlash.RunErrCode.XSDB_NOTFOUND:
                    this.tb_prog.Text = "xsdb not found";
                    this.tb_prog.Foreground = Brushes.Red;
                    MessageBox.Show("xsdb not found", "Flash Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case ProgramFlash.RunErrCode.BIN_NOTFOUND:
                    this.tb_prog.Text = "BOOT.bin not found";
                    this.tb_prog.Foreground = Brushes.Red;
                    MessageBox.Show("BOOT.bin not found", "Flash Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case ProgramFlash.RunErrCode.TCL_TMPL_NOTFOUND:
                    this.tb_prog.Text = "Template TCL not found";
                    this.tb_prog.Foreground = Brushes.Red;
                    MessageBox.Show("Template TCL not found", "Flash Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case ProgramFlash.RunErrCode.RUN_TCL_ERR:
                    this.tb_prog.Text = "Program Error";
                    this.tb_prog.Foreground = Brushes.Red;
                    MessageBox.Show("Error program flash", "Flash Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break ;
                default:
                    this.tb_prog.Text = "Done";
                    this.tb_prog.Foreground = Brushes.DarkGreen;
                    break;
            }

            this.SetEnableForm(true);
        }

    }
}
