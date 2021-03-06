//
// Structures (S24, S25)
//

using System;

namespace Microsoft.LiveLabs.PE
{
    //
    // Header
    //

    // S25.2.1
    public struct DOSHeader
    {
        public const uint Size = 128;

        private static readonly byte[] prefix = new byte[]
                                                {
                                                    0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00,
                                                    0x00, 0xFF, 0xFF, 0x00, 0x00, 0xb8, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                    0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                    0x00, 0x00, 0x00, 0x00, 0x00
                                                };
        public FileOffset LfaNew;
        private static readonly byte[] suffix = new byte[]
                                                {
                                                    0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd, 0x21, 0xb8, 0x01,
                                                    0x4c, 0xcd, 0x21, 0x54, 0x68, 0x69, 0x73, 0x20, 0x70, 0x72, 0x6f,
                                                    0x67, 0x72, 0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f, 0x74,
                                                    0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e, 0x20, 0x69, 0x6e, 0x20,
                                                    0x44, 0x4f, 0x53, 0x20, 0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d,
                                                    0x0a, 0x24, 0x00, 0x00, 0x00
                                                };
        // SPEC VARIATION: Should be zero but Silverlight assemblies put something there.
        public uint Final;

        private static bool EqualBytes(byte[] first, byte[] second)
        {
            if (first == null || second == null)
                throw new ArgumentNullException();
            if (first.Length != second.Length)
                return false;
            for (var i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                    return false;
            }
            return true;
        }

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            if (reader.RemainingBytes < Size)
                throw new PEException("missing DOSHeader");

            var actualPrefix = reader.ReadBytes((uint)prefix.Length);
            if (!EqualBytes(actualPrefix, prefix))
                throw new PEException("invalid DOSHeader.Prefix");

            LfaNew.Read(reader);
            if (LfaNew.Offset < Size)
                throw new PEException("invalid DOSHeader.LfaNew");

            var actualSuffix = reader.ReadBytes((uint)suffix.Length);
            if (!EqualBytes(actualSuffix, suffix))
                throw new PEException("invalid DOSHeader.Suffix");

            Final = reader.ReadUInt32();

            if (ctxt.Tracer != null)
                LfaNew.Append(ctxt, "DOSHeader.LfaNew");
        }

