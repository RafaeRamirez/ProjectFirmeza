using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Firmeza.Web.Services
{
    /// <summary>
    /// Cliente mínimo que firma peticiones HTTP a APIs de Google usando OAuth 2.0 con Service Accounts.
    /// </summary>
    public class GoogleApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<GoogleApiClient> _logger;

        public GoogleApiClient(HttpClient http, ILogger<GoogleApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<HttpResponseMessage> SendAsync(
            string url,
            HttpMethod method,
            string scope,
            string serviceAccountJsonPath,
            HttpContent? body = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(scope))
                throw new ArgumentException("Scope is required", nameof(scope));

            if (string.IsNullOrWhiteSpace(serviceAccountJsonPath))
                throw new ArgumentException("Service account path is required", nameof(serviceAccountJsonPath));

            if (!File.Exists(serviceAccountJsonPath))
                throw new FileNotFoundException("Service account file not found", serviceAccountJsonPath);

            var credential = GoogleCredential.FromFile(serviceAccountJsonPath).CreateScoped(scope);
            var token = await credential.UnderlyingCredential
                .GetAccessTokenForRequestAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
            {
                request.Content = body;
            }

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode >= 429)
            {
                _logger.LogWarning("Google API responded {StatusCode} for {Url}", response.StatusCode, url);
            }

            return response;
        }
    }
}
