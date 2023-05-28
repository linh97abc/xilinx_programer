using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.IO;
using System.Windows.Shapes;

namespace Xilinx_programer
{
    public class ProgramFlash
    {
        private string temp_path;
        private string xsdb_path;
        private string board_dir;
        private string offset;

        public Action<string, int> OnReportProgress;

        public enum RunErrCode
        {
            SUCCESS,
            BIN_NOTFOUND,
            TCL_TMPL_NOTFOUND,
            XSDB_NOTFOUND,
            RUN_TCL_ERR
        };

        public ProgramFlash(string temp_path, string vitis_path, string board_dir, string offset)
        {
            this.temp_path = temp_path;
            this.xsdb_path = System.IO.Path.Combine(vitis_path, "bin", "xsdb.bat");
            this.board_dir = board_dir;
            this.offset = offset;
            this.OnReportProgress = delegate (string p_info, int p_val) {  };
        }

        private void ReportProgress(string prog_info, int prog_val)
        {
            this.OnReportProgress(prog_info, prog_val);
        }

        public async Task<RunErrCode> Run(string boot_bin_path)
        {
            if (!System.IO.File.Exists(this.xsdb_path))
            {
                return RunErrCode.XSDB_NOTFOUND;
            }

            if (!System.IO.File.Exists(boot_bin_path))
            {
                return RunErrCode.BIN_NOTFOUND;
            }

            // create tcl
            string tcl;

            string tcl_tmpl_path = System.IO.Path.Combine(this.board_dir, "board.tcl");

            if (!System.IO.File.Exists(tcl_tmpl_path))
            {
                return RunErrCode.TCL_TMPL_NOTFOUND;
            }

            using (FileStream fs = File.OpenRead(tcl_tmpl_path))
            {
                using (var streamReader = new StreamReader(fs, Encoding.UTF8))
                {
                    tcl = streamReader.ReadToEnd();
                }
            }

            var bin_size = new System.IO.FileInfo(boot_bin_path).Length;

            tcl = tcl.Replace("%(boot_bin)s", boot_bin_path.Replace("\\", "/"));
            tcl = tcl.Replace("%(board_dir)s", this.board_dir.Replace("\\", "/"));
            tcl = tcl.Replace("%(offset)s", this.offset);
            tcl = tcl.Replace("%(bin_size)s", String.Format("{0}", bin_size));
            tcl = tcl.Replace("%(mwr_size)s", String.Format("{0}", bin_size/4));

            string tcl_path = System.IO.Path.Combine(this.temp_path, "board.tcl");
            using (FileStream fs = File.OpenWrite(tcl_path))
            {
                using (var streamWriter = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    streamWriter.Write(tcl);
                }
            }


            int ret = await this.RunTcl(tcl_path);

            return (ret == 0)? RunErrCode.SUCCESS: RunErrCode.RUN_TCL_ERR;
        }

        private async Task<int> RunTcl(string run_tcl_path)
        {
            var flash_process = new System.Diagnostics.Process();

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo()
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = this.xsdb_path,
                Arguments = run_tcl_path,
            };

            flash_process.StartInfo = startInfo;
            flash_process.Start();


            while (true)
            {
                var line = await flash_process.StandardOutput.ReadLineAsync();
                if (line == null) break;
                if (line == "") continue;

                if (line.StartsWith(">>>"))
                {
                    if (line.Contains("<<<"))
                    {
                        var prog_info = line.Split("<<<");
                        string prog_text = prog_info[0].Substring(3);

                        int prog_val;
                        bool is_prog_valid = int.TryParse(prog_info[1], out prog_val);
                        prog_val = is_prog_valid ? prog_val : -1;

                        this.ReportProgress(prog_text, prog_val);
                    }
                    else
                    {
                        string prog = line.Substring(3);
                        this.ReportProgress(prog, 1);
                    }

                }
            }

            await flash_process.WaitForExitAsync();

            if (flash_process.ExitCode != 0)
            {
                return flash_process.ExitCode;

            }

            string stderr = flash_process.StandardError.ReadToEnd();

            if (stderr.Length > 0)
            {
                return -1;
            }

            return 0;
            
        }


    }
}
