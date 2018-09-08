using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaseDrop
{
    internal enum snd_asset_channel : int
	{
		SND_ASSET_CHANNEL_L = 0x1,
		SND_ASSET_CHANNEL_R = 0x2,
		SND_ASSET_CHANNEL_C = 0x4,
		SND_ASSET_CHANNEL_LFE = 0x8,
		SND_ASSET_CHANNEL_LS = 0x10,
		SND_ASSET_CHANNEL_RS = 0x20,
		SND_ASSET_CHANNEL_LB = 0x40,
		SND_ASSET_CHANNEL_RB = 0x80,
	};

	internal enum snd_asset_flags : int
	{
		SND_ASSET_FLAG_DEFAULT = 0x0,
		SND_ASSET_FLAG_LOOPING = 0x1,
		SND_ASSET_FLAG_PAD_LOOP_BUFFER = 0x2,
	};

	internal enum snd_asset_format : int
	{
		SND_ASSET_FORMAT_PCMS16 = 0x0,
		SND_ASSET_FORMAT_PCMS24 = 0x1,
		SND_ASSET_FORMAT_PCMS32 = 0x2,
		SND_ASSET_FORMAT_IEEE = 0x3,
		SND_ASSET_FORMAT_XMA4 = 0x4,
		SND_ASSET_FORMAT_MP3 = 0x5,
		SND_ASSET_FORMAT_MSADPCM = 0x6,
		SND_ASSET_FORMAT_WMA = 0x7,
	};

    internal struct snd_asset
	{
        public uint version { get; set; }
        public uint frame_count { get; set; }
        public uint frame_rate { get; set; }
        public uint channel_count { get; set; }
        public uint header_size { get; set; }
        public uint block_size { get; set; }
        public uint buffer_size { get; set; }
        public snd_asset_format format { get; set; }
        public snd_asset_channel channel_flags { get; set; }
        public snd_asset_flags flags { get; set; }
        public uint seek_table_count { get; set; }
        public uint seek_ptr { get; set; }
        public uint data_size { get; set; }
        public uint data_ptr { get; set; }
	};

    public partial class Converter : Form
    {
        // Flags
        private bool ADPCMConv = false;
        private bool XWMAConv = false;
        private bool isLoop = false;
        private string[] FilesConv = null;
        private bool CanClose = false;
        // Dirs
        private string ExportingDirectory = string.Empty;
        private string WorkingDirectory = string.Empty;

        public Converter(Form OwnedParent, string[] Files, string ExportDir, string WorkingDir, bool ADPCM, bool XWMA, bool isLooping)
        {
            // Set it
            this.Owner = OwnedParent;
            // Build
            InitializeComponent();
            // Set
            ADPCMConv = ADPCM;
            XWMAConv = XWMA;
            isLoop = isLooping;
            FilesConv = Files;
            ExportingDirectory = ExportDir;
            WorkingDirectory = WorkingDir;
        }

        private void ConvertHandler(string FileConv, string SubPath = "")
        {
            // Convert
            try
            {
                // Check what kind of file we have, if it's a normal WAV go to BO1, else, convert to normal WAV (FF has it's own handler)
                if (File.Exists(FileConv))
                {
                    // Check for fastfile
                    if (FileConv.ToLower().EndsWith(".ff"))
                    {
                        // Send to FF handler
                        FastFileHandler(FileConv);
                    }
                    else
                    {
                        // Check the wav type
                        bool isBOFile = false;
                        // Check
                        using (BinaryReader readFile = new BinaryReader(File.OpenRead(FileConv)))
                        {
                            // Check for BO1 file
                            int Version = readFile.ReadInt32();
                            // Skip 8
                            readFile.BaseStream.Position += 8;
                            // Read channels
                            int Channels = readFile.ReadInt32();
                            // Check
                            if (Version == 1 && (Channels > 0 && Channels < 10))
                            {
                                // This is a BO1 sample, convert back to normal WAV
                                isBOFile = true;
                            }
                        }
                        // Go
                        if (isBOFile)
                        {
                            // Convert
                            ConvertToNormal(FileConv, SubPath);
                        }
                        else
                        {
                            // Convert to BO (Only if it's a wav)
                            if (FileConv.ToLower().EndsWith(".wav"))
                            {
                                ConvertToBO(FileConv, SubPath);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Nothing
            }
        }

        private void ConverterRun()
        {
            // Convert each one
            for (int f = 0; f < FilesConv.Length; f++)
            {
                // Set progress
                this.Invoke((Action)delegate
                {
                    this.ProgressBar.Progress = Convert.ToInt32(((float)(f + 1) / (float)FilesConv.Length) * 100.0);
                });
                // Ship
                ConvertHandler(FilesConv[f]);
            }
            // Set
            CanClose = true;
            // Close
            this.Invoke((Action)delegate
            {
                this.Close();
            });
        }

        private void FastFileHandler(string FileConv)
        {
            // Prepare to extract FastFile audio
            byte[] CompressedBuffer = null;
            // Read it
            using (BinaryReader readFile = new BinaryReader(File.OpenRead(FileConv)))
            {
                // Skip 12
                readFile.BaseStream.Position += 12;
                // Read all
                CompressedBuffer = readFile.ReadBytes((int)readFile.BaseStream.Length - 12);
            }
            // Try and decompress the buffer
            try
            {
                // Decompress directly to memory buffer
                var DecompressedBuffer = Ionic.Zlib.ZlibStream.UncompressBuffer(CompressedBuffer);
                // We made it
                CompressedBuffer = null;
                // Scan the buffer for any and all data
                List<int> SNDAssetOffsets = new List<int>();
                // Current offset
                long CurrentOffset = 0;
                // Wave magic
                var WaveMagic = System.Text.Encoding.ASCII.GetBytes(".wav");
                // Find all offsets
                while (true)
                {
                    // Find
                    long Offset = Utilities.LightningSearch(ref DecompressedBuffer, ref WaveMagic, CurrentOffset);
                    // If not found return
                    if (Offset < 0)
                    {
                        // Done
                        break;
                    }
                    else
                    {
                        // Got one
                        SNDAssetOffsets.Add(Convert.ToInt32(Offset));
                    }
                    // Set
                    CurrentOffset = Offset + WaveMagic.Length;
                }
                // List of files to convert
                HashSet<string> FilesToConvert = new HashSet<string>();
                // Loop in parallel to extract assets, once extracted, convert to normal
                Parallel.ForEach<int>(SNDAssetOffsets, (Offset) =>
                {
                    // Rebuild asset if possible (Find string start) 0xFFFFFFFF
                    int Position = -1;
                    // Find
                    for (int CurrentPos = Offset; CurrentPos > 0; CurrentPos--)
                    {
                        // Find the first 0xFF
                        if (DecompressedBuffer[CurrentPos] == 0xFF)
                        {
                            // Position = CurrentPos + 1
                            Position = CurrentPos + 1;
                            // Done
                            break;
                        }
                    }
                    // If we got it, continue
                    if (Position > 0 && ((Offset + 4) - Position) <= 2048)
                    {
                        try
                        {
                            // Process header
                            byte[] NameBuffer = new byte[(Offset + 4) - Position];
                            // Copy
                            Array.Copy(DecompressedBuffer, Position, NameBuffer, 0, (Offset + 4) - Position);
                            // Make it
                            string SoundName = Encoding.ASCII.GetString(NameBuffer).Trim();
                            // Clean it
                            if (SoundName.StartsWith("sound\\"))
                            {
                                // Remove
                                SoundName = SoundName.Substring(6);
                            }
                            // Clean
                            SoundName = SoundName.Replace("/", "\\");
                            // Copy header
                            byte[] HeaderBuffer = new byte[0x38];
                            // Copy
                            Array.Copy(DecompressedBuffer, Position - 0x38, HeaderBuffer, 0, 0x38);
                            // Cast
                            snd_asset Header = Utilities.ByteArrayToStruct<snd_asset>(ref HeaderBuffer);
                            // Make sure we have data
                            if (Header.data_size > 0 && Header.version == 1)
                            {
                                // Process
                                string TempFileName = Path.Combine(ExportingDirectory, "tmp_" + Path.GetFileNameWithoutExtension(FileConv), SoundName);
                                // Make dir if need be
                                lock (FilesToConvert)
                                {
                                    // Sync
                                    if (!Directory.Exists(Path.GetDirectoryName(TempFileName)))
                                    {
                                        // Make
                                        Directory.CreateDirectory(Path.GetDirectoryName(TempFileName));
                                    }
                                }
                                // Open it
                                using (BinaryWriter writeFile = new BinaryWriter(File.Create(TempFileName)))
                                {
                                    // Write header
                                    writeFile.Write(HeaderBuffer);
                                    // Check for seek table
                                    if (Header.seek_table_count != 0)
                                    {
                                        // Write seek table
                                        byte[] SeekTable = new byte[Header.seek_table_count * 4];
                                        // Read it
                                        Array.Copy(DecompressedBuffer, Offset + 5, SeekTable, 0, Header.seek_table_count * 4);
                                        // Write to file
                                        writeFile.Write(SeekTable);
                                    }
                                    // Check
                                    if (2040 - (4 * Header.seek_table_count) > 0)
                                    {
                                        // Pad it
                                        var PadData = new byte[2040 - (4 * Header.seek_table_count)];
                                        // Write
                                        writeFile.Write(PadData);
                                    }
                                    // Write data
                                    byte[] SoundData = new byte[Header.data_size];
                                    // Read it
                                    Array.Copy(DecompressedBuffer, (Offset + 5 + (Header.seek_table_count * 4)), SoundData, 0, Header.data_size);
                                    // Write to file
                                    writeFile.Write(SoundData);
                                }
                                // Add
                                lock (FilesToConvert)
                                {
                                    FilesToConvert.Add(TempFileName);
                                }
                            }
                        }
                        catch
                        {
                            // Nothing, move on
                        }
                    }
                });
                // Clean
                DecompressedBuffer = null;
                // Convert assets
                for (int c = 0; c < FilesToConvert.Count; c++)
                {
                    // Set progress
                    this.Invoke((Action)delegate
                    {
                        this.ProgressBar.Progress = Convert.ToInt32(((float)(c + 1) / (float)FilesToConvert.Count) * 100.0);
                    });
                    // Generate a subpath for the normal files folder
                    string ToConvert = FilesToConvert.ElementAt(c);
                    // Generate
                    string SubPath = Path.Combine(Path.GetFileNameWithoutExtension(FileConv), ToConvert.Substring(ToConvert.IndexOf("tmp_" + Path.GetFileNameWithoutExtension(FileConv)) + ("tmp_" + Path.GetFileNameWithoutExtension(FileConv)).Length + 1));
                    // Ship
                    ConvertHandler(FilesToConvert.ElementAt(c), SubPath);
                }
                // Delete the temporary folder!
                Directory.Delete(Path.Combine(ExportingDirectory, "tmp_" + Path.GetFileNameWithoutExtension(FileConv)), true);
            }
            catch
            {
                // Failed to decompress, just stop
                CompressedBuffer = null;
            }
            // Collect
            GC.Collect();
        }

        private void ConvertToBO(string FileConv, string SubPath = "")
        {
            // Run the file through the converter for the format we want
            File.Copy(FileConv, Path.Combine(WorkingDirectory, "convToBO.wav"), true);
            // Convert to format
            if (XWMAConv)
            {
                // Convert with a sample of 96k
                RunConvertProgramWithArgs(Path.Combine(WorkingDirectory, "XWMAEnc.exe"), "-b 96000 convToBO.wav boResult.wav", WorkingDirectory);
            }
            if (ADPCMConv)
            {
                // Convert with a block size of 512
                RunConvertProgramWithArgs(Path.Combine(WorkingDirectory, "ADPCMEnc.exe"), "-b 512 convToBO.wav boResult.wav", WorkingDirectory);
            }
            // Check if we have the file
            if (File.Exists(Path.Combine(WorkingDirectory, "boResult.wav")))
            {
                // We must read the file, then add the header
                ushort FormatSpecifier = 0;
                ushort ChannelCount = 0;
                uint FrameRate = 0;
                uint AverageBPS = 0;
                ushort BlockAlign = 0;
                ushort BitsPerSample = 0;
                ushort ExtraDataSize = 0;
                // Read properties and data
                using (BinaryReader readFile = new BinaryReader(File.OpenRead(Path.Combine(WorkingDirectory, "boResult.wav"))))
                {
                    // Skip generic header
                    readFile.BaseStream.Position = 0x10;
                    // Read size of header
                    int SizeOfHeader = readFile.ReadInt32();
                    // Read data flags
                    FormatSpecifier = readFile.ReadUInt16();
                    ChannelCount = readFile.ReadUInt16();
                    FrameRate = readFile.ReadUInt32();
                    AverageBPS = readFile.ReadUInt32();
                    BlockAlign = readFile.ReadUInt16();
                    BitsPerSample = readFile.ReadUInt16();
                    ExtraDataSize = readFile.ReadUInt16();
                    // Skip extra data
                    readFile.BaseStream.Position += ExtraDataSize;
                    // Data
                    byte[] SoundData = null;
                    byte[] SeekData = null;
                    // Loop
                    bool NeedsData = true;
                    // Check what the next block is, it's always a 4 byte block
                    while (NeedsData && (readFile.BaseStream.Position != readFile.BaseStream.Length))
                    {
                        // Read block and size
                        uint BlockID = readFile.ReadUInt32();
                        uint BlockSize = readFile.ReadUInt32();
                        // Check
                        switch (BlockID)
                        {

                            case 0x61746164: // Data
                                // Read data
                                SoundData = readFile.ReadBytes((int)BlockSize);
                                // Exit the loop
                                NeedsData = false;
                                break;
                            case 0x73647064: // Seek table
                                // Read data
                                SeekData = readFile.ReadBytes((int)BlockSize);
                                // Continue
                                break;
                            default:
                                // Skip over size
                                readFile.BaseStream.Position += BlockSize;
                                break;
                        }
                    }
                    // Now prepare the file for a BO1 header
                    string FileName = Path.Combine(ExportingDirectory, "bo_ready", Path.GetFileNameWithoutExtension(FileConv) + ".wav");
                    // Open and write
                    using (BinaryWriter writeWav = new BinaryWriter(File.Create(FileName)))
                    {
                        // Write a BO1 header
                        writeWav.Write((uint)0x1);
                        // Frame count
                        writeWav.Write((uint)0x0);
                        // Frame rate
                        writeWav.Write((uint)FrameRate);
                        // Channels
                        writeWav.Write((uint)ChannelCount);
                        // Header size
                        writeWav.Write((uint)0x830);
                        // Block size
                        writeWav.Write((uint)0x10600);
                        // Buffer size
                        writeWav.Write((uint)0x83000);
                        // Format
                        if (ADPCMConv)
                        {
                            writeWav.Write((uint)snd_asset_format.SND_ASSET_FORMAT_MSADPCM);
                        }
                        else if (XWMAConv)
                        {
                            writeWav.Write((uint)snd_asset_format.SND_ASSET_FORMAT_WMA);
                        }
                        // Channel flags
                        if (ChannelCount == 1)
                        {
                            // Just left
                            writeWav.Write((uint)snd_asset_channel.SND_ASSET_CHANNEL_L);
                        }
                        else
                        {
                            // Left right
                            writeWav.Write((uint)snd_asset_channel.SND_ASSET_CHANNEL_L | (uint)snd_asset_channel.SND_ASSET_CHANNEL_R);
                        }
                        // Flags
                        if (isLoop)
                        {
                            // Loop
                            writeWav.Write((uint)snd_asset_flags.SND_ASSET_FLAG_LOOPING);
                        }
                        else
                        {
                            // No loop
                            writeWav.Write((uint)0x0);
                        }
                        // Write seek table
                        if (SeekData != null)
                        {
                            writeWav.Write((uint)(SeekData.Length / 4));
                            writeWav.Write((uint)0x0);
                        }
                        else
                        {
                            writeWav.Write((uint)0x0);
                            writeWav.Write((uint)0x0);
                        }
                        // Data size
                        writeWav.Write((uint)SoundData.Length);
                        // No ptr
                        writeWav.Write((uint)0x0);
                        // Write seek table
                        if (SeekData != null)
                        {
                            // Write it
                            writeWav.Write(SeekData);
                            // Pad to remainder
                            var bytePad = new byte[Math.Max(0, 0x830 - writeWav.BaseStream.Position)];
                            // Write
                            writeWav.Write(bytePad);
                        }
                        else
                        {
                            // Pad
                            var bytePad = new byte[0x7F8];
                            // Write
                            writeWav.Write(bytePad);
                        }
                        // Write data
                        writeWav.Write(SoundData);
                    }
                }
                // Clean up
                try
                {
                    File.Delete(Path.Combine(WorkingDirectory, "convToBO.wav"));
                    File.Delete(Path.Combine(WorkingDirectory, "boResult.wav"));
                }
                catch
                {
                    // Nothing
                }
            }
            else
            {
                // Just clean up, failed
                try
                {
                    File.Delete(Path.Combine(WorkingDirectory, "convToBO.wav"));
                }
                catch
                {
                    // Nothing
                }
            }
        }

        private void ConvertToNormal(string FileConv, string SubPath = "")
        {
            // We must read the file, and convert back to a normal WAV
            using (BinaryReader readFile = new BinaryReader(File.OpenRead(FileConv)))
            {
                // Read header
                var Header = readFile.ReadStruct<snd_asset>();
                // Jump to data
                readFile.BaseStream.Position = Header.header_size;
                // Write a new file, ready to be converted
                using (BinaryWriter writeFile = new BinaryWriter(File.Create(Path.Combine(WorkingDirectory, "convToNormal.wav"))))
                {
                    // Read raw data (EOF or data size, take smallest)
                    int DataRead = Math.Min((int)Header.data_size,(int)(readFile.BaseStream.Length - readFile.BaseStream.Position));
                    // Read the actual data
                    var SoundData = readFile.ReadBytes(DataRead);
                    // Write header for the format
                    switch (Header.format)
                    {
                        case snd_asset_format.SND_ASSET_FORMAT_WMA:
                            // Size
                            int FileSize = SoundData.Length + 0x32;
                            // Write header
                            writeFile.Write(new char[] { 'R', 'I', 'F', 'F' });
                            // Write size
                            writeFile.Write((int)FileSize);
                            // Write format
                            writeFile.Write(new char[] { 'X', 'W', 'M', 'A', 'f', 'm', 't', ' ' });
                            // Write size
                            writeFile.Write((int)0x12);
                            // Write format
                            writeFile.Write((ushort)0x161);
                            // Write channels
                            writeFile.Write((ushort)Header.channel_count);
                            // Write framerate
                            writeFile.Write((uint)Header.frame_rate);
                            // Write average bps
                            writeFile.Write((uint)12000);
                            // Write block align
                            writeFile.Write((ushort)(SoundData.Length / Header.seek_table_count));
                            // Write bits per sample
                            writeFile.Write((ushort)0x10);
                            // No extra
                            writeFile.Write((ushort)0x0);
                            // Write blank seek
                            writeFile.Write(new char[] { 'd', 'p', 'd', 's' });
                            // Write length
                            writeFile.Write((uint)0x4);
                            // Write blank
                            writeFile.Write((uint)0x0);
                            // Write data block
                            writeFile.Write(new char[] { 'd', 'a', 't', 'a' });
                            // Write length
                            writeFile.Write((uint)SoundData.Length);
                            break;
                        case snd_asset_format.SND_ASSET_FORMAT_MSADPCM:
                            // Size
                            int FileSizeAD = SoundData.Length + 0x46;
                            // Write header
                            writeFile.Write(new char[] { 'R', 'I', 'F', 'F' });
                            // Write size
                            writeFile.Write((int)FileSizeAD);


                            // Write format
                            writeFile.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                            // Write size
                            writeFile.Write((int)0x32);
                            // Write format
                            writeFile.Write((ushort)0x2);
                            // Write channels
                            writeFile.Write((ushort)Header.channel_count);
                            // Write framerate
                            writeFile.Write((uint)Header.frame_rate);
                            // Write average bps
                            writeFile.Write((uint)45131);
                            // Write block align
                            writeFile.Write((ushort)(Header.channel_count * 262));
                            // Write bits per sample
                            writeFile.Write((ushort)0x4);
                            // ADPCM extra data
                            writeFile.Write((ushort)0x20);
                            // Write block size (512 for cod)
                            writeFile.Write((ushort)512);
                            // Write coeff count
                            writeFile.Write((ushort)7);
                            // Coeffs
                            var Coeffs = new short[] { 256, 0, 512, -256, 0, 0, 192, 64, 240, 0, 460, -208, 392, -232 };
                            // Write them
                            for (int i = 0; i < 14; i++) { writeFile.Write((short)Coeffs[i]); }
                            // Write data block
                            writeFile.Write(new char[] { 'd', 'a', 't', 'a' });
                            // Write length
                            writeFile.Write((uint)SoundData.Length);
                            break;
                    }
                    // Write it
                    writeFile.Write(SoundData);
                }
                // Now, convert that format to the normal WAV
                switch (Header.format)
                {
                    case snd_asset_format.SND_ASSET_FORMAT_WMA:
                        // Convert
                        RunConvertProgramWithArgs(Path.Combine(WorkingDirectory, "XWMAEnc.exe"), "convToNormal.wav normalResult.wav", WorkingDirectory);
                        break;
                    case snd_asset_format.SND_ASSET_FORMAT_MSADPCM:
                        // Convert
                        RunConvertProgramWithArgs(Path.Combine(WorkingDirectory, "ADPCMEnc.exe"), "convToNormal.wav normalResult.wav", WorkingDirectory);
                        break;
                }
                // Check if exists, copy if so, delete trash
                if (File.Exists(Path.Combine(WorkingDirectory, "normalResult.wav")))
                {
                    // Check for subpath
                    if (SubPath == "" || string.IsNullOrWhiteSpace(SubPath))
                    {
                        // Copy and move (normal)
                        File.Copy(Path.Combine(WorkingDirectory, "normalResult.wav"), Path.Combine(ExportingDirectory, "normal", Path.GetFileNameWithoutExtension(FileConv) + ".wav"), true);
                    }
                    else
                    {
                        // Copy and move with subpath
                        string MoveName = Path.Combine(ExportingDirectory, "normal", SubPath);
                        // Check if exists
                        if (!Directory.Exists(Path.GetDirectoryName(MoveName)))
                        {
                            // Make
                            Directory.CreateDirectory(Path.GetDirectoryName(MoveName));
                        }
                        // Copy
                        File.Copy(Path.Combine(WorkingDirectory, "normalResult.wav"), MoveName, true);
                    }
                    // Clean up
                    try
                    {
                        File.Delete(Path.Combine(WorkingDirectory, "convToNormal.wav"));
                        File.Delete(Path.Combine(WorkingDirectory, "normalResult.wav"));
                    }
                    catch
                    {
                        // Nothing
                    }
                }
                else
                {
                    // Just clean up
                    try
                    {
                        File.Delete(Path.Combine(WorkingDirectory, "convToNormal.wav"));
                    }
                    catch
                    {
                        // Nothing
                    }
                }
            }
        }

        private static void RunConvertProgramWithArgs(string ProgramPath, string Arguments, string WorkDir)
        {
            // We can run the converter
            using (Process pexporter = new Process())
            {
                // Set
                pexporter.StartInfo.FileName = ProgramPath;
                // Arguments
                pexporter.StartInfo.Arguments = Arguments;
                // Working dir
                pexporter.StartInfo.WorkingDirectory = WorkDir;
                // No window
                pexporter.StartInfo.CreateNoWindow = true;
                // No gui
                pexporter.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                // UI
                pexporter.StartInfo.UseShellExecute = false;
                // Start it
                pexporter.Start();
                // Wait
                pexporter.WaitForExit();
            }
        }

        private void Converter_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop
            if (e.CloseReason == CloseReason.UserClosing && !CanClose)
            {
                e.Cancel = true;
            }
        }

        private void Converter_Load(object sender, EventArgs e)
        {
            // Convert
            Task.Run((Action)ConverterRun);
        }
    }
}
