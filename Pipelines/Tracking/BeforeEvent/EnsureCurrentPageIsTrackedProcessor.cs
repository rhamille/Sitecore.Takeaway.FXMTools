using Sitecore.Analytics.Tracking;
using Sitecore.Collections;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.FXM.Matchers;
using Sitecore.FXM.Pipelines.Tracking;
using Sitecore.FXM.Pipelines.Tracking.ValidateRequest;
using Sitecore.FXM.Sites;
using Sitecore.FXM.Tracking;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.Sites;
using Sitecore.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sitecore.Takeaway.FXMTools.Pipelines.Tracking.BeforeEvent
{
    public class EnsureCurrentPageIsTrackedProcessor : ITrackingProcessor, ITrackingProcessor<ITrackingArgs>
    {
        public void Process(ITrackingArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            Assert.ArgumentNotNull((object)args.TrackerProvider, "TrackerProvider");
            Assert.ArgumentNotNull((object)args.TrackerProvider.Current, "Current tracker provider");
            Assert.ArgumentNotNull((object)args.TrackingRequest, "TrackingRequest");


            if (args.CurrentPageVisit != null)
                return;

            Log.Debug("[EnsureCurrentPageIsTrackedProcessor] CurrentPageVisit is null - StartTracking()");

            if (args.TrackerProvider.Current.CurrentPage == null)
            {
                args.TrackerProvider.Current.StartTracking();
            }

            if (args.TrackerProvider.Current.Interaction != null)
            {
                IPageContext pageInInteraction = this.GetCurrentPageInInteraction(args.TrackerProvider.Current.Interaction, args.TrackingRequest.Url);
                if (pageInInteraction != null)
                {
                    bool isInternal = false;

                    if (HttpContext.Current.Request.Url.Host == args.TrackingRequest.Url.Host)
                    {
                        isInternal = true;

                        SiteContext siteContext = Sitecore.Context.Site;

                        var siteProperties = new StringDictionary();

                        foreach (var key in siteContext.Properties)
                        {
                            siteProperties.Add((string)key, siteContext.Properties[(string)key] ?? string.Empty);
                        }

                        args.Context.SetSite(siteProperties);

                        Log.Debug("[EnsureCurrentPageIsTrackedProcessor] Interaction is not null - " + args.TrackingRequest.Url);

                        args.TrackerProvider.Current.CurrentPage.SetUrl(args.TrackingRequest.Url.PathAndQuery);

                        var item = Sitecore.Context.Database.GetItem(siteContext.StartPath + args.TrackingRequest.Url.AbsolutePath);

                        if (item != null)
                        {
                            args.TrackerProvider.Current.CurrentPage.Item = new Analytics.Model.ItemData();
                            args.TrackerProvider.Current.CurrentPage.Item.Id = item.ID.ToGuid();
                            args.TrackerProvider.Current.CurrentPage.Item.Language = item.Language.Name;
                            args.TrackerProvider.Current.CurrentPage.Item.Version = item.Version.Number;
                        }

                        args.TrackerProvider.Current.Interaction.SiteName = siteContext.Name;
                    }

                    args.CurrentPageVisit = pageInInteraction;
                    if (isInternal) args.CurrentPageVisit.Item = args.TrackerProvider.Current.CurrentPage.Item;
                }
            }

            if (args.CurrentPageVisit != null)
                return;

            Log.Debug("[EnsureCurrentPageIsTrackedProcessor] CurrentPageVisit is still null - Fail");

            args.AbortAndFailPipeline("the current page has not been tracked in the current session.", TrackingResultCode.CurrentPageMustBeTracked);
        }

        protected IPageContext GetCurrentPageInInteraction(CurrentInteraction interaction, Uri trackedPageUrl)
        {
            IPageContext pageContext = (IPageContext)interaction.CurrentPage;
            if (interaction.CurrentPage == null)
            {
                List<IPageContext> list = Enumerable.ToList<IPageContext>(interaction.GetPages());
                pageContext = Enumerable.Any<IPageContext>((IEnumerable<IPageContext>)list) ? Enumerable.FirstOrDefault<IPageContext>((IEnumerable<IPageContext>)list, (Func<IPageContext, bool>)(p => string.Format("{0}{1}", (object)p.Url.Path, (object)p.Url.QueryString) == trackedPageUrl.PathAndQuery)) : (IPageContext)null;
            }
            return pageContext;
        }
    }
}
