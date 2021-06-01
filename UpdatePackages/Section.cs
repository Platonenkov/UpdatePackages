using System.Collections.Generic;

namespace UpdatePackages
{
    public record Section
    {
        public string FileMask { get; init; }
        public IEnumerable<UpdatingPackage> Updating { get; init; }
    }
}