        public void Alloc(WriterContext ctxt)
        {
            LfaNew.Offset = Size;
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteBytes(prefix);
            LfaNew.Write(writer);
            writer.WriteBytes(suffix);
            writer.WriteUInt32(Final);
        }
    }

    // S25.2.2
    public struct PEFileHeader
    {
        public const uint Size = 24;
        private static readonly DateTime nineteenSeventy = new DateTime(1970, 1, 1).ToUniversalTime();

        // SPEC VARIATION: Spec also requires ImageFileFlags.LINE_NUMS_STRIPPED and ImageFileFlags.LOCAL_SYMS_STRIPPED to be set
        private const ImageFileFlags setFlags =
            ImageFileFlags.EXECUTABLE_IMAGE | ImageFileFlags.MACHINE_32_BIT;
        private const ImageFileFlags clearFlags =
            ImageFileFlags.RELOCS_STRIPPED | ImageFileFlags.AGGRESIVE_WS_TRIM | ImageFileFlags.LARGE_ADDRESS_AWARE |
            ImageFileFlags.BYTES_REVERSED_LO | ImageFileFlags.DEBUG_STRIPPED | ImageFileFlags.REMOVABLE_RUN_FROM_SWAP |
            ImageFileFlags.NET_RUN_FROM_SWAP | ImageFileFlags.SYSTEM | ImageFileFlags.UP_SYSTEM_ONLY |
            ImageFileFlags.BYTES_REVERSED_HI;

        private const uint prefix = 0x00004550;
        private const ushort machine = 0x014c;
        public const ushort NumberOfSections = 3;
        public DateTime DateTimeStamp;
        private const uint pointerToSymbolTable = 0;
        private const uint numberOfSymbols = 0;
        public ImageFileFlags Flags;

        public bool IsDLL { get { return (Flags & ImageFileFlags.DLL) != 0; } }

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            if (reader.RemainingBytes < Size)
                throw new PEException("missing PEFileHeader");

            var actualPrefix = reader.ReadUInt32();
            if (actualPrefix != prefix)
                throw new PEException("invalid PEFileHeader.Prefix");
            var actualMachine = reader.ReadUInt16();
            if (actualMachine != machine)
                throw new PEException("invalid PEFileHeader.Machine");
            var actualNumberOfSections = reader.ReadUInt16();
            if (actualNumberOfSections != NumberOfSections)
                throw new PEException("invalid PEFileHeader.NumberOfSections");
            var seconds = reader.ReadUInt32();
            DateTimeStamp = nineteenSeventy.AddSeconds((double)seconds);
            var actualPointerToSymbolTable = reader.ReadUInt32();
            if (actualPointerToSymbolTable != pointerToSymbolTable)
                throw new PEException("invalid PEFileHeader.PointerToSymbolTable");
            var actualNumberOfSymbols = reader.ReadUInt32();
            if (actualNumberOfSymbols != numberOfSymbols)
                throw new PEException("invalid PEFileHeader.NumberOfSymbols");
            var actualOptionalHeaderSize = reader.ReadUInt16();
            if (actualOptionalHeaderSize != PEOptionalHeader.Size)
                throw new PEException("invalid PEFileHeader.OptionalHeaderSize");
            Flags = (ImageFileFlags)reader.ReadUInt16();
            var check = Flags & ~ImageFileFlags.DLL;
            if ((check & setFlags) != setFlags)
                throw new PEException("invalid PEFileHeader.Flags");
            if ((check & clearFlags) != 0)
                throw new PEException("invalid PEFileHeader.Flags");
        }

        public void Alloc(WriterContext ctxt)
        {
            DateTimeStamp = DateTime.Now; // UTC???
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(prefix);
            writer.WriteUInt16(machine);
            writer.WriteUInt16(NumberOfSections);
            var seconds = (uint)(DateTimeStamp.ToUniversalTime() - nineteenSeventy).TotalSeconds;
            writer.WriteUInt32(seconds);
            writer.WriteUInt32(pointerToSymbolTable);
            writer.WriteUInt32(numberOfSymbols);
            writer.WriteUInt16(PEOptionalHeader.Size);
            writer.WriteUInt16((ushort)Flags);
        }
    }

        // S25.2.3.1
    public struct PEHeaderStandardFields
    {
        public const uint Size = 28;

        private const ushort magic = 0x010b;
        // SPEC VARIATION: Spec requires 6
        private const byte lMajorLow = 6;
        private const byte lMajorHigh = 8;
        public byte LMajor;
        private const byte lMinor = 0;
        public uint CodeSize; // == .text section SizeOfRawData
        public uint InitializedDataSize; // == .reloc + .rsrc section's SizeOfRawData
        private const uint uninitializedDataSize = 0;
        public RVA<byte[]> EntryPoint; // points to 0xff 0x25 <4-byte rva> in .text
        public RVA BaseOfCode; // == start of .text section
        public RVA BaseOfData; // == start of .reloc or .rsrc section, whichever is lower

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var actualMagic = reader.ReadUInt16();
            if (actualMagic != magic)
                throw new PEException("invalid PEHeaderStandardFields.Magic");
            LMajor = reader.ReadByte();
            if (LMajor < lMajorLow || LMajor > lMajorHigh)
                throw new PEException("invalid PEHeaderStandardFields.LMajor");
            var actualLMinor = reader.ReadByte();
            if (actualLMinor != lMinor)
                throw new PEException("invalid PEHeaderStandardFields.LMinor");
            CodeSize = reader.ReadUInt32();
            InitializedDataSize = reader.ReadUInt32();
            var actualUninitializedDataSize = reader.ReadUInt32();
            if (actualUninitializedDataSize != uninitializedDataSize)
                throw new PEException("invalid PEHeaderStandardFields.UninitializedDataSize");
            EntryPoint.Read(reader);
            BaseOfCode.Read(reader);
            BaseOfData.Read(reader);

            if (ctxt.Tracer != null)
            {
                ctxt.Tracer.AppendLine(String.Format("PEHeaderStandardFields.CodeSize: {0:x8}", CodeSize));
                ctxt.Tracer.AppendLine
                    (String.Format("PEHeaderStandardFields.InitializedDataSize: {0:x8}", InitializedDataSize));
            }
        }

        public void Deref(ReaderContext ctxt)
        {
            EntryPoint.Value = EntryPoint.GetReaderNonNull(ctxt).ReadBytes(6);
            if (EntryPoint.Value[0] != 0xff || EntryPoint.Value[1] != 0x25)
                throw new PEException("invalid PEHeaderStandardFields.EntryPoint");

            if (ctxt.Tracer != null)
            {
                EntryPoint.Append(ctxt, "PEHeaderStandardFields.EntryPoint");
                BaseOfCode.Append(ctxt, "PEHeaderStandardFields.BaseOfCode");
                BaseOfData.Append(ctxt, "PEHeaderStandardFields.BaseOfData");
            }
        }

        public void Alloc(WriterContext ctxt)
        {
            EntryPoint.Alloc(ctxt, Section.Text).WriteBytes(EntryPoint.Value);
        }

        public void Fixup(WriterContext ctxt)
        {
            CodeSize = ctxt.CodeSize();
            InitializedDataSize = ctxt.InitializedDataSize();
            EntryPoint.Fixup(ctxt, Section.Text);
            BaseOfCode.Address = ctxt.BaseOfCode();
            BaseOfData.Address = ctxt.BaseOfData();
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(magic);
            writer.WriteByte(LMajor);
            writer.WriteByte(lMinor);
            writer.WriteUInt32(CodeSize);
            writer.WriteUInt32(InitializedDataSize);
            writer.WriteUInt32(uninitializedDataSize);
            EntryPoint.Write(writer);
            BaseOfCode.Write(writer);
            BaseOfData.Write(writer);
        }
    }

    // S25.2.3.2
    public struct PEHeaderNTSpecificFields
    {
        public const uint Size = 68;

        // SPEC VARIATION: Image base varies under Silverlight
        // private const uint imageBase = 0x400000;
        public uint ImageBase;
        private const uint sectionAlignment = 0x2000;
        private const uint fileAlignment1 = 0x200;
        private const uint fileAlignment2 = 0x1000;
        public uint FileAlignment;
        private const ushort osMajor = 4;
        private const ushort osMinor = 0;
        private const ushort userMajor = 0;
        private const ushort userMinor = 0;
        private const ushort subSysMajor = 4;
        private const ushort subSysMinor = 0;
        private const uint reserved = 0;
        public uint ImageSize; // size of image in virtual memory, starting from ImageBase, in multiples of sectionAlignment
        public uint HeaderSize; // header size in multiple of FileAligment
        // SPEC VARIATION: File checksum not always zero
        // private const uint fileChecksum = 0;
        public uint FileChecksum;
        public SubSystem SubSystem;
        // SPEC VARIATION: Flags not always zero
        // private const ushort dllFlags = 0;
        public ushort DllFlags;
        private const uint stackReserveSize = 0x100000;
        // SPEC VARIATION: Different under Silverlight
        // private const uint stackCommitSize = 0x1000;
        public uint StackCommitSize;
        // SPEC VARIATION: Different under Silverlight
        // private const uint heapReserveSize = 0x100000;
        public uint HeapReserveSize;
        // SPEC VARIATION: Different under Silverlight
        // private const uint heapCommitSize = 0x1000;
        public uint HeapCommitSize;
        private const uint loaderFlags = 0;
        private const uint numberOfDataDirectories = 0x10;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            ImageBase = reader.ReadUInt32();
            var actualSectionAlignment = reader.ReadUInt32();
            if (actualSectionAlignment != sectionAlignment)
                throw new PEException("invalid PEHeaderNTSpecificFields.SectionAlignment");
            FileAlignment = reader.ReadUInt32();
            if (FileAlignment != fileAlignment1 && FileAlignment != fileAlignment2)
                throw new PEException("invalid PEHeaderNTSpecificFields.FileAlignment");
            var actualOSMajor = reader.ReadUInt16();
            if (actualOSMajor != osMajor)
                throw new PEException("invalid PEHeaderNTSpecificFields.OSMajor");
            var actualOSMinor = reader.ReadUInt16();
            if (actualOSMinor != osMinor)
                throw new PEException("invalid PEHeaderNTSpecificFields.OSMinor");
            var actualUserMajor = reader.ReadUInt16();
            if (actualUserMajor != userMajor)
                throw new PEException("invalid PEHeaderNTSpecificFields.UserMajor");
            var actualUserMinor = reader.ReadUInt16();
            if (actualUserMinor != userMinor)
                throw new PEException("invalid PEHeaderNTSpecificFields.UserMinor");
            var actualSubSysMajor = reader.ReadUInt16();
            if (actualSubSysMajor != subSysMajor)
                throw new PEException("invalid PEHeaderNTSpecificFields.SubSysMajor");
            var actualSubSysMinor = reader.ReadUInt16();
            if (actualSubSysMinor != subSysMinor)
                throw new PEException("invalid PEHeaderNTSpecificFields.SubSysMinor");
            var actualReserved = reader.ReadUInt32();
            if (actualReserved != reserved)
                throw new PEException("invalid PEHeaderNTSpecificFields.Reserved");
            ImageSize = reader.ReadUInt32();
            if (ImageSize % sectionAlignment != 0)
                throw new PEException("invalid PEHeaderNTSpecificFields.ImageSize");
            HeaderSize = reader.ReadUInt32();
            if (HeaderSize % FileAlignment != 0)
                throw new PEException("invalid PEHeaderNTSpecificFields.HeaderSize");
            FileChecksum = reader.ReadUInt32();
            SubSystem = (SubSystem)reader.ReadUInt16();
            if (SubSystem != SubSystem.WINDOWS_CUI && SubSystem != SubSystem.WINDOWS_GUI)
                throw new PEException("invalid PEHeaderNTSpecificFields.SubSystem");
            DllFlags = reader.ReadUInt16();
            var actualStackReserveSize = reader.ReadUInt32();
            if (actualStackReserveSize != stackReserveSize)
                throw new PEException("invalid PEHeaderNTSpecificFields.StackReserveSize");
            StackCommitSize = reader.ReadUInt32();
            HeapReserveSize = reader.ReadUInt32();
            HeapCommitSize = reader.ReadUInt32();
            var actualLoaderFlags = reader.ReadUInt32();
            if (actualLoaderFlags != loaderFlags)
                throw new PEException("invalid PEHeaderNTSpecificFields.LoaderFlags");
            var actualNumberOfDataDirectories = reader.ReadUInt32();
            if (actualNumberOfDataDirectories != numberOfDataDirectories)
                throw new PEException("invalid PEHeaderNTSpecificFields.NumberOfDataDirectories");

            if (ctxt.Tracer != null)
            {
                ctxt.Tracer.AppendLine(String.Format("PEHeaderNTSpecificFields.ImageBase: {0:x8}", ImageBase));
                ctxt.Tracer.AppendLine(String.Format("PEHeaderNTSpecificFields.ImageSize: {0:x8}", ImageSize));
                ctxt.Tracer.AppendLine(String.Format("PEHeaderNTSpecificFields.HeaderSize: {0:x8}", HeaderSize));
                ctxt.Tracer.AppendLine(String.Format("PEHeaderNTSpecificFields.FileChecksum: {0:x8}", FileChecksum));
            }
        }

        public void Fixup(WriterContext ctxt)
        {
            ImageSize = Constants.RoundUp(ctxt.VirtualLimit(), sectionAlignment);
            HeaderSize = Constants.RoundUp(DOSHeader.Size + PEFileHeader.Size + PEOptionalHeader.Size, FileAlignment);
            FileChecksum = 0; // ????
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(ImageBase);
            writer.WriteUInt32(sectionAlignment);
            writer.WriteUInt32(FileAlignment);
            writer.WriteUInt16(osMajor);
            writer.WriteUInt16(osMinor);
            writer.WriteUInt16(userMajor);
            writer.WriteUInt16(userMinor);
            writer.WriteUInt16(subSysMajor);
            writer.WriteUInt16(subSysMinor);
            writer.WriteUInt32(reserved);
            writer.WriteUInt32(ImageSize);
            writer.WriteUInt32(HeaderSize);
            writer.WriteUInt32(FileChecksum);
            writer.WriteUInt16((ushort)SubSystem);
            writer.WriteUInt16(DllFlags);
            writer.WriteUInt32(stackReserveSize);
            writer.WriteUInt32(StackCommitSize);
            writer.WriteUInt32(HeapReserveSize);
            writer.WriteUInt32(HeapCommitSize);
            writer.WriteUInt32(loaderFlags);
            writer.WriteUInt32(numberOfDataDirectories);
        }
    }

    // S25.2.3.3
    public struct PEHeaderDataDirectories
    {
        public const uint Size = 128;

        private const ulong exportTable = 0;
        public SizedRVA<ImportTable> ImportTable;
        // SPEC VARIATION: Should be zero
        // private const ulong resourceTable = 0;
        public SizedRVA<byte[]> ResourceTable;
        private const ulong exceptionTable = 0;
        // SPEC VARIATION: Should be zero
        // private const ulong certificateTable = 0;
        public SizedRVA<byte[]> CertificateTable;
        public SizedRVA<RelocationTable> BaseRelocationTable;
        // SPEC VARIATION: Should be zero
        // private const ulong debugTable = 0;
        public SizedRVA<byte[]> Debug;
        private const ulong copyright = 0;
        private const ulong globalPtr = 0;
        private const ulong tlsTable = 0;
        private const ulong loadConfigTable = 0;
        private const ulong boundImport = 0;
        public AliasedSizedRVA IAT; // Same as ImportTable's ImportAddressTable field
        private const ulong delayImportDescriptor = 0;
        public SizedRVA<CLIHeader> CLIHeader;
        private const ulong reserved = 0;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var actualExportTable = reader.ReadUInt64();
            if (actualExportTable != exportTable)
                throw new PEException("invalid PEHeaderDataDirecties.ExportTable");
            ImportTable.Read(reader);
            ResourceTable.Read(reader);
            var actualExceptionTable = reader.ReadUInt64();
            if (actualExceptionTable != exceptionTable)
                throw new PEException("invalid PEHeaderDataDirecties.ExceptionTable");
            CertificateTable.Read(reader);
            BaseRelocationTable.Read(reader);
            Debug.Read(reader);
            var actualCopyright = reader.ReadUInt64();
            if (actualCopyright != copyright)
                throw new PEException("invalid PEHeaderDataDirecties.Copyright");
            var actualGlobalPtr = reader.ReadUInt64();
            if (actualGlobalPtr != globalPtr)
                throw new PEException("invalid PEHeaderDataDirecties.GlobalPtr");
            var actualTLSTable = reader.ReadUInt64();
            if (actualTLSTable != tlsTable)
                throw new PEException("invalid PEHeaderDataDirecties.TlsTable");
            var actualLoadConfigTable = reader.ReadUInt64();
            if (actualLoadConfigTable != loadConfigTable)
                throw new PEException("invalid PEHeaderDataDirecties.LoadConfigTable");
            var actualBoundImport = reader.ReadUInt64();
            if (actualBoundImport != boundImport)
                throw new PEException("invalid PEHeaderDataDirecties.BoundImport");
            IAT.Read(reader);
            var actualDelayImportDescriptor = reader.ReadUInt64();
            if (actualDelayImportDescriptor != delayImportDescriptor)
                throw new PEException("invalid PEHeaderDataDirecties.DelayImportDescriptor");
            CLIHeader.Read(reader);
            var actualReserved = reader.ReadUInt64();
            if (actualReserved != reserved)
                throw new PEException("invalid PEHeaderDataDirecties.Reserved");
        }

        public void Deref(ReaderContext ctxt)
        {
            ImportTable.Value.Read(ctxt, ImportTable.GetReaderNonNull(ctxt));
            ResourceTable.Value = ResourceTable.GetBytes(ctxt);
            // Silverlight bug: these end up pointing outside of the .rsrc section
            // CertificateTable.Value = CertificateTable.GetBytes(ctxt);
            BaseRelocationTable.Value.Read(ctxt, BaseRelocationTable.GetReaderNonNull(ctxt));
            Debug.Value = Debug.GetBytes(ctxt);
            CLIHeader.Value.Read(ctxt, CLIHeader.GetReaderNonNull(ctxt));

            ImportTable.Value.Deref(ctxt);
            CLIHeader.Value.Deref(ctxt);

            if (ctxt.Tracer != null)
            {
                ImportTable.Append(ctxt, "PEHeaderDataDirectories.ImportTable");
                ResourceTable.Append(ctxt, "PEHeaderDataDirectories.ResourceTable");
                CertificateTable.Append(ctxt, "PEHeaderDataDirectories.CertificateTable");
                BaseRelocationTable.Append(ctxt, "PEHeaderDataDirectories.BaseRelocationTable");
                Debug.Append(ctxt, "PEHeaderDataDirectories.Debug");
                CLIHeader.Append(ctxt, "PEHeaderDataDirectories.CLIHeader");
            }
        }

        public void Alloc(WriterContext ctxt)
        {
            ImportTable.Size = ImportTable.Value.Write(ctxt, ImportTable.Alloc(ctxt, Section.Text));
            if (ResourceTable.Value != null)
            {
                ResourceTable.Alloc(ctxt, Section.Rsrc).WriteBytes(ResourceTable.Value);
                ResourceTable.Size = (uint)ResourceTable.Value.Length;
            }
            // ##########
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt64(exportTable);
            ImportTable.Write(writer);
            ResourceTable.Write(writer);
            writer.WriteUInt64(exceptionTable);
            CertificateTable.Write(writer);
            BaseRelocationTable.Write(writer);
            Debug.Write(writer);
            writer.WriteUInt64(copyright);
            writer.WriteUInt64(globalPtr);
            writer.WriteUInt64(tlsTable);
            writer.WriteUInt64(loadConfigTable);
            writer.WriteUInt64(boundImport);
            IAT.Write(writer);
            writer.WriteUInt64(delayImportDescriptor);
            CLIHeader.Write(writer);
            writer.WriteUInt64(reserved);
        }
    }

    // S25.2.3
    public struct PEOptionalHeader
    {
        public const ushort Size = 224; // PEHeaderStandardFields.Size + PEHeaderNTSpecificFields.Size + PEHeaderDataDirectories.Size;

        public PEHeaderStandardFields StandardFields;
        public PEHeaderNTSpecificFields NTSpecificFields;
        public PEHeaderDataDirectories DataDirectories;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            if (reader.RemainingBytes < Size)
                throw new PEException("missig PEOptionalHeader");

            StandardFields.Read(ctxt, reader);
            NTSpecificFields.Read(ctxt, reader);
            DataDirectories.Read(ctxt, reader);
        }

        public void Deref(ReaderContext ctxt)
        {
            StandardFields.Deref(ctxt);
            DataDirectories.Deref(ctxt);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            StandardFields.Write(ctxt, writer);
            NTSpecificFields.Write(ctxt, writer);
            DataDirectories.Write(ctxt, writer);
        }
    }


    public enum Section
    {
        Text,
        Rsrc,
        Reloc
    }

    // S25.3
    public struct SectionHeader
    {
        public Section Section;
        public uint VirtualSize; // may be larger than SizeOfRawData
        public uint VirtualAddress; // Address in virtual memory w.r.t. PEHeaderNTSpecificFields.ImageBase
        public uint SizeOfRawData; // length of section data in PE file
        public FileOffset PointerToRawData;
        public uint PointerToRelocations; // aliased with ".reloc" section
        private const uint pointerToLinenumbers = 0;
        public ushort NumberOfRelocations;
        private const ushort numberOfLinenumbers = 0;
        public ImageSectionCharacteristics Characteristics;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var name = reader.ReadAsciiZeroPaddedString(8);
            if (string.IsNullOrEmpty(name))
                throw new PEException("invalid SectionHeader.Name");
            if (name.Equals(".text", StringComparison.Ordinal))
                Section = Section.Text;
            else if (name.Equals(".rsrc", StringComparison.Ordinal))
                Section = Section.Rsrc;
            else if (name.Equals(".reloc", StringComparison.Ordinal))
                Section = Section.Reloc;
            else
                throw new PEException("invalid SectionHeader.Name");

            VirtualSize = reader.ReadUInt32();
            VirtualAddress = reader.ReadUInt32();
            SizeOfRawData = reader.ReadUInt32();
            // NOTE: Virtual size may be less than raw data size
            if (VirtualSize > SizeOfRawData)
                // Need to support readers with implicit zero padding at end
                throw new NotImplementedException();
            PointerToRawData.Read(reader);
            PointerToRelocations = reader.ReadUInt32();
            var actualPointerToLinenumbers = reader.ReadUInt32();
            if (actualPointerToLinenumbers != pointerToLinenumbers)
                throw new PEException("invalid SectionHeader.PointerToLinenumbers");
            NumberOfRelocations = reader.ReadUInt16();
            var actualNumberOfLinenumbers = reader.ReadUInt16();
            if (actualNumberOfLinenumbers != numberOfLinenumbers)
                throw new PEException("invalid SectionHeader.NumberOfLinenumbers");
            Characteristics = (ImageSectionCharacteristics)reader.ReadUInt32();

            if (ctxt.Tracer != null)
            {
                ctxt.Tracer.AppendLine("SectionHeader {");
                ctxt.Tracer.Indent();
                ctxt.Tracer.AppendLine(String.Format("Name: {0}", Section.ToString()));
                ctxt.Tracer.AppendLine(String.Format("VirtualSize: {0:x8}", VirtualSize));
                ctxt.Tracer.AppendLine(String.Format("VirtualAddress: {0:x8}", VirtualAddress));
                ctxt.Tracer.AppendLine(String.Format("SizeOfRawData: {0:x8}", SizeOfRawData));
                PointerToRawData.Append(ctxt, "PointerToRawData");
                ctxt.Tracer.Outdent();
                ctxt.Tracer.AppendLine("}");
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            var name = default(string);
            switch (Section)
            {
            case Section.Text:
                name = ".text"; break;
            case Section.Rsrc:
                name = ".rsrc"; break;
            case Section.Reloc:
                name = ".reloc"; break;
            default:
                throw new ArgumentOutOfRangeException();
            }
            writer.WriteAsciiZeroPaddedString(name, 8);
            writer.WriteUInt32(VirtualSize);
            writer.WriteUInt32(VirtualAddress);
            writer.WriteUInt32(SizeOfRawData);
            PointerToRawData.Write(writer);
            writer.WriteUInt32(PointerToRelocations);
            writer.WriteUInt32(pointerToLinenumbers);
            writer.WriteUInt16(NumberOfRelocations);
            writer.WriteUInt16(numberOfLinenumbers);
            writer.WriteUInt32((uint)Characteristics);
        }
    }

    //
    // Imports
    //

    // S25.3.1
    public struct ImportTable
    {
        public const string MSCorEE = "mscoree.dll";

        public RVA<ImportLookupOrAddressTable> ImportLookupTable;
        private const uint dateTimeStamp = 0;
        private const uint forwarderChain = 0;
        public RVA<string> Name;
        public RVA<ImportLookupOrAddressTable> ImportAddressTable; // Same as PEHeaderDataDirectories' IAT field
        private const int paddingBytes = 20;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            ImportLookupTable.Read(reader);
            var actualDateTimeStamp = reader.ReadUInt32();
            if (actualDateTimeStamp != dateTimeStamp)
                throw new PEException("invalid ImportTable.DateTimeStamp");
            var actualForwarderChain = reader.ReadUInt32();
            if (actualForwarderChain != forwarderChain)
                throw new PEException("invalid ImportTable.ForwarderChain");
            Name.Read(reader);
            ImportAddressTable.Read(reader);
            reader.Pad(paddingBytes);
        }

        public void Deref(ReaderContext ctxt)
        {
            ImportLookupTable.Value.Read(ctxt, ImportLookupTable.GetReaderNonNull(ctxt));
            Name.Value = Name.GetReaderNonNull(ctxt).ReadAsciiZeroTerminatedString(1);
            if (!Name.Value.Equals(MSCorEE, StringComparison.OrdinalIgnoreCase))
                throw new PEException("invalid ImportTable.Name");
            ImportAddressTable.Value.Read(ctxt, ImportAddressTable.GetReaderNonNull(ctxt));

            ImportLookupTable.Value.Deref(ctxt);
            ImportAddressTable.Value.Deref(ctxt);

            if (ctxt.Tracer != null)
            {
                ImportLookupTable.Append(ctxt, "ImportTable.ImportLookupTable");
                Name.Append(ctxt, "ImportTable.Name");
                ImportAddressTable.Append(ctxt, "ImportTable.ImportAddressTable");
            }
        }

        public uint Write(WriterContext ctxt, BlobWriter writer)
        {
            var offset = writer.Offset;
            ImportLookupTable.Write(writer);
            writer.WriteUInt32(dateTimeStamp);
            writer.WriteUInt32(forwarderChain);
            Name.Write(writer);
            ImportAddressTable.Write(writer);
            writer.Pad(paddingBytes);
            return writer.Offset - offset;
        }
    }

    // S25.3.1
    public struct ImportLookupOrAddressTable
    {
        public RVA<HintNameTable> HintNameTable;
        private const uint padding = 0;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            HintNameTable.Read(reader);
            if (HintNameTable.Address >> 31 != 0)
                throw new PEException("invalid ImportLookupTable/ImportAddressTable.HintNameTable");
            var actualPadding = reader.ReadUInt32();
            if (actualPadding != padding)
                throw new PEException("invalid ImportLookupTable/ImportAddressTable.Padding");
        }

        public void Deref(ReaderContext ctxt)
        {
            HintNameTable.Value.Read(ctxt, HintNameTable.GetReaderNonNull(ctxt));

            if (ctxt.Tracer != null)
            {
                HintNameTable.Append(ctxt, "ImportLookupTable/ImportAddressTable.HintNameTable");
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            HintNameTable.Write(writer);
            writer.WriteUInt32(padding);
        }
    }

    // S25.3.1
    public struct HintNameTable
    {
        private const ushort hint = 0;
        public string Name;
        public const string ExeHintName = "_CorExeMain";
        public const string DllHintName = "_CorDllMain";

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var actualHint = reader.ReadUInt16();
            if (actualHint != hint)
                throw new PEException("invalid HintNameTable.Hint");
            Name = reader.ReadAsciiZeroTerminatedString(1);
            if (!Name.Equals(ExeHintName, StringComparison.Ordinal) &&
                !Name.Equals(DllHintName, StringComparison.Ordinal))
                throw new PEException("invalid HintNameTable.Name");
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt16(hint);
            writer.WriteAsciiZeroTerminatedString(Name, 1);
        }
    }

    //
    // Relocations
    //

    // S25.3.2
    public struct FixupEntry
    {
        public const int Size = 2;

        public ImageRelocation Type;
        public ushort Offset;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var word = reader.ReadUInt16();
            Type = (ImageRelocation)(word >> 12);
            Offset = (ushort)(word & 0xFFF);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            var word = (ushort)(((byte)Type) << 12 | Offset);
            writer.WriteUInt16(word);
        }
    }

    // S25.3.2
    public struct FixupBlock
    {
        public uint Page;  // Virtual address of page, w.r..t ImageBase
        public FixupEntry[] Entries;

        public static void Skip(BlobReader reader)
        {
            reader.Offset += 4;
            var blockSize = reader.ReadUInt32();
            reader.Offset += blockSize - 8;
        }

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Page = reader.ReadUInt32();
            if ((Page & 0xfff) != 0)
                throw new PEException("invalid FixupBlock.Page");
            var blockSize = reader.ReadUInt32();
            var numFixupEntries = (blockSize - 8)/ FixupEntry.Size;
            Entries = new FixupEntry[(int)numFixupEntries];
            for (var i = 0; i < numFixupEntries; i++)
                Entries[i].Read(ctxt, reader);
            reader.Align(4);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(Page);
            var blockSize = (uint)((Entries.Length * FixupEntry.Size) + 8);
            writer.WriteUInt32(blockSize);
            for (var i = 0; i < Entries.Length; i++)
                Entries[i].Write(ctxt, writer);
            writer.Align(4);
        }
    }

    // S25.3.2
    public struct RelocationTable
    {
        public FixupBlock[] Blocks;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var initOffset = reader.Offset;
            var n = 0;
            while (!reader.AtEndOfBlob) {
                FixupBlock.Skip(reader);
                n++;
            }
            Blocks = new FixupBlock[n];
            reader.Offset = initOffset;
            for (var i = 0; i < n; i++)
                Blocks[i].Read(ctxt, reader);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            for (var i = 0; i < Blocks.Length; i++)
                Blocks[i].Write(ctxt, writer);
        }
    }

    //
    // CLR
    //

    // S25.3.3
    public struct CLIHeader
    {
        private const uint cb = 72;
        public const ushort DefaultMajorRuntimeVersion = 2;
        public ushort MajorRuntimeVersion;
        public const ushort DefaultMinorRuntimeVersion = 5;
        public ushort MinorRuntimeVersion;
        public SizedRVA<MetadataHeader> MetaData;
        public RuntimeFlags Flags;
        // Can't use RowRef<PETableRow> for following since need to read token
        // before metadata tables have been established
        public uint EntryPointToken;
        public SizedRVA<byte[]> Resources;
        public SizedRVA<byte[]> StrongNameSignature;
        private const ulong codeManagerTable = 0;
        public SizedRVA<VtableFixups> VtableFixups;
        private const ulong exportAddressTableJumps = 0;
        private const ulong managedNativeHeader = 0;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var actualCb = reader.ReadUInt32();
            if (actualCb != cb)
                throw new PEException("invalid CLIHeader.ActualCb");
            MajorRuntimeVersion = reader.ReadUInt16();
            MinorRuntimeVersion = reader.ReadUInt16();
            MetaData.Read(reader);
            Flags = (RuntimeFlags)reader.ReadUInt32();
            EntryPointToken = reader.ReadUInt32();
            Resources.Read(reader);
            StrongNameSignature.Read(reader);
            var actualCodeManagerTable = reader.ReadUInt64();
            if (actualCodeManagerTable != codeManagerTable)
                throw new PEException("invalid CLIHeader.CodeManagerTable");
            VtableFixups.Read(reader);
            var actualExportAddressTableJumps = reader.ReadUInt64();
            if (actualExportAddressTableJumps != exportAddressTableJumps)
                throw new PEException("invalid CLIHeader.ExportAddressTableJumps");
            var actualManagedNativeHeader = reader.ReadUInt64();
            if (actualManagedNativeHeader != managedNativeHeader)
                throw new PEException("invalid CLIHeader.ManagedNativeHeader");
        }

        public void Deref(ReaderContext ctxt)
        {
            var metadataReader = MetaData.GetReaderNonNull(ctxt);
            MetaData.Value.Read(ctxt, metadataReader);
            ctxt.MetadataReader = metadataReader;
            ctxt.StreamHeaders = MetaData.Value.StreamHeaders; // Blob etc refs may now be resolved

            Resources.Value = Resources.GetBytes(ctxt);
            StrongNameSignature.Value = StrongNameSignature.GetBytes(ctxt);
            VtableFixups.Value.Read(ctxt, VtableFixups.GetReader(ctxt));

            if (ctxt.Tracer != null)
            {
                MetaData.Append(ctxt, "CLIHeader.MetaData");
                Resources.Append(ctxt, "CLIHeader.Resources");
                StrongNameSignature.Append(ctxt, "CLIHeader.StrongNameSignature");
                VtableFixups.Append(ctxt, "CLIHeader.VtableFixups");
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(cb);
            writer.WriteUInt16(MajorRuntimeVersion);
            writer.WriteUInt16(MinorRuntimeVersion);
            MetaData.Write(writer);
            writer.WriteUInt32((uint)Flags);
            writer.WriteUInt32(EntryPointToken);
            Resources.Write(writer);
            StrongNameSignature.Write(writer);
            writer.WriteUInt64(codeManagerTable);
            VtableFixups.Write(writer);
            writer.WriteUInt64(exportAddressTableJumps);
            writer.WriteUInt64(managedNativeHeader);
        }
    }

    // S25.3.3.3
    public struct VtableFixup
    {
        public const uint StructSize = 8;

        public uint VirtualAddress;
        public ushort Size;
        public CorVtable Type;

        public static void Skip(BlobReader reader)
        {
            reader.Offset += StructSize;
        }

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            VirtualAddress = reader.ReadUInt32();
            Size = reader.ReadUInt16();
            Type = (CorVtable)reader.ReadUInt16();
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(VirtualAddress);
            writer.WriteUInt16(Size);
            writer.WriteUInt16((ushort)Type);
        }
    }

    public struct VtableFixups
    {
        public VtableFixup[] Fixups;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            if (reader == null)
                Fixups = new VtableFixup[0];
            else
            {
                var initOffset = reader.Offset;
                var n = 0;
                while (!reader.AtEndOfBlob) {
                    VtableFixup.Skip(reader);
                    n++;
                }
                reader.Offset = initOffset;
                Fixups = new VtableFixup[n];
                for (var i = 0; i < n; i++)
                    Fixups[i].Read(ctxt, reader);
            }
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            for (var i = 0; i < Fixups.Length; i++)
                Fixups[i].Write(ctxt, writer);
        }
    }

    // S24.2.2
    public struct StreamHeader
    {
        public MetadataOffset Offset;
        public uint Size;
        public string Name;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            Offset.Read(reader);
            Size = reader.ReadUInt32();
            Name = reader.ReadAsciiZeroTerminatedString(4);
            if (string.IsNullOrEmpty(Name))
                throw new PEException("invalid StreamHeader.Name");
        }

        public void Write(WriterContext ctxt,  BlobWriter writer)
        {
            Offset.Write(writer);
            writer.WriteUInt32(Size);
            writer.WriteAsciiZeroTerminatedString(Name, 4);
        }
    }

    // S24.2.1
    public struct MetadataHeader
    {
        private const uint signature = 0x424A5342;
        public const ushort DefaultMajorVersion = 1;
        public ushort MajorVersion;
        public const ushort DefaultMinorVersion = 1;
        public ushort MinorVersion;
        private const uint reserved = 0;
        public const string DefaultVersion = "v2.0.50727";
        public string Version;
        private const ushort flags = 0;
        public StreamHeader[] StreamHeaders;

        public void Read(ReaderContext ctxt, BlobReader reader)
        {
            var actualSignature = reader.ReadUInt32();
            if (actualSignature != signature)
                throw new PEException("invalid MetadataHeader.Signature");
            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();
            var actualReserved = reader.ReadUInt32();
            if (actualReserved != reserved)
                throw new PEException("invalid MetadataHeader.Reserved");
            Version = reader.ReadUTF8SizedZeroPaddedString(4);
            var actualFlags = reader.ReadUInt16();
            if (actualFlags != flags)
                throw new PEException("invalid MetadataHeader.Flags");
            var numStreams = reader.ReadUInt16();
            StreamHeaders = new StreamHeader[numStreams];
            for (var i = 0; i < numStreams; i++)
                StreamHeaders[i].Read(ctxt, reader);
        }

        public void Write(WriterContext ctxt, BlobWriter writer)
        {
            writer.WriteUInt32(signature);
            writer.WriteUInt16(MajorVersion);
            writer.WriteUInt16(MinorVersion);
            writer.WriteUInt32(reserved);
            writer.WriteUTF8SizedZeroPaddedString(Version, 4);
            writer.WriteUInt16(flags);
            writer.WriteUInt16((ushort)StreamHeaders.Length);
            for (var i = 0; i < StreamHeaders.Length; i++)
                StreamHeaders[i].Write(ctxt, writer);
        }
    }

    //
    // File
    //

    // S25.1
    public class PEFile
    {
        public DOSHeader DOSHeader;
        public PEFileHeader PEFileHeader;
        public PEOptionalHeader PEOptionalHeader;
        public SectionHeader[] SectionHeaders;
        public MetadataTables Tables;

        public void Read(ReaderContext ctxt)
        {
            if (ctxt.Tracer != null)
            {
                ctxt.Tracer.Append("PEFile '");
                ctxt.Tracer.Append(ctxt.FileName);
                ctxt.Tracer.AppendLine("' {");
                ctxt.Tracer.Indent();
            }
            try
            {
                var reader = ctxt.GetFileReader();

                DOSHeader.Read(ctxt, reader);
                reader.Offset = DOSHeader.LfaNew.Offset; // Follow LfaNew pointer
                PEFileHeader.Read(ctxt, reader);
                PEOptionalHeader.Read(ctxt, reader);
                SectionHeaders = new SectionHeader[PEFileHeader.NumberOfSections];
                for (var i = 0; i < SectionHeaders.Length; i++)
                    SectionHeaders[i].Read(ctxt, reader);
                ctxt.SectionHeaders = SectionHeaders; // RVA's may now be resolved

                PEOptionalHeader.Deref(ctxt);

                Tables = new MetadataTables();
                ctxt.Tables = Tables; // Coded indexes may now be resolved
                Tables.Read(ctxt);
            }
            finally
            {
                if (ctxt.Tracer != null)
                {
                    ctxt.Tracer.Outdent();
                    ctxt.Tracer.AppendLine("}");
                }
            }
        }

        public uint EntryPointToken
        {
            get { return PEOptionalHeader.DataDirectories.CLIHeader.Value.EntryPointToken; }
        }

#if false
        private void Write(WriterContext ctxt)
        {
            // TextSection.PointerToRawData = Helpers.Align((uint)(376 + 40 * PEFileHeader.NumberOfSections), PEOptionalHeader.NTSpecificFields.SectionAlignment);
            TextSection.PointerToRawData = 0x200;
            PEOptionalHeader.StandardFields.EntryPointRVA = WriteEntryPoint();
            RelocationTable.Blocks.Add
                (new FixupBlock
                 {
                     PageRVA = TextSection.PointerToRawData,
                     Entries =
                         {
                             new FixupEntry
                             {
                                 Offset =
                                     (ushort)
                                     (PEOptionalHeader.StandardFields.EntryPointRVA - TextSection.PointerToRawData),
                                 Type = ImageRelocation.IMAGE_REL_BASED_HIGHLOW
                             }
                         }
                 });
            MetadataTables.PersistIndexes(this);

            MetadataHeader.PersistIndexes(this);
            CLIHeader.PersistIndexes(this);
            ImportAddressTable.PersistIndexes(this);
            ImportLookupTable.PersistIndexes(this);
            ImportTable.PersistIndexes(this);
            DosHeader.PersistIndexes(this);
            PEFileHeader.PersistIndexes(this);
            PEOptionalHeader.PersistIndexes(this);
            RelocationTable.Write(RelocationSection.Writer);
            foreach (var sectionHeader in SectionHeaders)
            {
                sectionHeader.PersistIndexes(this);
            }
            PEOptionalHeader.DataDirectories.BaseRelocationTable = RelocationSection.PointerToRawData;

            InitializePEFile();
            PersistIndexes();
            var writer = new BlobWriter();
            DosHeader.Write(writer);
            PEFileHeader.Write(writer);
            PEOptionalHeader.Write(writer);

            // TODO: precalc offsets in written section headers...
            // TODO: Write stream data
            foreach (var sectionHeader in SectionHeaders)
            {
                sectionHeader.Write(writer);
            }
            foreach (var sectionHeader in SectionHeaders)
            {
                writer.EnsureAtOffset(sectionHeader.PointerToRawData);
                sectionHeader.WriteData(this, writer);
            }
            return writer.GetBlob();
        }
#endif

#if false
        public static PEFile CreateNew()
        {
            var file = new PEFile();
            file.InitializePEFile();
            file.DosHeader = DOSHeader.CreateNew();
            file.PEFileHeader = PEFileHeader.CreateNew();
            file.PEOptionalHeader = PEOptionalHeader.CreateNew();
            file.ImportTable = ImportTable.CreateNew();
            file.ImportAddressTable = ImportAddressTable.CreateNew();
            file.ImportLookupTable = ImportLookupTable.CreateNew();
            file.CLIHeader = CLIHeader.CreateNew();
            file.MetadataHeader = MetadataHeader.CreateNew();
            file.MetadataTables = MetadataTables.CreateNew();
            return file;
        }
        private void InitializePEFile()
        {
            SectionHeaders.Clear();
            SectionHeaders.Add(TextSection = new SectionHeader { Name = ".text" });
            SectionHeaders.Add(ResourceSection = new SectionHeader { Name = ".rsrc" });
            SectionHeaders.Add(RelocationSection = new SectionHeader { Name = ".reloc" });
            TextSection.Characteristics = ImageSectionCharacteristics.MEM_READ;

            MetadataHeader.StreamHeaders.Clear();
            MetadataHeader.StreamHeaders.Add(StringsHeap = new StreamHeader { Name = "#Strings" });
            MetadataHeader.StreamHeaders.Add(UserStringHeap= new StreamHeader { Name = "#US" });
            MetadataHeader.StreamHeaders.Add(BlobHeap = new StreamHeader { Name = "#Blob" });
            MetadataHeader.StreamHeaders.Add(GuidHeap = new StreamHeader { Name = "#GUID" });
            MetadataHeader.StreamHeaders.Add(MetadataHeap = new StreamHeader { Name = "#~" });            

            StringsHeap.Writer.WriteByte((byte)0);
            GuidHeap.Writer.WriteByte((byte)0);
            BlobHeap.Writer.WriteByte((byte)0);
            UserStringHeap.Writer.WriteByte((byte)0);            
        }


        private uint WriteEntryPoint()
        {
            TextSection.Writer.Align(2);
            var index = TextSection.Writer.WriteByte((byte)0xff);
            TextSection.Writer.WriteByte((byte)0x25);
            var offset = ImportTable.ImportAddressTable + PEOptionalHeader.StandardFields.BaseOfCode;
            TextSection.Writer.WriteUInt32(offset);
            TextSection.Writer.WriteUInt32((uint)0);
            TextSection.Writer.WriteUInt32((uint)0);
            return index;
        }
#endif

    }
}
