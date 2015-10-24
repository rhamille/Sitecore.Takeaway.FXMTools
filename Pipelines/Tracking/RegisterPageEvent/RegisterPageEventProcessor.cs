using Sitecore;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Data.Items;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent;
using Sitecore.FXM.Tracking;
using System.Collections.Generic;

namespace Sitecore.Takeaway.FXMTools.Pipelines.Tracking.RegisterPageEvent
{
    public class RegisterPageEventProcessor : IRegisterPageEventProcessor, IRegisterPageEventProcessor<RegisterPageEventArgs>
    {
        public void Process(RegisterPageEventArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.IsNotNull((object)args.PageEventItem, "No item has been found corresponding to the page event.");
            Assert.IsNotNull((object)args.CurrentPage, "The current page is not tracked in the current session.  No events can be triggered.");
            switch (args.EventParameters.EventType)
            {
                case PageEventType.Goal:
                case PageEventType.Event:
                    PageEventItem pageEventItem = new PageEventItem(args.PageEventItem);
                    args.CurrentPage.Register(pageEventItem);
                    break;
                case PageEventType.Campaign:
                    CampaignItem campaignItem = new CampaignItem(args.PageEventItem);
                    args.CurrentPage.TriggerCampaign(campaignItem);
                    break;
                case PageEventType.Element:
                    TrackingField trackingField = new TrackingField(args.PageEventItem.Fields["__Tracking"]);
                    foreach (TrackingField.PageEventData pageEventData in trackingField.Events)
                    {
                        Item obj = trackingField.InnerField.Item;
                        PageEventData pageData = new PageEventData(pageEventData.Name)
                        {
                            Data = pageEventData.Data,
                            ItemId = obj.ID.Guid,
                            DataKey = StringUtil.Right(obj.Paths.Path, 100)
                        };
                        args.CurrentPage.Register(pageData);
                    }
                    using (IEnumerator<CampaignItem> enumerator = trackingField.Campaigns.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            CampaignItem current = enumerator.Current;
                            args.CurrentPage.TriggerCampaign(current);
                        }
                        break;
                    }
            }
        }
    }
}
