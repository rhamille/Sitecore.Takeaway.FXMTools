using Sitecore.Analytics.Tracking;
using Sitecore.Data.Fields;
using Sitecore.Diagnostics;
using Sitecore.FXM.Pipelines.Tracking.TrackPageVisit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Takeaway.FXMTools.Pipelines.Tracking.TrackPageVisit
{
    public class TrackPageVisitProcessor : ITrackPageVisitProcessor, ITrackPageVisitProcessor<ITrackPageVisitArgs>
    {
        public void Process(ITrackPageVisitArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)args.TrackerProvider, "TrackerProvider");
            args.TrackerProvider.Current.StartTracking();
            args.CurrentPageVisit = (IPageContext)args.TrackerProvider.Current.Interaction.CurrentPage;
            var isInternal = (CheckboxField)args.DomainMatcher.Item.Fields["Is Internal"];
            if (isInternal.Checked)
            {
                args.TrackerProvider.Current.CurrentPage.Cancel();
                Log.Debug("[TrackPageVisitProcessorCustom] Domain is internal - cancelling page visit to avoid double counting");
            }
        }
    }
}
