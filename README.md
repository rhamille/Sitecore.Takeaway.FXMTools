# Sitecore.Takeaway.FXMTools

Sitecore Experience Tag Management anyone?

This is a proof-of-concept on how to use FXM for "tagging" Sitecore internal page events. 

FXM has been a great tool to track visits, tag and raise events, goals, campaigns from external websites. 

What I like the most is the ability to raise page element level events, such as button clicks, and so on and associate it to a Sitecore Event, Goal, or Campaign.

It's the same tagging concept comparable to Google Tag Manager and Adobe Dynamic Tag Management.

However, currently, we are unable to use the same feature to tag internal Sitecore pages/components. 

If we were to tag and track a button click of lets say a "Buy Now" CTA from a media carousel, we would normally go to the backend and raise a Sitecore event programmatically for Sitecore to track.

Prior to FXM, I have seen implementations of creating a custom web service/api and invoke them through ajax by adding js snippets to elements that needs to raise the event.

Since FXM already has that builtin in functionality, it is quite tempting to just register an internal "website" over in FXM and start tracking it. However, this approach often lead to double counting of page visits and "messy" reporting in Experience Analytics.

As such, to resolve those issue, we need to tweak FXM on how it handles internal page visits and events.

1. Update the Domain Matcher template, to include a checkbox field "Is Internal", (not a good idea of messing around with the ootb tempates, but for POC sake we do this approach for now)

2. Create a new website in FXM and set the field "Is Internal" to true

3. Override the following processors of "tracking.trackpagevisit" pipeline

  **TrackPageVisitProcessor**

  We need to cancel the tracking for the current page visit to avoid double counting for internal pages as highlighted above.

  ```xml
<!-- <processor type="Sitecore.FXM.Pipelines.Tracking.TrackPageVisit.TrackPageVisitProcessor, Sitecore.FXM" /> 
-->

<processor type="Sitecore.Takeaway.FXMTools.Pipelines.Tracking.TrackPageVisit.TrackPageVisitProcessor, Sitecore.Takeaway.FXMTools"/>
  ```

  ```c#
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
  ```

  **FixRequestUrlProcessor**

  We need to check if the url being processed is internal site, if it is, we need to set the CurrenPage Url to PathAndQuery instead of absolute, this is important so that it shows neatly and consistent in Experience Analytics

  ```xml
<!-- <processor type="Sitecore.FXM.Pipelines.Tracking.TrackPageVisit.FixRequestUrlProcessor, Sitecore.FXM" /> 
-->

<processor type="Sitecore.Takeaway.FXMTools.Pipelines.Tracking.TrackPageVisit.FixRequestUrlProcessor, Sitecore.Takeaway.FXMTools"/>
  ```
 
  ```c#
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
  ```

4. Override the following processor of the "tracking.triggerpageevent" pipeline

  **EnsureCurrentPageIsTrackedProcessor**

  Before we let FXM raise the event, we need to ensure that the SiteName is properly set.

  ```xml
<!-- <processor type="Sitecore.FXM.Pipelines.Tracking.BeforeEvent.EnsureCurrentPageIsTrackedProcessor, Sitecore.FXM" /> 
-->

<processor type="Sitecore.Takeaway.FXMTools.Pipelines.Tracking.BeforeEvent.EnsureCurrentPageIsTrackedProcessor, Sitecore.Takeaway.FXMTools"/>
  ```

  ```c#
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
  ```

  Once this is setup, you can use FXM for tagging internal pages in Sitcore.
  
  
