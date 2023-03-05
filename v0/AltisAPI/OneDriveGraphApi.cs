using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using AltApi.Entities;
using AltApi.Helpers;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Web;
using System.Net.Http.Headers;
using AltisAPI.Entities;

namespace AltApi
{
    /// <summary>
    /// API for both OneDrive Personal and OneDrive for Business on Office 365 through the Microsoft Graph API
    /// Create your own Client ID / Client Secret at https://apps.dev.microsoft.com
    /// </summary>
    public class OneDriveGraphApi 
    {
        #region Properties

        /// <summary>
        /// The oAuth 2.0 Application Client ID
        /// </summary>
        public string ClientId { get; protected set; }

        /// <summary>
        /// The oAuth 2.0 Application Client Secret
        /// </summary>
        public string ClientSecret { get; protected set; }

        /// <summary>
        /// If provided, this proxy will be used for communication with the OneDrive API. If not provided, no proxy will be used.
        /// </summary>
        public IWebProxy ProxyConfiguration { get; set; }

        /// <summary>
        /// If provided along with a proxy configuration, these credentials will be used to authenticate to the proxy. If omitted, the default system credentials will be used.
        /// </summary>
        public NetworkCredential ProxyCredential { get; set; }

        /// <summary>
        /// Authorization token used for requesting tokens
        /// </summary>
        public string AuthorizationToken { get; private set; }

        /// <summary>
        /// Access Token for communicating with OneDrive
        /// </summary>
        public OneDriveAccessToken AccessToken { get; protected set; }

        /// <summary>
        /// Date and time until which the access token should be valid based on the information provided by the oAuth provider
        /// </summary>
        public DateTime? AccessTokenValidUntil { get; protected set; }

        /// <summary>
        /// Base URL of the OneDrive API
        /// </summary>
        protected string OneDriveApiBaseUrl { get; set; }

        public OneDriveUserProfile oneDriveUserProfile { get; set; }



        #endregion
        #region Constants

        /// <summary>
        /// The url to provide as the redirect URL after successful authentication
        /// </summary>
        public string AuthenticationRedirectUrl { get; set; } = "https://localhost:44326/home/about";//"https://login.microsoftonline.com/common/oauth2/nativeclient";


        /// <summary>
        /// String formatted Uri that needs to be called to authenticate to the Graph API
        /// </summary>
        protected  string AuthenticateUri => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={0}&response_type=code&redirect_uri={1}&response_mode=query&scope=offline_access%20files.readwrite.all";

        /// <summary>
        /// String formatted Uri that can be called to sign out from the Graph API
        /// </summary>
        public  string SignoutUri => "https://login.microsoftonline.com/common/oauth2/v2.0/logout";

        /// <summary>
        /// The url where an access token can be obtained
        /// </summary>
        protected  string AccessTokenUri => "https://login.microsoftonline.com/common/oauth2/v2.0/token";

        internal void SetAuthorizationToken(string code)
        {
            AuthorizationToken = code;
        }

        /// <summary>
        /// Defines the maximum allowed file size that can be used for basic uploads. Should be set 4 MB as described in the API documentation at https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/item_uploadcontent
        /// </summary>
        public new static long MaximumBasicFileUploadSizeInBytes = 4 * 1024;

        /// <summary>
        /// Size of the chunks to upload when using the resumable upload method. Must be a multiple of 327680 bytes. See https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/api/driveitem_createuploadsession#best-practices
        /// </summary>
        public new long ResumableUploadChunkSizeInBytes = 10485760;

        /// <summary>
        /// The default scopes to request access to at the Graph API
        /// </summary>
        public string[] DefaultScopes => new[] { "offline_access", "files.readwrite.all" };

        /// <summary>
        /// Base URL of the Graph API
        /// </summary>
        protected string GraphApiBaseUrl => "https://graph.microsoft.com/v1.0/";

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of the Graph API
        /// </summary>
        /// <param name="applicationId">Microsoft Application ID to use to connect</param>
        /// <param name="clientSecret">Microsoft Application secret to use to connect</param>
        public OneDriveGraphApi(string applicationId,string redirecturl, string clientSecret = null) 
        {

            ClientId = applicationId;
            ClientSecret = clientSecret;// "voL8Q~H4r15z55cjbUsX7LXe.ucn5m4Sa5Rpcaqt";
            AuthenticationRedirectUrl = redirecturl;
            OneDriveApiBaseUrl = GraphApiBaseUrl + "me/";
        }

        #endregion

        #region Public Methods - Authentication

