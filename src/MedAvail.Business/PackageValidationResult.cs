using System.Collections.Generic;
using System.Linq;

namespace MedAvail.Business
{
    /// <summary>Outcome of validating a package definition (pure business rules).</summary>
    public sealed class PackageValidationResult
    {
        private readonly List<string> _errors = new();

        public IReadOnlyList<string> Errors => _errors;
        public bool IsValid => _errors.Count == 0;

        public void AddError(string message) => _errors.Add(message);

        public string Summary =>
            IsValid ? "Valid" : string.Join("; ", _errors);
    }
}
