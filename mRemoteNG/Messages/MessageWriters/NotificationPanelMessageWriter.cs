using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;
using mRemoteNG.UI;
using mRemoteNG.UI.Window;

namespace mRemoteNG.Messages.MessageWriters
{
    [SupportedOSPlatform("windows")]
    public class NotificationPanelMessageWriter(ErrorAndInfoWindow messageWindow) : IMessageWriter
    {
        private readonly ErrorAndInfoWindow _messageWindow = messageWindow ?? throw new ArgumentNullException(nameof(messageWindow));
        private readonly List<ListViewItem> _pendingItems = [];
        private readonly object _pendingItemsLock = new();
        private bool _eventsSubscribed;

        public void Write(IMessage message)
        {
            NotificationMessageListViewItem lvItem = new(message);
            AddToList(lvItem);
        }

        private void AddToList(ListViewItem lvItem)
        {
            if (_messageWindow.IsDisposed || _messageWindow.lvErrorCollector.IsDisposed)
            {
                return;
            }

            if (!EnsureMessageListReady())
            {
                QueuePendingItem(lvItem);
                return;
            }

            if (_messageWindow.lvErrorCollector.InvokeRequired)
            {
                try
                {
                    _messageWindow.lvErrorCollector.Invoke((MethodInvoker)(() => AddToList(lvItem)));
                }
                catch (System.ComponentModel.InvalidAsynchronousStateException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
            else
            {
                _messageWindow.lvErrorCollector.Items.Insert(0, lvItem);

                if (_messageWindow.lvErrorCollector.Items.Count > 0)
                {
                    _messageWindow.pbError.Visible = true;
                }
            }
        }

        private bool EnsureMessageListReady()
        {
            if (_messageWindow.IsDisposed || _messageWindow.lvErrorCollector.IsDisposed)
            {
                return false;
            }

            if (_messageWindow.lvErrorCollector.IsHandleCreated)
            {
                return true;
            }

            SubscribeToActivationEvents();

            try
            {
                _messageWindow.CreateControl();
                _messageWindow.lvErrorCollector.CreateControl();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            return _messageWindow.lvErrorCollector.IsHandleCreated;
        }

        private void SubscribeToActivationEvents()
        {
            if (_eventsSubscribed)
            {
                return;
            }

            _messageWindow.HandleCreated += MessageWindow_HandleCreated;
            _messageWindow.VisibleChanged += MessageWindow_VisibleChanged;
            _messageWindow.lvErrorCollector.HandleCreated += MessageList_HandleCreated;
            _eventsSubscribed = true;
        }

        private void QueuePendingItem(ListViewItem lvItem)
        {
            lock (_pendingItemsLock)
            {
                _pendingItems.Add(lvItem);
            }
        }

        private void FlushPendingItems()
        {
            if (_messageWindow.IsDisposed || _messageWindow.lvErrorCollector.IsDisposed || !_messageWindow.lvErrorCollector.IsHandleCreated)
            {
                return;
            }

            List<ListViewItem> itemsToFlush;
            lock (_pendingItemsLock)
            {
                if (_pendingItems.Count == 0)
                {
                    return;
                }

                itemsToFlush = [.. _pendingItems];
                _pendingItems.Clear();
            }

            foreach (ListViewItem item in itemsToFlush)
            {
                AddToList(item);
            }
        }

        private void MessageWindow_HandleCreated(object sender, EventArgs e)
        {
            FlushPendingItems();
        }

        private void MessageWindow_VisibleChanged(object sender, EventArgs e)
        {
            if (_messageWindow.Visible)
            {
                FlushPendingItems();
            }
        }

        private void MessageList_HandleCreated(object sender, EventArgs e)
        {
            FlushPendingItems();
        }
    }
}