        /// <summary>
        /// Returns the Uri that needs to be called to authenticate to the OneDrive for Business API
        /// </summary>
        /// <returns>Uri that needs to be called in a browser to authenticate to the OneDrive for Business API</returns>
        public Uri GetAuthenticationUri()
        {
            var uri = string.Format(AuthenticateUri, ClientId, AuthenticationRedirectUrl);
            return new Uri(uri);
        }

        /// <summary>
        /// Returns the Uri that needs to be called to authenticate to the OneDrive for Business API
        /// </summary>
        /// <returns>Uri that needs to be called in a browser to authenticate to the OneDrive for Business API</returns>
        public  Uri GetAuthenticationUri(string url)
        {
            var uri = string.Format(AuthenticateUri, ClientId, url ?? AuthenticationRedirectUrl);
            return new Uri(uri);
        }

        /// <summary>
        /// Gets an access token from the provided authorization token using the default scopes defined in DefaultScopes
        /// </summary>
        /// <param name="authorizationToken">Authorization token</param>
        /// <returns>Access token for the Graph API</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected  async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken)
        {
            return await GetAccessTokenFromAuthorizationToken(authorizationToken, DefaultScopes);
        }

        /// <summary>
        /// Gets an access token from the provided authorization token
        /// </summary>
        /// <param name="authorizationToken">Authorization token</param>
        /// <param name="scopes">Scopes to request access for</param>
        /// <returns>Access token for the Graph API</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken, string[] scopes)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("scope", scopes.Aggregate((x, y) => $"{x} {y}"));
            queryBuilder.Add("code", authorizationToken);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("grant_type", "authorization_code");
            if (ClientSecret != null)
                queryBuilder.Add("client_secret", ClientSecret);
            return await PostToTokenEndPoint(queryBuilder);
        }

        /// <summary>
        /// Gets an access token from the provided refresh token using the default scopes defined in DefaultScopes
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Access token for the Graph API</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected  async Task<OneDriveAccessToken> GetAccessTokenFromRefreshToken(string refreshToken)
        {
            return await GetAccessTokenFromRefreshToken(refreshToken, DefaultScopes);
        }

