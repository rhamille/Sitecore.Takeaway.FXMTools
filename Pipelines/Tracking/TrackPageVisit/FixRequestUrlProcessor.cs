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
    public class FixRequestUrlProcessor : ITrackPageVisitProcessor, ITrackPageVisitProcessor<ITrackPageVisitArgs>
    {
        public void Process(ITrackPageVisitArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)args.TrackingRequest, "TrackingRequest");
            Assert.ArgumentNotNull((object)args.TrackerProvider, "TrackerProvider");
            Uri url = args.TrackingRequest.Url;
            if (!(url != (Uri)null))
                return;
            var isInternal = (CheckboxField)args.DomainMatcher.Item.Fields["Is Internal"];
            if (isInternal.Checked)
            {
                args.TrackerProvider.Current.CurrentPage.SetUrl(url.PathAndQuery);
                Log.Debug("[FixInternalRequestUrlProcessor] Domain is internal - tracking " + url.PathAndQuery + " instead of " + url.AbsoluteUri);
            }
            else
            {
                args.TrackerProvider.Current.CurrentPage.SetUrl(url.AbsoluteUri);
                Log.Debug("[FixInternalRequestUrlProcessor] Domain is external - tracking " + url.AbsoluteUri);
            }
        }
    }
}
