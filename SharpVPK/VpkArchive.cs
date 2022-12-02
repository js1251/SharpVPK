using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpVPK.Exceptions;
using SharpVPK.V1;

namespace SharpVPK {
    public class VpkArchive {
        public List<VpkDirectory> Directories { get; set; }
        public bool IsMultiPart { get; set; }
        private VpkReaderBase _reader;
        internal List<ArchivePart> Parts { get; set; }
        internal string ArchivePath { get; set; }

        public VpkArchive() {
            Directories = new List<VpkDirectory>();
        }

        public void Load(string filename, VpkVersions.Versions version = VpkVersions.Versions.V1) {
            ArchivePath = filename;
            IsMultiPart = filename.EndsWith("_dir.vpk");
            if (IsMultiPart)
                LoadParts(filename);
            if (version == VpkVersions.Versions.V1)
                _reader = new VpkReaderV1(filename);
            else if (version == VpkVersions.Versions.V2)
                _reader = new V2.VpkReaderV2(filename);
            var hdr = _reader.ReadArchiveHeader();
            if (!hdr.Verify())
                throw new ArchiveParsingException("Invalid archive header");
            Directories.AddRange(_reader.ReadDirectories(this));
        }

        public void Load(byte[] file, VpkVersions.Versions version = VpkVersions.Versions.V1) {
            if (version == VpkVersions.Versions.V1)
                _reader = new VpkReaderV1(file);
            else if (version == VpkVersions.Versions.V2)
                _reader = new V2.VpkReaderV2(file);
            var hdr = _reader.ReadArchiveHeader();
            if (!hdr.Verify())
                throw new ArchiveParsingException("Invalid archive header");
            Directories.AddRange(_reader.ReadDirectories(this));
        }

        private void LoadParts(string filePath) {
            Parts = new List<ArchivePart>();

            var fileName = Path.GetFileName(filePath);

            // ignore incorrect files
            if (!fileName.Contains("_dir.vpk")) {
                return;
            }
            
            var fileBaseName = fileName.Replace("_dir.vpk", "");

            var dir = Path.GetDirectoryName(filePath);

            foreach (var subFile in Directory.GetFiles(dir)) {
                // ignore self
                if (subFile.Equals(filePath)) {
                    continue;
                }

                var subFileName = Path.GetFileName(subFile);

                if (!subFileName.Contains("_")) {
                    continue;
                }
                
                var subLastUnderscoreIndex = subFileName.LastIndexOf('_');
                var subFileBaseName = subFileName.Substring(0, subLastUnderscoreIndex);

                // ignore other files and vpk archives of other base archives
                if (!subFileBaseName.Equals(fileBaseName)) {
                    continue;
                }

                var fileInfo = new FileInfo(subFile);
                var subSplit = subFileName.Split('_');
                var stringNumber = subSplit[subSplit.Length - 1].Replace(".vpk", "");

                var partIdx = int.Parse(stringNumber);
                Parts.Add(new ArchivePart((uint)fileInfo.Length, partIdx, subFile));
            }

            Parts.Add(new ArchivePart((uint)new FileInfo(filePath).Length, -1, filePath));
            Parts = Parts.OrderBy(p => p.Index).ToList();
        }
    }
}