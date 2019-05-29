using System.Threading.Tasks;
using Microsoft.Azure.SpatialAnchors.Extensions;

namespace Microsoft.Azure.SpatialAnchors
{
    /// <summary>
    /// Use this class to create, locate and manage spatial anchors.
    /// </summary>
    public partial class CloudSpatialAnchorSession
    {
        // Replace internal IFuture returning methods with Task versions

        /// <summary>
        /// Creates a new persisted spatial anchor from the specified local anchor and string properties.
        /// </summary>
        /// <param name="anchor">Anchor to be persisted.</param>
        /// <returns></returns>
        public Task CreateAnchorAsync(CloudSpatialAnchor anchor) => CreateAnchorAsync_Internal(anchor).AsAsync();

        /// <summary>
        /// Deletes a persisted spatial anchor.
        /// </summary>
        /// <param name="anchor">The anchor to be deleted.</param>
        /// <returns></returns>
        public Task DeleteAnchorAsync(CloudSpatialAnchor anchor) => DeleteAnchorAsync_Internal(anchor).AsAsync();

        /// <summary>
        /// Gets the Azure Spatial Anchors access token from account key.
        /// </summary>
        /// <param name="accountKey">Account key.</param>
        /// <returns></returns>
        public Task<string> GetAccessTokenWithAccountKeyAsync(string accountKey) => GetAccessTokenWithAccountKeyAsync_Internal(accountKey).AsAsyncString();

        /// <summary>
        /// Gets the Azure Spatial Anchors access token from authentication token.
        /// </summary>
        /// <param name="authenticationToken">Authentication token.</param>
        /// <returns></returns>
        public Task<string> GetAccessTokenWithAuthenticationTokenAsync(string authenticationToken) => GetAccessTokenWithAuthenticationTokenAsync_Internal(authenticationToken).AsAsyncString();

        /// <summary>
        /// Gets a cloud spatial anchor for the given identifier, even if it hasn't been located yet.
        /// </summary>
        /// <param name="identifier">The identifier to look for.</param>
        /// <returns></returns>
        public Task<CloudSpatialAnchor> GetAnchorPropertiesAsync(string identifier) => GetAnchorPropertiesAsync_Internal(identifier).AsAsync<CloudSpatialAnchor>();

        public Task RefreshAnchorPropertiesAsync(CloudSpatialAnchor anchor) => RefreshAnchorPropertiesAsync_Internal(anchor).AsAsync();

        public Task UpdateAnchorPropertiesAsync(CloudSpatialAnchor anchor) => UpdateAnchorPropertiesAsync_Internal(anchor).AsAsync();

        /// <summary>
        /// Gets an object describing the status of the session.
        /// </summary>
        /// <returns></returns>
        public Task<SessionStatus> GetSessionStatusAsync() => this.SessionStatusAsync_Internal().AsAsync<SessionStatus>();
    }
}