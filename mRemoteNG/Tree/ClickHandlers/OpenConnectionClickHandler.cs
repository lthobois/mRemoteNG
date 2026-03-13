using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.Connection;

namespace mRemoteNG.Tree.ClickHandlers
{
    [SupportedOSPlatform("windows")]
    public class OpenConnectionClickHandler : ITreeNodeClickHandler<ConnectionInfo>
    {
        private readonly IConnectionInitiator _connectionInitiator;
        private readonly Func<ConnectionInfo, IEnumerable<ConnectionInfo>> _nodesToOpenProvider;

        public OpenConnectionClickHandler(
            IConnectionInitiator connectionInitiator,
            Func<ConnectionInfo, IEnumerable<ConnectionInfo>> nodesToOpenProvider = null)
        {
            if (connectionInitiator == null)
                throw new ArgumentNullException(nameof(connectionInitiator));

            _connectionInitiator = connectionInitiator;
            _nodesToOpenProvider = nodesToOpenProvider;
        }

        public void Execute(ConnectionInfo clickedNode)
        {
            if (clickedNode == null)
                throw new ArgumentNullException(nameof(clickedNode));

            IEnumerable<ConnectionInfo> nodesToOpen = _nodesToOpenProvider?.Invoke(clickedNode) ?? [clickedNode];
            foreach (ConnectionInfo node in nodesToOpen.Where(node =>
                         node.GetTreeNodeType() == TreeNodeType.Connection ||
                         node.GetTreeNodeType() == TreeNodeType.PuttySession))
            {
                _connectionInitiator.OpenConnection(node);
            }
        }
    }
}
