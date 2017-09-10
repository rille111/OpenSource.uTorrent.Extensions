using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NLog;
using RestSharp;
using RestSharp.Authenticators;
using Rille.uTorrent.Extensions.PostProcess.Model;
using System.Net;

namespace Rille.uTorrent.Extensions.PostProcess.Services
{
    /// <summary>
    /// http://help.utorrent.com/customer/en/portal/topics/664593-web-api-and-webui-/articles
    /// 
    /// http://help.utorrent.com/customer/en/portal/articles/1573947-torrent-labels-list---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573951-torrent-job-properties---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573949-files-list---webapi
    /// http://help.utorrent.com/customer/en/portal/articles/1573952-actions---webapi
    /// </summary>
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    public class UTorrentManager : ITorrentManager
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Config _config;
        private readonly RestClient _restClient;
        private FileManager fileManager;

        public UTorrentManager(Config config)
        {
            _config = config;
            _restClient = new RestClient(_config.TorrentWebApiUrl);
            _restClient.Authenticator = new HttpBasicAuthenticator(_config.TorrentWebApiLogin, _config.TorrentWebApiPassword);
            //_restClient.Authenticator = new SimpleAuthenticator("username", "admin", "password", "admin");
        }

        public UTorrentManager(Config config, FileManager fileManager) : this(config)
        {
            this.fileManager = fileManager;
        }

        public List<Torrent> GetTorrentList()
        {
            var ret = new List<Torrent>();
            var req = new RestRequest("gui/?list=1");

            

            //var resp = _restClient.Get<dynamic>(req);
            var response = _restClient.Execute(req);

            if (response.ResponseStatus != ResponseStatus.Completed || !IsSuccessStatusCode(response.StatusCode))
                throw new System.Exception($"Invalid response from Torrent WebApi. HttpStatus: {response.StatusCode} {response.StatusDescription}, Http: {response.ResponseStatus.ToString()}");

            if (string.IsNullOrEmpty(response.Content))
                return new List<Torrent>();

            dynamic json = JObject.Parse(response.Content);
            var torrentsJArray = (JArray)json.torrents;
            foreach (var jToken in torrentsJArray)
            {
                var torrent = new Torrent(jToken[0].ToString(), _config);
                torrent.NumericStatus = (int)jToken[1];
                torrent.Name = jToken[2].ToString();
                torrent.Path = jToken[26].ToString();
                torrent.ActualSeedRatio = (int)jToken[7];
                ret.Add(torrent);
            }

            return ret;
        }

        public void DeleteTorrent(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public bool HasTorrentBeenPostProcessed(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public bool HasTorrentGoalsBeenReached(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public void MarkTorrentAsProcessing(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public void MarkTorrentAsProcessFinished(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public void MarkTorrentAsProcessFailed(Torrent torrent)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSuccessStatusCode(HttpStatusCode statusCode)
        {
            return ((int)statusCode >= 200) && ((int)statusCode <= 299);
        }

    }
}