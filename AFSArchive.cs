using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AFS
{
    class ToCEntry
    {
        public uint offset;
        public uint size;
    }

    class Header
    {
        public string magic;
        public uint files;
    }

    class DirectoryEntry
    {
        public string filename;
        public UInt16 year;
        public UInt16 month;
        public UInt16 day;
        public UInt16 hour;
        public UInt16 minute;
        public UInt16 second;
        public uint size;
    }

    class AFSFile
    {
        public ToCEntry toc = new ToCEntry();
        public DirectoryEntry entry = new DirectoryEntry();
        public byte[] data;
    }

    class AFSArchive
    {
        public Header header = new Header();
        public AFSFile[] Files;

        public AFSArchive(string file, int first_data = 0x80000, int block_size = 2048, int dir_address = 0x7FFF8)
        {
            byte[] bytes = File.ReadAllBytes(file);
            byte[] magic = new byte[4];
            Array.Copy(bytes, 0, magic, 0, 4);
            header.magic = Encoding.ASCII.GetString(magic);

            if (header.magic != "AFS\0")
                throw new InvalidDataException("The file is not a valid AFS file.");


            header.files = BitConverter.ToUInt32(bytes, 4);
            uint directory_address = BitConverter.ToUInt32(bytes, dir_address);

            Files = new AFSFile[header.files];

            for (int i = 0; i < header.files; i++)
            {
                Files[i] = new AFSFile();

                Files[i].toc.offset = BitConverter.ToUInt32(bytes, 8 + (i * 8));
                Files[i].toc.size = BitConverter.ToUInt32(bytes, 8 + (i * 8) + 4);

                Files[i].data = new byte[Files[i].toc.size];
                Array.Copy(bytes, Files[i].toc.offset, Files[i].data, 0, Files[i].toc.size);
                
                for (int cbyte = 0; cbyte < 32; cbyte++)
                {
                    byte current_byte = bytes[(int)directory_address + (i * 0x30) + cbyte];

                    if (current_byte != 00)
                        Files[i].entry.filename += Encoding.ASCII.GetString(new[] { current_byte });
                }

                Files[i].entry.year = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 32);
                Files[i].entry.month = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 34);
                Files[i].entry.day = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 36);
                Files[i].entry.hour = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 38);
                Files[i].entry.minute = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 40);
                Files[i].entry.second = BitConverter.ToUInt16(bytes, (int)directory_address + (i * 0x30) + 42);

                Files[i].entry.size = BitConverter.ToUInt32(bytes, (int)directory_address + (i * 0x30) + 44);
            }

        }

        public bool FileExists(string name, bool ignorecase = true)
        {
            foreach (AFSFile afile in Files)
            {
                string filename = afile.entry.filename;
                string filetofind = name;

                if (ignorecase == true)
                {
                    filename = filename.ToLower();
                    filetofind = filetofind.ToLower();
                }

                if (filename == filetofind)
                    return true;
            }

            return false;
        }

        public int AddFile(string file)
        {
            string filename = Path.GetFileName(file);

            if (filename.Length > 32)
                throw new InvalidDataException("The filename is too long for AFS.");

            if (FileExists(filename))
                throw new Exception("The file exists.");

            FileInfo finfo = new FileInfo(file);
            byte[] bytes = File.ReadAllBytes(file);

            AFSFile afs = new AFSFile();
            afs.toc.size = (uint)bytes.Length;
            afs.data = bytes;

            afs.entry.filename = filename;
            afs.entry.year = (UInt16)finfo.LastWriteTime.Year;
            afs.entry.month = (UInt16)finfo.LastWriteTime.Month;
            afs.entry.day = (UInt16)finfo.LastWriteTime.Day;
            afs.entry.hour = (UInt16)finfo.LastWriteTime.Hour;
            afs.entry.minute = (UInt16)finfo.LastWriteTime.Minute;
            afs.entry.second = (UInt16)finfo.LastWriteTime.Second;
            afs.entry.size = (uint)bytes.Length;

            Array.Resize(ref Files, Files.Length + 1);
            Files[Files.Length - 1] = afs;
            header.files++;

            return 0;
        }

        public int ReplaceFile(string name, AFSFile file, bool ignorecase = true)
        {
            int file_index = 0;
            bool file_found = false;
            int i = 0;

            foreach (AFSFile afile in Files)
            {
                string filename = afile.entry.filename;
                string filetofind = name;

                if (ignorecase == true)
                {
                    filename = filename.ToLower();
                    filetofind = filetofind.ToLower();
                }

                if (filename == filetofind)
                {
                    file_index = i;
                    file_found = true;
                }

                i++;
            }

            if (file_found == false)
                throw new FileNotFoundException("The file wasn't found.");

            Files[file_index] = file;
            return 0;
        }

        public int AddFile(AFSFile file)
        {

            if (FileExists(file.entry.filename))
                throw new Exception("The file exists.");

            Array.Resize(ref Files, Files.Length + 1);
            Files[Files.Length - 1] = file;
            header.files++;

            return 0;
        }

        public int RemoveFile(string name, bool ignorecase = true)
        {
            int file_index = 0;
            bool file_found = false;

            int i = 0;
            foreach (AFSFile afile in Files)
            {
                string filename = afile.entry.filename;
                string filetofind = name;

                if (ignorecase == true)
                {
                    filename = filename.ToLower();
                    filetofind = filetofind.ToLower();
                }

                if (filename == filetofind)
                {
                    file_index = i;
                    file_found = true;
                }

                i++;
            }

            if (file_found == false)
                throw new FileNotFoundException("The file wasn't found.");

            List<AFSFile> LFiles = new List<AFSFile>(Files);
            LFiles.RemoveAt(file_index);
            Array.Resize(ref Files, Files.Length - 1);
            Files = LFiles.ToArray();
            header.files -= 1;

            return 0;
        }

        public void Save(string output, int first_data = 0x80000, int block_size = 2048, int dir_address = 0x7FFF8)
        {
            using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(output)))
            {
                int total_data_blocks = 0;

                /* Calculate the total data blocks */
                foreach (AFSFile file in Files)
                {
                    int temp_data_size = file.data.Length;
                    while (temp_data_size > 0)
                    {
                        temp_data_size -= block_size;
                        total_data_blocks++;
                    }
                }

                int directory_size_temp = 0x30 * (int)header.files;
                int directory_block = 0;

                /* Calculate the Directory Size */
                while (directory_size_temp > 0)
                {
                    directory_size_temp -= block_size;
                    directory_block++;
                }

                int total_size = first_data + (total_data_blocks * block_size) + (directory_block * block_size);

                bw.Write(Encoding.ASCII.GetBytes("AFS\0"));
                bw.Write(header.files);

                int last_toc_count = 0;
                int current_block = 0;
                int directory_offset = first_data + (total_data_blocks * block_size);

                /* NULL the Directory, so the size is correct */
                bw.BaseStream.Position = directory_offset;
                for (int i2 = 0; i2 < directory_block * block_size; i2++)
                    bw.Write((byte)00);

                int i = 0;
                foreach (AFSFile file in Files)
                {
                    ToCEntry toc = new ToCEntry();
                    toc.offset = (uint)(first_data + (current_block * block_size));
                    toc.size = (uint)file.data.Length;

                    /* Calculate next block size */
                    int temp_data_size = (int)toc.size;
                    while (temp_data_size > 0)
                    {
                        temp_data_size -= block_size;
                        current_block++;
                    }

                    /* Write ToC to File */
                    bw.BaseStream.Position = 8 + (8 * last_toc_count);
                    bw.Write(toc.offset);
                    bw.Write(toc.size);

                    /* Write File data to File */
                    bw.BaseStream.Position = toc.offset;
                    bw.Write(file.data);

                    /* Write Filename Directory Entry */
                    bw.BaseStream.Position = directory_offset + (0x30 * i);
                    bw.Write(Encoding.ASCII.GetBytes(file.entry.filename));
                    bw.BaseStream.Position = directory_offset + (0x30 * i) + 32;
                    bw.Write(file.entry.year);
                    bw.Write(file.entry.month);
                    bw.Write(file.entry.day);
                    bw.Write(file.entry.hour);
                    bw.Write(file.entry.minute);
                    bw.Write(file.entry.second);
                    bw.Write(file.entry.size);

                    last_toc_count++;
                    i++;
                }

                /* Write Filename Directory Information */
                bw.BaseStream.Position = dir_address;
                bw.Write(directory_offset);
                bw.Write(i * 0x30);

                bw.Close();
                
            }
           
        }
    }
}