        /// <summary>
        /// Gets an access token from the provided refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="scopes">Scopes to request access for</param>
        /// <returns>Access token for the Graph API</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected async Task<OneDriveAccessToken> GetAccessTokenFromRefreshToken(string refreshToken, string[] scopes)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("scope", scopes.Aggregate((x, y) => $"{x} {y}"));
            queryBuilder.Add("refresh_token", refreshToken);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("grant_type", "refresh_token");
            if (ClientSecret != null)
                queryBuilder.Add("client_secret", ClientSecret);
            return await PostToTokenEndPoint(queryBuilder);
        }

        /// <summary>
        /// Gets the AppFolder root its metadata
        /// </summary>
        /// <returns>OneDriveItem object with the information about the current App Registration its AppFolder</returns>
        public async Task<OneDriveUserProfile> GetProfil()
        {
            var completeUrl = string.Concat(OneDriveApiBaseUrl, "");

            var result = await SendMessageReturnOneDriveItem<OneDriveUserProfile>(string.Empty, HttpMethod.Get, completeUrl, HttpStatusCode.OK);
            oneDriveUserProfile = result;
            return result;
        }

        #endregion


        #region Public Methods - Authentication

        /// <summary>
        /// Instantiates a new HttpClient preconfigured for use. Note that the caller is responsible for disposing this object.
        /// </summary>
        /// <param name="bearerToken">Bearer token to add to the HTTP Client for authorization (optional)</param>
        /// <returns>HttpClient instance</returns>
        protected HttpClient CreateHttpClient(string bearerToken = null)
        {
            // Define the HttpClient settings
            var httpClientHandler = new HttpClientHandler
            {
                UseDefaultCredentials = ProxyCredential == null,
                UseProxy = ProxyConfiguration != null,
                Proxy = ProxyConfiguration
            };

            // Check if we need specific credentials for the proxy
            if (ProxyCredential != null && httpClientHandler.Proxy != null)
            {
                httpClientHandler.Proxy.Credentials = ProxyCredential;
            }

            // Create the new HTTP Client
            var httpClient = new HttpClient(httpClientHandler);

            if (!string.IsNullOrEmpty(bearerToken))
            {
                // Provide the access token through a bearer authorization header
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", bearerToken);
            }

            return httpClient;
        }

        
        /// <summary>
        /// Returns the authorization token from the provided URL to which the OneDrive API authentication request was sent after succesful authentication
        /// </summary>
        /// <param name="url">Url received from the OneDrive API after succesful authentication</param>
        /// <returns>Authorization token or NULL if unable to identify from provided URL</returns>
        public string GetAuthorizationTokenFromUrl(string url)
        {
            // Url must be provided
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            // Url must start with the return url followed by a question mark to provide querystring parameters
            if (!url.StartsWith(string.Concat(AuthenticationRedirectUrl, "?")) && !url.StartsWith(string.Concat(AuthenticationRedirectUrl, "/?")))
            {
                return null;
            }

            // Get the querystring parameters from the URL
            var queryString = url.Remove(0, AuthenticationRedirectUrl.Length + 1);
            var queryStringParams = HttpUtility.ParseQueryString(queryString);

            AuthorizationToken = queryStringParams["code"];
            return AuthorizationToken;
        }

        /// <summary>
        /// Tries to retrieve an access token based on the tokens already available in this OneDrive instance
        /// </summary>
        /// <returns>OneDrive access token or NULL if unable to get an access token</returns>
        public async Task<OneDriveAccessToken> GetAccessToken()
        {
            // Check if we have an access token
            if (AccessToken != null)
            {
                // We have an access token, check if its still valid
                if (AccessTokenValidUntil.HasValue && AccessTokenValidUntil.Value > DateTime.Now)
                {
                    // Access token is still valid, use it
                    return AccessToken;
                }

                // Access token is no longer valid, check if we have a refresh token to request a new access token
                if (!string.IsNullOrEmpty(AccessToken.RefreshToken))
                {
                    // We have a refresh token, request a new access token using it
                    AccessToken = await GetAccessTokenFromRefreshToken(AccessToken.RefreshToken);
                    return AccessToken;
                }
            }

            // No access token is available, check if we have an authorization token
            if (string.IsNullOrEmpty(AuthorizationToken))
            {
                // No access token, no authorization token, we need to authorize first which can't be done without an UI
                return null;
            }

            // No access token but we have an authorization token, request the access token
            AccessToken = await GetAccessTokenFromAuthorizationToken(AuthorizationToken);
            AccessTokenValidUntil = DateTime.Now.AddSeconds(AccessToken.AccessTokenExpirationDuration);
            return AccessToken;
        }

        /// <summary>
        /// Returns the Uri that needs to be called to sign the current user out of the OneDrive API
        /// </summary>
        /// <returns>Uri that needs to be called to sign the current user out of the OneDrive API</returns>
        public Uri GetSignOutUri()
        {
            return new Uri(string.Format(SignoutUri, ClientId));
        }

        /// <summary>
        /// Sends a HTTP POST to the OneDrive Token EndPoint
        /// </summary>
        /// <param name="queryBuilder">The querystring parameters to send in the POST body</param>
        /// <returns>Access token for OneDrive or NULL if unable to retrieve an access token</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected async Task<OneDriveAccessToken> PostToTokenEndPoint(QueryStringBuilder queryBuilder)
        {
            if (string.IsNullOrEmpty(AccessTokenUri))
            {
                throw new InvalidOperationException("AccessTokenUri has not been set");
            }

            // Create an HTTPClient instance to communicate with the REST API of OneDrive
            using (var client = CreateHttpClient())
            {
                // Load the content to upload
                using (var content = new StringContent(queryBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"))
                {
                    // Construct the message towards the webservice
                    using (var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenUri))
                    {
                        // Set the content to send along in the message body with the request
                        request.Content = content;

                        // Request the response from the webservice
                        var response = await client.SendAsync(request);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        var options = new JsonSerializerOptions();
                        options.Converters.Add(new JsonStringEnumConverter());

                        // Verify if the request was successful (response status 200-299)
                        if (response.IsSuccessStatusCode)
                        {
                            // Successfully retrieved token, parse it from the response
                            var appTokenResult = JsonSerializer.Deserialize<OneDriveAccessToken>(responseBody, options);
                            return appTokenResult;
                        }

                        // Not able to retrieve a token, parse the error and throw it as an exception
                        OneDriveError errorResult;
                        try
                        {
                            // Try to parse the response as a OneDrive API error message
                            errorResult = JsonSerializer.Deserialize<OneDriveError>(responseBody, options);
                        }
                        catch (Exception ex)
                        {
                            throw new Exceptions.TokenRetrievalFailedException(innerException: ex);
                        }

                        throw new Exceptions.TokenRetrievalFailedException(message: errorResult.ErrorDescription, errorDetails: errorResult);
                    }
                }
            }
        }

        /// <summary>
        /// Authenticates to OneDrive using the provided Refresh Token
        /// </summary>
        /// <param name="refreshToken">Refreshtoken to use to authenticate to OneDrive</param>
        public async Task AuthenticateUsingRefreshToken(string refreshToken)
        {
            AccessToken = await GetAccessTokenFromRefreshToken(refreshToken);
            AccessTokenValidUntil = DateTime.Now.AddSeconds(AccessToken.AccessTokenExpirationDuration);
        }

        #endregion


        #region Sending

        /// <summary>
        /// Sends a message to the OneDrive webservice and returns the HttpResponse instance
        /// </summary>
        /// <param name="bodyText">String with the message to send to the webservice</param>
        /// <param name="httpMethod">HttpMethod to use to send with the webservice (i.e. POST, GET, PUT, etc.)</param>
        /// <param name="url">Url of the OneDrive webservice to send the message to</param>
        /// <param name="preferRespondAsync">Provide true if the Prefer Async header should be sent along with the request. This is required for some requests. Optional, default = false = do not send the async header.</param>
        /// <returns>HttpResponse of the webservice call. Note that the caller needs to dispose the returned instance.</returns>
        protected virtual async Task<HttpResponseMessage> SendMessageReturnHttpResponse(string bodyText, HttpMethod httpMethod, string url, bool preferRespondAsync = false)
        {
            // Get an access token to perform the request to OneDrive
            var accessToken = await GetAccessToken();

            // Create an HTTPClient instance to communicate with the REST API of OneDrive
            using (var client = CreateHttpClient(accessToken.AccessToken))
            {
                // Load the content to upload
                using (var content = new StringContent(bodyText ?? "", Encoding.UTF8, "application/json"))
                {
                    // Construct the message towards the webservice
                    using (var request = new HttpRequestMessage(httpMethod, url))
                    {
                        if (preferRespondAsync)
                        {
                            // Add a header to prefer the operation to happen while we continue processing our code
                            request.Headers.Add("Prefer", "respond-async");
                        }

                        // Check if a body to send along with the request has been provided
                        if (!string.IsNullOrEmpty(bodyText) && httpMethod != HttpMethod.Get)
                        {
                            // Set the content to send along in the message body with the request
                            request.Content = content;
                        }

                        // Request the response from the webservice
                        var response = await client.SendAsync(request);
                        return response;
                    }
                }
            }
        }


        /// <summary>
        /// Sends a message to the OneDrive webservice and returns a string with the response
        /// </summary>
        /// <param name="bodyText">String with the message to send to the webservice</param>
        /// <param name="httpMethod">HttpMethod to use to send with the webservice (i.e. POST, GET, PUT, etc.)</param>
        /// <param name="url">Url of the OneDrive webservice to send the message to</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be NULL to indicate failure.</param>
        /// <returns>String containing the response of the webservice</returns>
        protected virtual async Task<string> SendMessageReturnString(string bodyText, HttpMethod httpMethod, string url, HttpStatusCode? expectedHttpStatusCode = null)
        {
            using (var response = await SendMessageReturnHttpResponse(bodyText, httpMethod, url))
            {
                if (!expectedHttpStatusCode.HasValue || (expectedHttpStatusCode.HasValue && response != null && response.StatusCode == expectedHttpStatusCode.Value))
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    return responseString;
                }
                return null;
            }
        }

        /// <summary>
        /// Sends a message to the OneDrive webservice and returns a OneDriveBaseItem with the response
        /// </summary>
        /// <typeparam name="T">OneDriveBaseItem type of the expected response</typeparam>
        /// <param name="bodyText">String with the message to send to the webservice</param>
        /// <param name="httpMethod">HttpMethod to use to send with the webservice (i.e. POST, GET, PUT, etc.)</param>
        /// <param name="url">Url of the OneDrive webservice to send the message to</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be NULL to indicate failure.</param>
        /// <returns>Typed OneDrive entity with the result from the webservice</returns>
        protected virtual async Task<T> SendMessageReturnOneDriveItem<T>(string bodyText, HttpMethod httpMethod, string url, HttpStatusCode? expectedHttpStatusCode = null) where T : OneDriveItemBase
        {
            var responseString = await SendMessageReturnString(bodyText, httpMethod, url, expectedHttpStatusCode);

            // Validate output was generated
            if (string.IsNullOrEmpty(responseString)) return null;

            // Convert the JSON result to its appropriate type
            try
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());

                var responseOneDriveItem = JsonSerializer.Deserialize<T>(responseString, options);
                responseOneDriveItem.OriginalJson = responseString;

                return responseOneDriveItem;
            }
            catch (JsonException e)
            {
                throw new Exceptions.InvalidResponseException(responseString, e);
            }
        }

        #endregion
    }
}
