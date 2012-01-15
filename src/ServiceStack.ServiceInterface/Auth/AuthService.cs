using System;
using System.Configuration;
using System.Linq;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Auth
{
	/// <summary>
	/// Inject logic into existing services by introspecting the request and injecting your own
	/// validation logic. Exceptions thrown will have the same behaviour as if the service threw it.
	/// 
	/// If a non-null object is returned the request will short-circuit and return that response.
	/// </summary>
	/// <param name="service">The instance of the service</param>
	/// <param name="httpMethod">GET,POST,PUT,DELETE</param>
	/// <param name="requestDto"></param>
	/// <returns>Response DTO; non-null will short-circuit execution and return that response</returns>
	public delegate object ValidateFn(IServiceBase service, string httpMethod, object requestDto);

	public class Auth
	{
		public string provider { get; set; }
		public string State { get; set; }
		public string oauth_token { get; set; }
		public string oauth_verifier { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public bool? RememberMe { get; set; }
	}

	public class AuthResponse
	{
		public AuthResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		public string SessionId { get; set; }

		public string UserName { get; set; }

		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AuthService : RestServiceBase<Auth>
	{
		public const string BasicProvider = "basic";
		public const string CredentialsProvider = "credentials";
		public const string LogoutAction = "logout";

		public static Func<IAuthSession> CurrentSessionFactory { get; private set; }
		public static ValidateFn ValidateFn { get; set; }

		public static string DefaultOAuthProvider { get; private set; }
		public static string DefaultOAuthRealm { get; private set; }
		public static AuthConfig[] AuthConfigs { get; private set; }
		
		public static AuthConfig GetAuthConfig(string provider)
		{
			foreach (var authConfig in AuthConfigs)
			{
				if (string.Compare(authConfig.Provider, provider,
					StringComparison.InvariantCultureIgnoreCase) == 0)
					return authConfig;
			}

			return null;
		}

		public static string GetSessionKey(string sessionId)
		{
			return IdUtils.CreateUrn<IAuthSession>(sessionId);
		}

		public static void Init(IAppHost appHost, Func<IAuthSession> sessionFactory, params AuthConfig[] authConfigs)
		{
			if (authConfigs.Length == 0)
				throw new ArgumentNullException("authConfigs");

			DefaultOAuthProvider = authConfigs[0].Provider;
			DefaultOAuthRealm = authConfigs[0].AuthRealm;

			AuthConfigs = authConfigs;
			CurrentSessionFactory = sessionFactory;
			appHost.RegisterService<AuthService>();

			SessionFeature.Init(appHost);
		}

		private void AssertAuthProviders()
		{
			if (AuthConfigs == null || AuthConfigs.Length == 0)
				throw new ConfigurationException("No OAuth providers have been registered in your AppHost.");
		}

		public override object OnGet(Auth request)
		{
			return OnPost(request);
		}

        public override object OnPost(Auth request)
        {
			AssertAuthProviders();

			if (ValidateFn != null)
			{
				var response = ValidateFn(this, HttpMethods.Get, request);
				if (response != null) return response;
			}

			var opt = request.RememberMe.GetValueOrDefault(false)
				? SessionOptions.Permanent
				: SessionOptions.Temporary;

			base.RequestContext.Get<IHttpResponse>()
				.AddSessionOptions(base.RequestContext.Get<IHttpRequest>(), opt);

			var provider = request.provider ?? AuthConfigs[0].Provider;
			var oAuthConfig = GetAuthConfig(provider);
			if (oAuthConfig == null)
				throw HttpError.NotFound("No configuration was added for OAuth provider '{0}'".Fmt(provider));

			if (request.provider == LogoutAction)
				return oAuthConfig.Logout(this, request);

			var session = this.GetSession();
			if (!oAuthConfig.IsAuthorized(session, session.GetOAuthTokens(provider)))
			{
				return oAuthConfig.Authenticate(this, session, request);
			}

			//Already Authenticated
			return this.Redirect(session.ReferrerUrl.AddHashParam("s", "0"));
		}

        public override object OnDelete(Auth request)
        {
            if (ValidateFn != null)
            {
                var response = ValidateFn(this, HttpMethods.Delete, request);
                if (response != null) return response;
            }

            this.RemoveSession();

            return new AuthResponse();
        }
	}

}
