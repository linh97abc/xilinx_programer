import argparse
import time
import sys

def add_parse_arguments():
    parser = argparse.ArgumentParser(
        description="Program flash",
        formatter_class=argparse.RawDescriptionHelpFormatter, allow_abbrev=False)
    parser.add_argument("--offset", default="0x0",
                        help="Flash Offset")
    parser.add_argument("--hw", required=True,
                        help="Verbose Output")
    parser.add_argument("--bin", required=True,
                        help="file BOOT.bin")
    return parser

prog = (
    ">>> Connect JTAG",
    ">>> Reset",
    ">>> Start Program",
    ">>> Running...",
    ">>> Writing...<<<10",
    ">>> Writing...<<<20",
    ">>> Writing...<<<30",
    ">>> Writing...<<<40",
    ">>> Writing...<<<50",
    ">>> Writing...<<<60",
    ">>> Writing...<<<70",
    ">>> Writing...<<<80",
    ">>> Writing...<<<90",
    ">>> Writing...<<<100",
)

def main():
    parser = add_parse_arguments()
    args = parser.parse_args()
    
    # sys.exit(1)
    print(args)

    for line in prog:
        print(line)
        print("xsdb% klskdjj")
        print("klskdjj")
        sys.stdout.flush()
        time.sleep(0.5)

if __name__ == "__main__":
    main()