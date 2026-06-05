using System.Collections.Generic;

namespace MedAvail.Common
{
    /// <summary>
    /// Probes connectivity to the MedAvail databases and confirms that each
    /// connection actually lands in the expected database.
    /// </summary>
    public interface IDatabaseConnectivityChecker
    {
        /// <summary>Probe a single database.</summary>
        ConnectivityResult Check(MedAvailDatabase database);

        /// <summary>Probe all four databases.</summary>
        IReadOnlyList<ConnectivityResult> CheckAll();
    }
}
