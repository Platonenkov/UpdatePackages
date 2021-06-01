using System.Collections.Generic;

namespace UpdatePackages.Classes
{
    public record Section
    {
        public string FileMask { get; init; }
        public IEnumerable<string> Regular { get; init; }
    }

    public class Package
    {
        public string Library { get; init; }
        public string OldVersion { get; init; }
        public string NewVersion { get; init; }
        public void Deconstruct(out string LibraryName, out string OldValue, out string NewValue) => (LibraryName, OldValue, NewValue) = (this.Library, OldVersion, NewVersion);
    }

    public record UpdatingPackage
    {
        public IEnumerable<Section> Sections { get; init; }
        public IEnumerable<Package> Packages { get; init; }
        public void Deconstruct(out IEnumerable<Package> packages, out IEnumerable<Section> sections) => (packages, sections) = (Packages, Sections);

    }
}