using Sitecore.Diagnostics;
using Sitecore.FXM.Abstractions;
using Sitecore.FXM.Pipelines.Tracking.RegisterPageEvent;
using Sitecore.FXM.Tracking;
using System;

namespace Sitecore.Takeaway.FXMTools.Pipelines.Tracking.RegisterPageEvent
{
    public class EnsureEventItemExistsProcessor : IRegisterPageEventProcessor, IRegisterPageEventProcessor<RegisterPageEventArgs>
    {
        private readonly ISitecoreDatabaseContext databaseContext;

        public EnsureEventItemExistsProcessor()
          : this((ISitecoreDatabaseContext)new SitecoreContextWrapper())
        {
        }

        public EnsureEventItemExistsProcessor(ISitecoreDatabaseContext databaseContext)
        {
            Assert.ArgumentNotNull((object)databaseContext, "databaseContext");
            this.databaseContext = databaseContext;
        }

        public void Process(RegisterPageEventArgs args)
        {
            if (args.PageEventItem != null)
                return;
            if (args.EventParameters.ById)
            {
                args.PageEventItem = this.databaseContext.Database.GetItem(args.EventParameters.Id);
            }
            else
            {
                string path;
                switch (args.EventParameters.EventType)
                {
                    case PageEventType.Goal:
                        path = "/sitecore/system/Marketing Control Panel/Goals/" + args.EventParameters.Name;
                        break;
                    case PageEventType.Event:
                        path = "/sitecore/system/Settings/Analytics/Page Events/" + args.EventParameters.Name;
                        break;
                    case PageEventType.Campaign:
                        path = "/sitecore/system/Marketing Control Panel/Campaigns/" + args.EventParameters.Name;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("EventType", "The event type is not recognised so no event item can be verified: " + args.EventParameters.Name);
                }
                args.PageEventItem = this.databaseContext.Database.GetItem(path);
            }
            if (args.PageEventItem != null)
                return;
            args.AbortAndFailPipeline(string.Format("The {0} {1} does not exist.", (object)args.EventParameters.EventType, args.EventParameters.ById ? (object)args.EventParameters.Id.ToString() : (object)args.EventParameters.Name), TrackingResultCode.TrackingEntityDoesNotExist);
        }
    }
}
