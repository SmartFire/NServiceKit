using System.Net;
using System.Web;
using NServiceKit.Common.Web;
using NServiceKit.ServiceHost;

namespace NServiceKit.WebHost.Endpoints.Support
{
    /// <summary>An index page HTTP handler.</summary>
	public class IndexPageHttpHandler
		: INServiceKitHttpHandler, IHttpHandler
	{

		/// <summary>
		/// Non ASP.NET requests
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="operationName"></param>
		public void ProcessRequest(IHttpRequest request, IHttpResponse response, string operationName)
		{
			var defaultUrl = EndpointHost.Config.ServiceEndpointsMetadataConfig.DefaultMetadataUri;

			if (request.PathInfo == "/")
			{
				var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
				var absoluteUrl = request.RawUrl.TrimEnd('/') + relativeUrl;
				response.StatusCode = (int) HttpStatusCode.Redirect;
				response.AddHeader(HttpHeaders.Location, absoluteUrl);
			}
			else
			{
				response.StatusCode = (int)HttpStatusCode.Redirect;
				response.AddHeader(HttpHeaders.Location, defaultUrl);
			}
		}

        /// <summary>
        /// ASP.NET requests
        /// </summary>
        /// <param name="context"></param>
		public void ProcessRequest(HttpContext context)
		{
			var defaultUrl = EndpointHost.Config.ServiceEndpointsMetadataConfig.DefaultMetadataUri;

			if (context.Request.PathInfo == "/"
				|| context.Request.FilePath.EndsWith("/"))
			{
				//new NotFoundHttpHandler().ProcessRequest(context); return;
				
				var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
				var absoluteUrl = context.Request.Url.AbsoluteUri.TrimEnd('/') + relativeUrl;
				context.Response.Redirect(absoluteUrl);
			}
			else
			{
				context.Response.Redirect(defaultUrl);
			}

		}

        /// <summary>Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.</summary>
        ///
        /// <value>true if the <see cref="T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.</value>
		public bool IsReusable
		{
			get { return true; }
		}
	}
}