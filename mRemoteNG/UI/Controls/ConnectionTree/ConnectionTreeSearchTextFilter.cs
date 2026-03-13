using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Container;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    [SupportedOSPlatform("windows")]
    public class ConnectionTreeSearchTextFilter : IModelFilter
    {
        public string FilterText { get; set; } = "";

        /// <summary>
        /// A list of <see cref="ConnectionInfo"/> objects that should
        /// always be included in the output, regardless of matching
        /// the desired <see cref="FilterText"/>.
        /// </summary>
        public List<ConnectionInfo> SpecialInclusionList { get; } = [];

        public bool Filter(object modelObject)
        {
            if (modelObject is not ConnectionInfo objectAsConnectionInfo)
                return false;

            if (SpecialInclusionList.Contains(objectAsConnectionInfo))
                return true;

            return MatchesConnectionOrChildren(objectAsConnectionInfo, FilterText.ToLowerInvariant());
        }

        private static bool MatchesConnectionOrChildren(ConnectionInfo connectionInfo, string filterTextLower)
        {
            if (MatchesConnection(connectionInfo, filterTextLower))
                return true;

            if (connectionInfo is not ContainerInfo containerInfo)
                return false;

            return containerInfo.Children.Any(child => MatchesConnectionOrChildren(child, filterTextLower));
        }

        private static bool MatchesConnection(ConnectionInfo connectionInfo, string filterTextLower)
        {
            return connectionInfo.Name.ToLowerInvariant().Contains(filterTextLower) ||
                   connectionInfo.Hostname.ToLowerInvariant().Contains(filterTextLower) ||
                   connectionInfo.Description.ToLowerInvariant().Contains(filterTextLower);
        }
    }
}
