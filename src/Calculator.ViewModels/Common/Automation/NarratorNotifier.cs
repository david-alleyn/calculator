// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if WINDOWS_UWP

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;

#else

using System;

#endif


#if WINDOWS_UWP
namespace CalculatorApp.ViewModel.Common.Automation
{
    public sealed class NarratorNotifier : DependencyObject
    {
        private UIElement _announcementElement;

        private static DependencyProperty s_announcementProperty;

        public NarratorNotifier()
        {
        }

        public NarratorAnnouncement Announcement
        {
            get => GetAnnouncement(this);
            set => SetAnnouncement(this, value);
        }

        public static DependencyProperty AnnouncementProperty => s_announcementProperty;

        public static NarratorAnnouncement GetAnnouncement(DependencyObject element)
        {
            return (NarratorAnnouncement)element.GetValue(s_announcementProperty);
        }

        public static void SetAnnouncement(DependencyObject element, NarratorAnnouncement value)
        {
            element.SetValue(s_announcementProperty, value);
        }

        public static void RegisterDependencyProperties()
        {
            s_announcementProperty = DependencyProperty.Register(
                "Announcement",
                typeof(NarratorAnnouncement),
                typeof(NarratorNotifier),
                new PropertyMetadata(null, OnAnnouncementChanged));
        }

        public void Announce(NarratorAnnouncement announcement)
        {
            if (NarratorAnnouncement.IsValid(announcement))
            {
                if (_announcementElement == null)
                {
                    _announcementElement = new TextBlock();
                }

                var peer = FrameworkElementAutomationPeer.FromElement(_announcementElement);
                if (peer != null)
                {
                    peer.RaiseNotificationEvent(
                        announcement.Kind,
                        announcement.Processing,
                        announcement.Announcement,
                        announcement.ActivityId);
                }
            }
        }

        private static void OnAnnouncementChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is NarratorNotifier instance)
            {
                instance.Announce(e.NewValue as NarratorAnnouncement);
            }
        }
    }
}

#else

using System;

namespace CalculatorApp.ViewModel.Common.Automation
{
    // UI automation notifications are not supported on Linux yet. The
    // announcement plumbing stays so callers need no changes; the notifier
    // only stores the latest announcement.
    public sealed class NarratorNotifier
    {
        public NarratorAnnouncement Announcement { get; set; }

        public void Announce(NarratorAnnouncement announcement)
        {
            if (NarratorAnnouncement.IsValid(announcement))
            {
                Announcement = announcement;
            }
        }

        public static void RegisterDependencyProperties()
        {
        }

        public static NarratorAnnouncement GetAnnouncement(object element)
        {
            return null;
        }
    }
}
#endif

