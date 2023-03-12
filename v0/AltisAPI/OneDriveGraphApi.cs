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
using AltApi.Entities;
using AltApi.Enums;

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
        public string AuthorizationToken { get;  set; }

        /// <summary>
        /// Access Token for communicating with OneDrive
        /// </summary>
        public OneDriveAccessToken AccessToken { get;  set; }

        /// <summary>
        /// Date and time until which the access token should be valid based on the information provided by the oAuth provider
        /// </summary>
        public DateTime? AccessTokenValidUntil { get;  set; }

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

        #region Events

        /// <summary>
        /// Event triggered when uploading a file using UploadFileViaResumableUpload to indicate the progress of the upload process
        /// </summary>
        public event EventHandler<OneDriveUploadProgressChangedEventArgs> UploadProgressChanged;

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

        /// <summary>
        /// Retrieves the OneDrive root folder
        /// </summary>
        public virtual async Task<OneDriveItem> GetDriveRoot()
        {
            return await GetData<OneDriveItem>("drive/root");
        }


        protected virtual async Task<T> GetData<T>(string url) where T : OneDriveItemBase
        {
            // Construct the complete URL to call
            var completeUrl = ConstructCompleteUrl(url);

            // Call the OneDrive webservice
            var result = await SendMessageReturnOneDriveItem<T>("", HttpMethod.Get, completeUrl, HttpStatusCode.OK);
            return result;
        }
        /// <summary>
        /// Retrieves the OneDrive Item from the provided drive by it's unique identifier
        /// </summary>
        /// <param name="id">Unique identifier of the OneDrive item to retrieve</param>
        /// <param name="driveId">Id of the drive on which the item resides</param>
        /// <returns>OneDriveItem representing the file or NULL if the file was not found</returns>
        public virtual async Task<OneDriveItem> GetItemFromDriveById(string id, string driveId)
        {
            return await GetData<OneDriveItem>(string.Concat("drives/", driveId, "/items/", id));
        }
        /// <summary>
        /// Initiates a resumable upload session to OneDrive. It doesn't perform the actual upload yet.
        /// </summary>
        /// <param name="fileName">Filename to store the uploaded content under</param>
        /// <param name="oneDriveFolder">OneDriveItem container in which the file should be uploaded</param>
        /// <returns>OneDriveUploadSession instance containing the details where to upload the content to</returns>
        protected virtual async Task<OneDriveUploadSession> CreateResumableUploadSession(string fileName, OneDriveItem oneDriveFolder)
        {
            // Construct the complete URL to call
            string completeUrl;
            if (oneDriveFolder.RemoteItem != null)
            {
                // Item will be uploaded to another drive
                completeUrl = string.Concat("drives/", oneDriveFolder.RemoteItem.ParentReference.DriveId, "/items/", oneDriveFolder.RemoteItem.Id, ":/", fileName, ":/upload.createSession");
            }
            else if (oneDriveFolder.ParentReference != null && !string.IsNullOrEmpty(oneDriveFolder.ParentReference.DriveId))
            {
                // Item will be uploaded to another drive
                completeUrl = string.Concat("drives/", oneDriveFolder.ParentReference.DriveId, "/items/", oneDriveFolder.Id, ":/", fileName, ":/upload.createSession");
            }
            else if (!string.IsNullOrEmpty(oneDriveFolder.WebUrl) && oneDriveFolder.WebUrl.Contains("cid="))
            {
                // Item will be uploaded to another drive. Used by OneDrive Personal when using a shared item.
                completeUrl = string.Concat("drives/", oneDriveFolder.WebUrl.Remove(0, oneDriveFolder.WebUrl.IndexOf("cid=") + 4), "/items/", oneDriveFolder.Id, ":/", fileName, ":/upload.createSession");
            }
            else
            {
                // Item will be uploaded to the current user its drive
                completeUrl = string.Concat("drive/items/", oneDriveFolder.Id, ":/", fileName, ":/upload.createSession");
            }

            completeUrl = ConstructCompleteUrl(completeUrl);

            // Construct the OneDriveUploadSessionItemContainer entity with the upload details
            // Add the conflictbehavior header to always overwrite the file if it already exists on OneDrive
            var uploadItemContainer = new OneDriveUploadSessionItemContainer
            {
                Item = new OneDriveUploadSessionItem
                {
                    FilenameConflictBehavior = NameConflictBehavior.Replace
                }
            };

            // Call the OneDrive webservice
            var result = await SendMessageReturnOneDriveItem<OneDriveUploadSession>(uploadItemContainer, HttpMethod.Post, completeUrl, HttpStatusCode.OK);
            return result;
        }

        /// <summary>
        /// Sends a message to the OneDrive webservice and returns a OneDriveBaseItem with the response
        /// </summary>
        /// <typeparam name="T">OneDriveBaseItem type of the expected response</typeparam>
        /// <param name="oneDriveItem">OneDriveBaseItem of the message to send to the webservice</param>
        /// <param name="httpMethod">HttpMethod to use to send with the webservice (i.e. POST, GET, PUT, etc.)</param>
        /// <param name="url">Url of the OneDrive webservice to send the message to</param>
        /// <param name="expectedHttpStatusCode">The expected Http result status code. Optional. If provided and the webservice returns a different response, the return type will be NULL to indicate failure.</param>
        /// <returns>Typed OneDrive entity with the result from the webservice</returns>
        protected virtual async Task<T> SendMessageReturnOneDriveItem<T>(OneDriveItemBase oneDriveItem, HttpMethod httpMethod, string url, HttpStatusCode? expectedHttpStatusCode = null) where T : OneDriveItemBase
        {
            var bodyText = oneDriveItem != null ? JsonSerializer.Serialize(oneDriveItem) : null;

            return await SendMessageReturnOneDriveItem<T>(bodyText, httpMethod, url, expectedHttpStatusCode);
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
                        try
                        {

                       
                        // Set the content to send along in the message body with the request
                        request.Content = content;

                        // Request the response from the webservice
                        var response = await client.SendAsync(request).ConfigureAwait(false);
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
                        catch (Exception ex)
                        {

                            throw;
                        }
                       

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
        protected virtual string ConstructCompleteUrl(string commandUrl)
        {
            // Check if the commandUrl is already a full URL, if so leave it as is. If not, prepend it with the Api Base URL.
            return commandUrl.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) ? commandUrl : string.Concat(OneDriveApiBaseUrl, commandUrl);
        }


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
        /// Uploads the provided file to OneDrive keeping the original filename
        /// </summary>
        /// <param name="filePath">Full path to the file to upload</param>
        /// <param name="parentFolder">OneDriveItem of the folder to which the file should be uploaded</param>
        /// <returns>OneDriveItem representing the uploaded file when successful or NULL when the upload failed</returns>
        public virtual async Task<OneDriveItem> UploadFile(string filePath, OneDriveItem parentFolder)
        {
            return await UploadFileAs(filePath, null, parentFolder);
        }
        /// <summary>
        /// Uploads the provided file to OneDrive using the provided filename
        /// </summary>
        /// <param name="filePath">Full path to the file to upload</param>
        /// <param name="fileName">Filename to assign to the file on OneDrive</param>
        /// <param name="parentFolder">OneDriveItem of the folder to which the file should be uploaded</param>
        /// <returns>OneDriveItem representing the uploaded file when successful or NULL when the upload failed</returns>
        public virtual async Task<OneDriveItem> UploadFileAs(string filePath, string fileName, OneDriveItem parentFolder)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Provided file could not be found", nameof(filePath));
            }

            // Get a reference to the file to upload
            var fileToUpload = new FileInfo(filePath);

            // If no filename has been provided, use the same filename as the original file has
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = fileToUpload.Name;
            }

            // Verify if the filename does not contain any for OneDrive illegal characters
            //if (!ValidFilename(fileName))
            //{
            //    throw new ArgumentException("Provided file contains illegal characters in its filename", nameof(filePath));
            //}

            // Verify which upload method should be used
            if (fileToUpload.Length <= MaximumBasicFileUploadSizeInBytes)
            {
                // Use the basic upload method                
                return await UploadFileViaSimpleUpload(fileToUpload, fileName, parentFolder);
            }

            // Use the resumable upload method
            return await UploadFileViaResumableUpload(fileToUpload, fileName, parentFolder, null);
        }

        /// <summary>
        /// Uploads a file to OneDrive using the resumable file upload method
        /// </summary>
        /// <param name="file">FileInfo instance pointing to the file to upload</param>
        /// <param name="fileName">The filename under which the file should be stored on OneDrive</param>
        /// <param name="parentFolder">OneDrive item representing the folder to which the file should be uploaded</param>
        /// <param name="fragmentSizeInBytes">Size in bytes of the fragments to use for uploading. Higher numbers are faster but require more stable connections, lower numbers are slower but work better with unstable connections. Provide NULL to use the default.</param>
        /// <returns>OneDriveItem instance representing the uploaded item</returns>
        public virtual async Task<OneDriveItem> UploadFileViaResumableUpload(FileInfo file, string fileName, OneDriveItem parentFolder, long? fragmentSizeInBytes)
        {
            // Open the source file for reading
            using (var fileStream = file.OpenRead())
            {
                return await UploadFileViaResumableUpload(fileStream, fileName, parentFolder, fragmentSizeInBytes);
            }
        }

        /// <summary>
        /// Uploads a file to OneDrive using the resumable file upload method
        /// </summary>
        /// <param name="fileStream">Stream pointing to the file to upload</param>
        /// <param name="fileName">The filename under which the file should be stored on OneDrive</param>
        /// <param name="parentFolder">OneDrive item representing the folder to which the file should be uploaded</param>
        /// <param name="fragmentSizeInBytes">Size in bytes of the fragments to use for uploading. Higher numbers are faster but require more stable connections, lower numbers are slower but work better with unstable connections</param>
        /// <returns>OneDriveItem instance representing the uploaded item</returns>
        public virtual async Task<OneDriveItem> UploadFileViaResumableUpload(Stream fileStream, string fileName, OneDriveItem parentFolder, long? fragmentSizeInBytes)
        {
            var oneDriveUploadSession = await CreateResumableUploadSession(fileName, parentFolder);
            return await UploadFileViaResumableUploadInternal(fileStream, oneDriveUploadSession, fragmentSizeInBytes);
        }

        /// <summary>
        /// Uploads a file to OneDrive using the resumable file upload method
        /// </summary>
        /// <param name="fileStream">Stream pointing to the file to upload</param>
        /// <param name="oneDriveUploadSession">Upload session under which the upload will be performed</param>
        /// <param name="fragmentSizeInBytes">Size in bytes of the fragments to use for uploading. Higher numbers are faster but require more stable connections, lower numbers are slower but work better with unstable connections.</param>
        /// <returns>OneDriveItem instance representing the uploaded item</returns>
        protected virtual async Task<OneDriveItem> UploadFileViaResumableUploadInternal(Stream fileStream, OneDriveUploadSession oneDriveUploadSession, long? fragmentSizeInBytes)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException("fileStream");
            }
            if (oneDriveUploadSession == null)
            {
                throw new ArgumentNullException("oneDriveUploadSession");
            }

            // Get an access token to perform the request to OneDrive
            var accessToken = await GetAccessToken();

            // Amount of bytes succesfully sent
            long totalBytesSent = 0;

            // Used for retrying failed transmissions
            var transferAttemptCount = 0;
            const int transferMaxAttempts = 3;

            do
            {
                // Keep a counter how many times it has been attempted to send this file
                transferAttemptCount++;

                // Start sending the file from the first byte
                long currentPosition = 0;

                // Defines a buffer which will be filled with bytes from the original file and then sent off to the OneDrive webservice
                var fragmentBuffer = new byte[fragmentSizeInBytes ?? ResumableUploadChunkSizeInBytes];

                // Create an HTTPClient instance to communicate with the REST API of OneDrive to perform the upload 
                using (var client = CreateHttpClient(accessToken.AccessToken))
                {
                    // Keep looping through the source file length until we've sent all bytes to the OneDrive webservice
                    while (currentPosition < fileStream.Length)
                    {
                        var fragmentSuccessful = true;

                        // Define the end position in the file bytes based on the buffer size we're using to send fragments of the file to OneDrive
                        var endPosition = currentPosition + fragmentBuffer.LongLength;

                        // Make sure our end position isn't further than the file size in which case it would be the last fragment of the file to be sent
                        if (endPosition > fileStream.Length) endPosition = fileStream.Length;

                        // Define how many bytes should be read from the source file
                        var amountOfBytesToSend = (int)(endPosition - currentPosition);

                        // Copy the bytes from the source file into the buffer
                        await fileStream.ReadAsync(fragmentBuffer, 0, amountOfBytesToSend);

                        // Load the content to upload
                        using (var content = new ByteArrayContent(fragmentBuffer, 0, amountOfBytesToSend))
                        {
                            // Indicate that we're sending binary data
                            content.Headers.Add("Content-Type", "application/octet-stream");

                            // Provide information to OneDrive which range of bytes we're going to send and the total amount of bytes the file exists out of
                            content.Headers.Add("Content-Range", string.Concat("bytes ", currentPosition, "-", endPosition - 1, "/", fileStream.Length));

                            // Construct the PUT message towards the webservice containing the binary data
                            using (var request = new HttpRequestMessage(HttpMethod.Put, oneDriveUploadSession.UploadUrl))
                            {
                                // Set the binary content to upload
                                request.Content = content;

                                // Send the data to the webservice
                                using (var response = await client.SendAsync(request))
                                {
                                    // Check the response code
                                    switch (response.StatusCode)
                                    {
                                        // Fragment has been received, awaiting next fragment
                                        case HttpStatusCode.Accepted:
                                            // Move the current position pointer to the end of the fragment we've just sent so we continue from there with the next upload
                                            currentPosition = endPosition;
                                            totalBytesSent += amountOfBytesToSend;

                                            // Trigger event
                                            UploadProgressChanged?.Invoke(this, new OneDriveUploadProgressChangedEventArgs(totalBytesSent, fileStream.Length));
                                            break;

                                        // All fragments have been received, the file did already exist and has been overwritten
                                        case HttpStatusCode.OK:
                                        // All fragments have been received, the file has been created
                                        case HttpStatusCode.Created:
                                            // Read the response as a string
                                            var responseString = await response.Content.ReadAsStringAsync();

                                            // Convert the JSON result to its appropriate type
                                            try
                                            {
                                                var options = new JsonSerializerOptions();
                                                options.Converters.Add(new JsonStringEnumConverter());

                                                var responseOneDriveItem = JsonSerializer.Deserialize<OneDriveItem>(responseString, options);
                                                responseOneDriveItem.OriginalJson = responseString;

                                                return responseOneDriveItem;
                                            }
                                            catch (JsonException e)
                                            {
                                                throw new Exceptions.InvalidResponseException(responseString, e);
                                            }

                                        // All other status codes are considered to indicate a failed fragment transmission and will be retried
                                        default:
                                            fragmentSuccessful = false;
                                            break;
                                    }
                                }
                            }
                        }

                        // Check if the fragment was successful, if not, retry the complete upload
                        if (!fragmentSuccessful)
                            break;
                    }
                }
            } while (transferAttemptCount < transferMaxAttempts);

            // Request failed
            return null;
        }
        /// <summary>
        /// Performs a file upload to OneDrive using the simple OneDrive API. Best for small files on reliable network connections.
        /// </summary>
        /// <param name="file">File reference to the file to upload</param>
        /// <param name="fileName">The filename under which the file should be stored on OneDrive</param>
        /// <param name="oneDriveItem">OneDriveItem of the folder to which the file should be uploaded</param>
        /// <returns>The resulting OneDrive item representing the uploaded file</returns>
        public async Task<OneDriveItem> UploadFileViaSimpleUpload(FileInfo file, string fileName, OneDriveItem oneDriveItem)
        {
            // Read the file to upload
            using (var fileStream = file.OpenRead())
            {
                return await UploadFileViaSimpleUpload(fileStream, fileName, oneDriveItem);
            }
        }
        /// <summary>
        /// Performs a file upload to OneDrive using the simple OneDrive API. Best for small files on reliable network connections.
        /// </summary>
        /// <param name="fileStream">Stream to the file to upload</param>
        /// <param name="fileName">The filename under which the file should be stored on OneDrive</param>
        /// <param name="parentFolder">OneDriveItem of the folder to which the file should be uploaded</param>
        /// <returns>The resulting OneDrive item representing the uploaded file</returns>
        public async Task<OneDriveItem> UploadFileViaSimpleUpload(Stream fileStream, string fileName, OneDriveItem parentFolder)
        {
            // Construct the complete URL to call
            string completeUrl;
            if (parentFolder.RemoteItem != null)
            {
                // Item will be uploaded to another drive
                completeUrl = string.Concat("drives/", parentFolder.RemoteItem.ParentReference.DriveId, "/items/", parentFolder.RemoteItem.Id, "/children/", fileName, "/content");
            }
            else if (parentFolder.ParentReference != null && !string.IsNullOrEmpty(parentFolder.ParentReference.DriveId))
            {
                // Item will be uploaded to another drive
                // Koen
                var existingItem = GetItemFromDriveById("", parentFolder.ParentReference.DriveId);

                completeUrl = string.Concat("drives/", parentFolder.ParentReference.DriveId, "/items/", parentFolder.Id, "/children/", fileName, "/content");
            }
            else if (!string.IsNullOrEmpty(parentFolder.WebUrl) && parentFolder.WebUrl.Contains("cid="))
            {
                // Item will be uploaded to another drive. Used by OneDrive Personal when using a shared item.
                completeUrl = string.Concat("drives/", parentFolder.WebUrl.Remove(0, parentFolder.WebUrl.IndexOf("cid=") + 4), "/items/", parentFolder.Id, "/children/", fileName, "/content");
            }
            else
            {
                // Item will be uploaded to the current user its drive
                completeUrl = string.Concat("drive/items/", parentFolder.Id, "/children/", fileName, "/content");
            }

            completeUrl = ConstructCompleteUrl(completeUrl);

            return await UploadFileViaSimpleUploadInternal(fileStream, completeUrl);
        }

        /// <summary>
        /// Performs a file upload to OneDrive using the simple OneDrive API. Best for small files on reliable network connections.
        /// </summary>
        /// <param name="fileStream">Stream to the file to upload</param>
        /// <param name="oneDriveUrl">The URL to POST the file contents to</param>
        /// <returns>The resulting OneDrive item representing the uploaded file</returns>
        protected async Task<OneDriveItem> UploadFileViaSimpleUploadInternal(Stream fileStream, string oneDriveUrl)
        {
            // Get an access token to perform the request to OneDrive
            var accessToken = await GetAccessToken();

            // Create an HTTPClient instance to communicate with the REST API of OneDrive
            using (var client = CreateHttpClient(accessToken.AccessToken))
            {
                // Load the content to upload
                using (var content = new StreamContent(fileStream))
                {
                    // Indicate that we're sending binary data
                    content.Headers.Add("Content-Type", "application/octet-stream");

                    // Construct the PUT message towards the webservice
                    using (var request = new HttpRequestMessage(HttpMethod.Put, oneDriveUrl))
                    {
                        // Set the content to upload
                        request.Content = content;

                        // Request the response from the webservice
                        using (var response = await client.SendAsync(request))
                        {
                            // Read the response as a string
                            var responseString = await response.Content.ReadAsStringAsync();

                            // Convert the JSON result to its appropriate type
                            try
                            {
                                var options = new JsonSerializerOptions();
                                options.Converters.Add(new JsonStringEnumConverter());

                                var responseOneDriveItem = JsonSerializer.Deserialize<OneDriveItem>(responseString, options);
                                responseOneDriveItem.OriginalJson = responseString;

                                return responseOneDriveItem;
                            }
                            catch (JsonException e)
                            {
                                throw new Exceptions.InvalidResponseException(responseString, e);
                            }
                        }
                    }
                }
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
